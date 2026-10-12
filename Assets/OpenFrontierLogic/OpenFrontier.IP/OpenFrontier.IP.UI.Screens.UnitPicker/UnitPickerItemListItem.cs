using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using TMPro;

namespace OpenFrontier.IP.UI.Screens.UnitPicker
{
	public class UnitPickerItemListItem : ScrollListItem<UnitPickerItem>
	{
		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI SectorLabel;

		public UnitConditionControllerUI UnitConditionController;

		public CargoUsageSlider CargoUsageSlider;

		public override void Refresh()
		{
			base.Refresh();
			UnitConditionController.gameObject.SetActive(Item.Unit != null);
			CargoUsageSlider.gameObject.SetActive(Item.Unit != null);
			if (Item.Unit != null)
			{
				CargoUsageSlider.RefreshFromCargoBayComponent(Item.Unit.CargoBayComponent);
				Faction faction = Item.Unit.Faction;
				RefreshNameLabelText();
				RefreshNameLabelColor(faction);
				RefreshSectorLabelText();
				UnitConditionController.LocalUnit = Item.Unit;
			}
			else
			{
				NameLabel.text = "None";
				SectorLabel.text = string.Empty;
			}
		}

		private void RefreshNameLabelText()
		{
			NameLabel.text = Item.Unit.GetFriendlyName();
		}

		private void RefreshNameLabelColor(Faction unitFaction)
		{
			if (unitFaction != null && Item.Unit.Engine.LocalFaction != null)
			{
				NameLabel.color = Item.Unit.Engine.GetFactionHostilityColor(unitFaction, Item.Unit.Engine.LocalFaction);
			}
			else
			{
				NameLabel.color = Item.Unit.Engine.AttitudeNeutralColor;
			}
		}

		private void RefreshSectorLabelText()
		{
			int value = 0;
			if (Item.LocalSector != null)
			{
				value = Item.Unit.Sector.GetJumpDistanceTo(Item.LocalSector);
			}
			SectorLabel.text = TextFormattingHelper.GetSectorNameAndDistance(Item.Unit.Sector, value);
		}
	}
}
