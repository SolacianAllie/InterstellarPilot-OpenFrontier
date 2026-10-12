using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens.BuySectorIntel;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Intel
{
	public static class IntelOptionsController
	{
		public static void PopulateOptions(DynamicCommsHandler commsHandler, List<ICommsStageOption> stageOptionCache)
		{
			CommsStageOptionGroup optionGroup = new CommsStageOptionGroup
			{
				DisplayText = "Intel..."
			};
			if (commsHandler.OwnerFaction.FactionAI != null && commsHandler.OwnerFaction.FactionAI.WillSellIntelTo(EngineASX.Instance.LocalFaction))
			{
				List<Sector> sectorsForWhileIntelCanBeBought = GetSectorsForWhileIntelCanBeBought(commsHandler.OwnerFaction);
				if (sectorsForWhileIntelCanBeBought.Count > 0)
				{
					BuySectorIntelOption item = new BuySectorIntelOption
					{
						OptionText = "Buy sector intel",
						OptionGroup = optionGroup,
						Sectors = sectorsForWhileIntelCanBeBought
					};
					stageOptionCache.Add(item);
				}
			}
		}

		private static List<Sector> GetSectorsForWhileIntelCanBeBought(Faction sellingFaction)
		{
			List<Sector> list = new List<Sector>(10);
			List<Unit> list2 = new List<Unit>(32);
			foreach (int discoveredSectorId in sellingFaction.Intel.DiscoveredSectorIds)
			{
				Sector sectorById = EngineASX.Instance.GetSectorById(discoveredSectorId);
				if (sectorById != null)
				{
					list2.Clear();
					sellingFaction.Intel.GetDiscoveredUnitsInSceneWhichOtherFactionHasntNonAlloc(sectorById, EngineASX.Instance.LocalFaction, list2);
					if (list2.Any((Unit intelUnit) => BuySectorIntelScreen.CanPlayerBuySectorIntelOnUnit(intelUnit) && sellingFaction.FactionAI.WillSellIntelItemTo(EngineASX.Instance.LocalFaction, intelUnit)))
					{
						list.Add(sectorById);
					}
				}
			}
			return list;
		}
	}
}
