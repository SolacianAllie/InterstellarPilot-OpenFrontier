using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.HUDScannerDisplay
{
	public class HUDScannerDisplayListItem : ScrollListItem<HUDScannerItem>
	{
		public Graphic MissionPathIcon;

		public Graphic CustomPathIcon;

		public UnitConditionControllerUI UnitConditionController;

		public TextMeshProUGUI NameText;

		public TextMeshProUGUI DistanceText;

		public override void Refresh()
		{
			base.Refresh();
			UnitConditionController.LocalUnit = Item.Unit;
			UnitConditionController.Tick();
			MissionPathIcon.gameObject.SetActive(Item.IsMissionWaypoint);
			CustomPathIcon.gameObject.SetActive(Item.IsCustomWaypoint);
			if (Item != null)
			{
				RefreshNameText();
				RefreshDistanceText();
			}
		}

		private void RefreshDistanceText()
		{
			DistanceText.text = GetDistanceText(Item);
		}

		private void RefreshNameText()
		{
			NameText.text = GetNameText(Item.Unit);
			EngineASX instance = EngineASX.Instance;
			NameText.color = UnitInfoHelper.GetUnitDisplayColor(Item.Unit, instance.LocalFaction);
		}

		private string GetDistanceText(HUDScannerItem item)
		{
			return TextFormattingHelper.FormatDistance(item.Distance);
		}

		private string GetNameText(Unit item)
		{
			bool showingShipNameInLabel;
			return UnitNamer.GetNameWithFactionAndFleet(EngineASX.Instance.LocalFaction, item, usePilotNamesAsDesignations: false, preferShortUnitName: true, preferShortFactionName: true, out showingShipNameInLabel);
		}

		public override void Tick()
		{
			base.Tick();
			UnitConditionController.Tick();
		}

		public override void CleanupOnDisable()
		{
			UnitConditionController.CleanupOnDisable();
		}
	}
}
