using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Intel
{
	public class BuySectorIntelOption : DynamicCommsStageOption
	{
		private ICommsController commsController;

		public List<Sector> Sectors;

		public override void Select(ICommsController commsController)
		{
			this.commsController = commsController;
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
			{
				universeMap.Title = "Select sector...";
				universeMap.AllowSectorSelection = true;
				universeMap.EnabledSectors = Sectors.ToList();
				universeMap.ShowSelectedSectorInfo = false;
				universeMap.SectorSelectedCallback = SectorPicked;
				PopulateVisibleSectors(universeMap);
				universeMap.RestrictNavigationAway();
			});
		}

		private void PopulateVisibleSectors(UniverseMapScreen universeMap)
		{
			List<Sector> list = Sectors.ToList();
			if (EngineASX.Instance.LocalPlayerSector != null && !Sectors.Contains(EngineASX.Instance.LocalPlayerSector))
			{
				list.Add(EngineASX.Instance.LocalPlayerSector);
			}
			foreach (int discoveredSectorId in EngineASX.Instance.LocalFaction.Intel.DiscoveredSectorIds)
			{
				Sector sectorById = EngineASX.Instance.GetSectorById(discoveredSectorId);
				if (!list.Contains(sectorById))
				{
					list.Add(sectorById);
				}
			}
			universeMap.CustomDisplayedSectors = list;
		}

		private void SectorPicked(UniverseMapScreen sender, Sector selectedSector)
		{
			if (selectedSector != null)
			{
				BuySectorIntelConfirmStage buySectorIntelConfirmStage = new BuySectorIntelConfirmStage();
				buySectorIntelConfirmStage.Sector = selectedSector;
				buySectorIntelConfirmStage.DynamicCommsHandler = (DynamicCommsHandler)commsController.CommsHandler;
				commsController.ChangeStage(buySectorIntelConfirmStage);
			}
		}
	}
}
