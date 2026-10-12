using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Core.Units
{
	public static class ConstructedStationBroadcaster
	{
		private static HashSet<int> broadcastedFactions = new HashSet<int>(20);

		public static void Broadcast(Unit unit)
		{
			Sector sector = unit.Sector;
			if (!StationTypeNeedsBroadcasting(unit))
			{
				return;
			}
			SectorFinder.FindSectorsWithinJumpDistanceOfSimple(sector, 3, includeUnstableWormholes: false);
			broadcastedFactions.Clear();
			foreach (SectorFinder.SectorResult result in SectorFinder.Results)
			{
				foreach (Faction item in result.Sector.FactionsHeadquartered)
				{
					if (item != unit.Faction && !broadcastedFactions.Contains(item.UniqueId) && ShouldBroadcastToFaction(unit, unit.Faction, item))
					{
						broadcastedFactions.Add(item.UniqueId);
						if (!unit.Faction.IsPlayerFaction && item.IsPlayerFaction)
						{
							EngineASX.Instance.BroadcastStationPositionToPlayer(unit);
						}
						else if (item.Intel.DiscoverUnit(unit) == DiscoverUnitResult.New)
						{
							EngineASX.Instance.DebugInfo.NumTimesConstructedStationBroadcastedToNpc++;
						}
					}
				}
			}
		}

		private static bool StationTypeNeedsBroadcasting(Unit unit)
		{
			if (unit.IsMinorStation())
			{
				return false;
			}
			return unit.Faction.FactionType switch
			{
				FactionType.Bandit => false, 
				FactionType.Outlaw => unit.UnitClass.StationPurpose == StationPurpose.Factory, 
				_ => true, 
			};
		}

		private static bool ShouldBroadcastToFaction(Unit unit, Faction ownerFaction, Faction otherFaction)
		{
			if (ownerFaction.IsHostileToOrAlwaysHostileTo(otherFaction) || otherFaction.IsHostileToOrAlwaysHostileTo(ownerFaction))
			{
				return false;
			}
			if (otherFaction.Intel == null)
			{
				return false;
			}
			if (!unit.UnitClass.Legal)
			{
				return ownerFaction.TradeIllegalGoods;
			}
			return true;
		}
	}
}
