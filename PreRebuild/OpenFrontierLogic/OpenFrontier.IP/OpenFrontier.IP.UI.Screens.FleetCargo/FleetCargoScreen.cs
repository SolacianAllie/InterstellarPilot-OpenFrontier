using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.UI.Screens.UnitCargo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FleetCargo
{
	public class FleetCargoScreen : EngineScreen
	{
		public TextMeshProUGUI ShipCargoText;

		public Image CargoIconImage;

		public Text UnitNameText;

		public CargoUsageSlider CargoUsageSlider;

		public Button EjectButton;

		public Text EjectCargoQuantityLabel;

		public GameObject EjectCargoRoot;

		public Slider EjectCargoSlider;

		public Button InfoButton;

		public GameObject InfoRoot;

		public ShipCargoItemList ItemList;

		public Button PricesButton;

		public TextMeshProUGUI SelectedLabel;

		private Fleet sourceFleet;

		public bool NavigateBackWhenSourceInvaild;

		private float lastCargoBayChangeTime;

		public Fleet SourceFleet
		{
			get
			{
				return sourceFleet;
			}
			set
			{
				sourceFleet = value;
			}
		}

		public int GetEjectCargoQuantity()
		{
			if (ItemList.FirstSelectedItem == null)
			{
				return 0;
			}
			int cargoCountOf = sourceFleet.GetCargoCountOf(ItemList.FirstSelectedItem.CargoClass);
			int num = Mathf.Min(1, cargoCountOf);
			return num + Mathf.RoundToInt(EjectCargoSlider.value * (float)(cargoCountOf - num));
		}

		public void EjectSelectedQuantity()
		{
			if (ItemList.FirstSelectedItem == null)
			{
				return;
			}
			int num = GetEjectCargoQuantity();
			if (num <= 0)
			{
				return;
			}
			for (int i = 0; i < sourceFleet.Ships.Count; i++)
			{
				if (num <= 0)
				{
					break;
				}
				UnitComponentHolder unitComponentHolder = sourceFleet.Ships[i];
				if (unitComponentHolder != null && unitComponentHolder.CargoBayComponent != null)
				{
					int countOf = unitComponentHolder.CargoBayComponent.GetCountOf(ItemList.FirstSelectedItem.CargoClass);
					if (countOf > 0)
					{
						int num2 = Mathf.Min(countOf, num);
						unitComponentHolder.CargoBayComponent.EjectCargo(ItemList.FirstSelectedItem.CargoClass, num2);
						num -= num2;
					}
				}
			}
			Refresh();
		}

		protected override void awake()
		{
			base.awake();
			EjectCargoSlider.onValueChanged.AddListener(EjectCargoSlider_ValueChanged);
			EjectButton.onClick.AddListener(EjectSelectedQuantity);
			InfoButton.onClick.AddListener(ShowCargoInfo);
			PricesButton.onClick.AddListener(ShowPrices);
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
		}

		protected override void onEnable()
		{
			base.onEnable();
			EjectCargoSlider.value = 1f;
		}

		protected override void refresh()
		{
			base.refresh();
			if (Eng != null && sourceFleet != null)
			{
				ItemList.SetItems(UIHelper.GetCargoItems(sourceFleet));
				CargoUsageSlider.Refresh(sourceFleet.CalculateCargoUsage(), sourceFleet.CalculateTotalCargoCapacity());
				RefreshSelectedItemInfo();
				UnitNameText.text = "Fleet cargo - " + sourceFleet.GetFriendlyNameNoPrefix();
				lastCargoBayChangeTime = GetLastCargoBayChangeTime();
				PricesButton.interactable = ItemList.FirstSelectedItem != null && !ItemList.FirstSelectedItem.CargoClass.IsReserved;
			}
		}

		private void RefreshShipCargoText()
		{
			ShipCargoText.text = GetShipCargoText();
		}

		private string GetShipCargoText()
		{
			CargoBayItem firstSelectedItem = ItemList.FirstSelectedItem;
			if (firstSelectedItem == null)
			{
				return "-";
			}
			List<(Unit, int)> list = new List<(Unit, int)>();
			foreach (UnitComponentHolder ship in sourceFleet.Ships)
			{
				int cargoCountOf = ship.GetCargoCountOf(firstSelectedItem.CargoClass);
				if (cargoCountOf > 0)
				{
					list.Add((ship.Unit, cargoCountOf));
				}
			}
			if (list.Count == 0)
			{
				return "-";
			}
			return string.Join("<br>", from e in list.Take(8)
				orderby e.Item2 descending
				select $"{e.Item1.GetFriendlyName(shortName: true)} x {e.Item2:N0}");
		}

		private float GetLastCargoBayChangeTime()
		{
			float num = 0f;
			foreach (UnitComponentHolder ship in sourceFleet.Ships)
			{
				if (ship.CargoBayComponent != null)
				{
					num = Mathf.Max(num, ship.CargoBayComponent.LastChangedTime);
				}
			}
			return num;
		}

		protected override void update()
		{
			base.update();
			if (NavigateBackWhenSourceInvaild && SourceInvalid() && GetBackTarget() != null)
			{
				if (Eng.PlayerUnit != null && Eng.PlayerUnit.IsValidAndNotDestroyed)
				{
					UIController.Instance.QuickMsg.AddMessage("Lost contact with target");
				}
				NavigateBack();
			}
			else if (sourceFleet != null && GetLastCargoBayChangeTime() > lastCargoBayChangeTime)
			{
				Refresh();
			}
		}

		private bool SourceInvalid()
		{
			if (!(sourceFleet == null))
			{
				return !sourceFleet.IsValid;
			}
			return true;
		}

		private void ItemList_SelectedItemChanged(ScrollList<CargoBayItem> sender, CargoBayItem oldItem, CargoBayItem newItem)
		{
			RefreshSelectedItemInfo();
		}

		private void RefreshSelectedItemInfo()
		{
			InfoRoot.gameObject.SetActive(ItemList.FirstSelectedItem != null);
			if (ItemList.FirstSelectedItem != null)
			{
				SelectedLabel.text = ItemList.FirstSelectedItem.CargoClass.ClassName;
				CargoIconImage.sprite = Eng.EngineResources.GetCargoSpriteOrDefault(ItemList.FirstSelectedItem.CargoClass);
			}
			EjectCargoRoot.gameObject.SetActive(ItemList.FirstSelectedItem != null && CanEjectCargo());
			if (ItemList.FirstSelectedItem != null)
			{
				RefreshEjectQuantity();
			}
			RefreshShipCargoText();
		}

		private void ShowPrices()
		{
			UIController.Instance.ScreenNavigator.ShowCargoPricesScreen(ItemList.FirstSelectedItem.CargoClass);
		}

		private void ShowCargoInfo()
		{
			UIController.Instance.ScreenNavigator.ShowCargoInfoScreen(ItemList.FirstSelectedItem.CargoClass);
		}

		private void EjectCargoSlider_ValueChanged(float delta)
		{
			if ((bool)sourceFleet)
			{
				RefreshEjectQuantity();
			}
		}

		private bool CanEjectCargo()
		{
			return true;
		}

		private void RefreshEjectQuantity()
		{
			EjectCargoQuantityLabel.text = TextFormattingHelper.FormatNumber(GetEjectCargoQuantity());
		}
	}
}
