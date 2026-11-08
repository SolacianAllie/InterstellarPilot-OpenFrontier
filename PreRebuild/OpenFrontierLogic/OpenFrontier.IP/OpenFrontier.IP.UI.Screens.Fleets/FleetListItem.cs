using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Fleets
{
	public class FleetListItem : MonoBehaviour
	{
		public Graphic HomeGraphic;

		public Graphic IdleGraphic;

		public Image BountyImage;

		public Image HostileTargetsImage;

		public FleetListItemShipIcon[] ShipIcons;

		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI CurrentOrderLabel;

		public CargoUsageSlider CargoUsageSlider;

		public TextMeshProUGUI LocationLabel;

		public TextMeshProUGUI ShipCountLabel;

		public Slider HullSlider;

		public Slider ShieldsSlider;

		private float lastAutoRefreshTime;

		public Transform ShipImagesTransform;

		public Fleet Item { get; set; }

		private void Awake()
		{
			FleetListItemShipIcon[] shipIcons = ShipIcons;
			for (int i = 0; i < shipIcons.Length; i++)
			{
				shipIcons[i].gameObject.SetActive(value: false);
			}
			BountyImage.enabled = false;
			CurrentOrderLabel.enabled = false;
			IdleGraphic.enabled = false;
			HostileTargetsImage.enabled = false;
			HomeGraphic.enabled = false;
		}

		public void Refresh(Fleet fleet)
		{
			Item = fleet;
			if (Item != null && Item.IsValid)
			{
				RefreshNameLabel();
				RefreshVolatile();
			}
		}

		private void RefreshVolatile()
		{
			RefreshLocationLabel();
			RefreshOrderLabel();
			RefreshShipImages();
			HostileTargetsImage.enabled = Item.HasHostileTargets;
			CargoUsageSlider.Refresh(Item.GetCachedCargoUsage(), Item.GetCachedTotalCargoCapacity());
			BountyImage.enabled = Item.HasBounty();
		}

		private void RefreshShipImages()
		{
			int num = 0;
			int? num2 = null;
			for (int num3 = Mathf.Min(Item.NpcPilots.Count, ShipIcons.Length) - 1; num3 >= 0; num3--)
			{
				NpcPilot npcPilot = Item.NpcPilots[num3];
				if (npcPilot != null && npcPilot.CurrentUnitComponents != null)
				{
					ShipIcons[num].gameObject.SetActive(value: true);
					ShipIcons[num].SetUnit(npcPilot.CurrentUnit);
					ShipIcons[num].Refresh();
					if (npcPilot.CurrentUnit.IsPlayerCurrentUnit)
					{
						num2 = num;
					}
					num++;
				}
			}
			for (int i = num; i < ShipIcons.Length; i++)
			{
				ShipIcons[i].gameObject.SetActive(value: false);
			}
			HomeGraphic.enabled = num2.HasValue;
			if (num2.HasValue)
			{
				RectTransform rectTransform = (RectTransform)ShipIcons[0].transform;
				float x = rectTransform.offsetMin.x;
				Vector2 anchoredPosition = HomeGraphic.rectTransform.anchoredPosition;
				anchoredPosition.x = x + (float)num2.Value * rectTransform.rect.width;
				HomeGraphic.rectTransform.anchoredPosition = anchoredPosition;
			}
		}

		private void Update()
		{
			if (Item != null && Item.IsValid && Time.time > lastAutoRefreshTime + 1f)
			{
				RefreshVolatile();
				lastAutoRefreshTime = Time.time;
			}
		}

		private void RefreshNameLabel()
		{
			string nameText = GetNameText();
			NameLabel.text = nameText;
			NameLabel.color = Item.Engine.OwnedColor;
		}

		private string GetNameText()
		{
			if (Item.Ships.Count == 1)
			{
				return Item.Ships[0].Unit.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction);
			}
			return Item.GetFriendlyName();
		}

		private void RefreshOrderLabel()
		{
			bool flag = OrdersHelper.HasFleetGotStatusToDisplay(Item);
			CurrentOrderLabel.enabled = flag;
			if (flag)
			{
				CurrentOrderLabel.text = OrdersHelper.GetOrdersTextAndFleetStatus(Item, EngineASX.Instance.LocalFaction);
			}
			IdleGraphic.enabled = !flag;
		}

		private void RefreshLocationLabel()
		{
			LocationLabel.text = UnitNamer.GetSectorNameAndDistanceForFaction(EngineASX.Instance.LocalFaction, EngineASX.Instance.ActiveSector, Item.Sector);
		}
	}
}
