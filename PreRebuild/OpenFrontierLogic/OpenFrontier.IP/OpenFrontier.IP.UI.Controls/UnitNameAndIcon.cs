using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Controls
{
	public class UnitNameAndIcon : MonoBehaviour
	{
		public Image UnitIconImage;

		public Image CargoIconImage;

		public Unit Unit;

		public TextMeshProUGUI Label;

		public bool ShortName = true;

		private void Awake()
		{
			if (CargoIconImage != null)
			{
				CargoIconImage.enabled = false;
			}
		}

		private void Start()
		{
			Refresh();
		}

		private void Update()
		{
			if (Unit != null)
			{
				RefreshLabelColor();
			}
		}

		private void RefreshLabelColor()
		{
			if (Label != null)
			{
				Label.color = EngineASX.Instance.GetFactionHostilityColorForPlayerTarget(Unit.Faction);
			}
		}

		public void Refresh()
		{
			if (CargoIconImage != null)
			{
				CargoIconImage.enabled = Unit != null && Unit.CargoComponent != null && Unit.CargoComponent.CargoClass != null && UnitIconImage != null;
				if (CargoIconImage.enabled)
				{
					CargoIconImage.sprite = Unit.CargoComponent.CargoClass.GetCargoSpriteOrDefault();
				}
			}
			if (Unit != null)
			{
				if (Label != null)
				{
					Label.text = Unit.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction, ShortName);
				}
				RefreshLabelColor();
				if (UnitIconImage != null)
				{
					UnitIconImage.sprite = Unit.UnitClass.GetIconSprite();
				}
			}
		}

		public void SetUnit(Unit unit)
		{
			Unit = unit;
			Refresh();
		}
	}
}
