using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.RequestTaxi
{
	public class RequestTaxiScreen : EngineScreen
	{
		public Button RequestTaxiButton;

		public RequestTaxiSceneList SceneList;

		public RequestTaxiStationList StationList;

		protected override void awake()
		{
			base.awake();
			SceneList.SelectedItemChanged += SceneList_SelectedItemChanged;
			StationList.SelectedItemChanged += StationList_SelectedItemChanged;
			RequestTaxiButton.onClick.AddListener(RequestTaxiButton_Activated);
		}

		protected override void refresh()
		{
			base.refresh();
			PopulateSceneList();
			RefreshTaxiButton();
		}

		private void RequestTaxiButton_Activated()
		{
			if (StationList.FirstSelectedItem != null && ShouldShowUnit(StationList.FirstSelectedItem))
			{
				if (StationList.FirstSelectedItem.Faction == null || StationList.FirstSelectedItem.Faction.RequestDock(StationList.FirstSelectedItem, Eng.LocalPlayer.Faction))
				{
					Eng.ChangePlayerUnit(StationList.FirstSelectedItem);
					Eng.PlayChangeShipAudio();
				}
				else
				{
					UIController.Instance.QuickMsg.AddMessage("Docking denied");
				}
			}
		}

		private void StationList_SelectedItemChanged(ScrollList<Unit> sender, Unit oldItem, Unit newItem)
		{
			RefreshTaxiButton();
		}

		private void RefreshTaxiButton()
		{
			RequestTaxiButton.gameObject.SetActive(StationList.FirstSelectedItem != null);
		}

		private void SceneList_SelectedItemChanged(ScrollList<Sector> sender, Sector oldItem, Sector newItem)
		{
			PopulateStationList();
		}

		private void PopulateSceneList()
		{
			List<Sector> copyOfDiscoveredSectors = Eng.LocalFaction.Intel.GetCopyOfDiscoveredSectors();
			SceneList.SetItems(copyOfDiscoveredSectors.OrderBy((Sector e) => e.GetJumpDistanceTo(Eng.LocalPlayer.Person.Sector)));
		}

		private void PopulateStationList()
		{
			List<Unit> unitsByType = SceneList.FirstSelectedItem.GetUnitsByType(UnitType.Station);
			if (unitsByType != null)
			{
				StationList.SetItems(unitsByType.Where((Unit e) => ShouldShowUnit(e)));
			}
			else
			{
				StationList.SetItems(new Unit[0]);
			}
		}

		private bool ShouldShowUnit(Unit unit)
		{
			if (!unit.UnitClass.IsTurret && unit.IsDockable && unit.GetRootUnit() != DockUI.PlayerRootUnit)
			{
				return Eng.LocalFaction.Intel.IsUnitDiscoveredOrOwned(unit);
			}
			return false;
		}
	}
}
