using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionValidTradeTargetsController
	{
		public static void Get(Faction faction, List<CargoTrader> list, out bool foundRefinery)
		{
			EngineASX engine = faction.Engine;
			foundRefinery = false;
			if (!(faction.Intel != null))
			{
				return;
			}
			foreach (int discoveredTraderId in faction.Intel.DiscoveredTraderIds)
			{
				Unit unitByid = EngineASX.Instance.GetUnitByid(discoveredTraderId);
				if (IsTraderUnitValid(faction, engine, unitByid) && (!unitByid.Faction.IsAIFactionType || unitByid.Faction.RequestDock(unitByid, faction)))
				{
					if (unitByid.UnitClass.StationPurpose == StationPurpose.Refinery)
					{
						foundRefinery = true;
					}
					list.Add(unitByid.Components.CargoTrader);
				}
			}
		}

		public static void CheckValidTraderTarget(Unit traderUnit, Faction faction, List<CargoTrader> list)
		{
			if (IsTraderUnitValid(faction, EngineASX.Instance, traderUnit) && !list.Contains(traderUnit.Components.CargoTrader))
			{
				list.Add(traderUnit.Components.CargoTrader);
			}
		}

		private static bool IsTraderUnitValid(Faction faction, EngineASX engine, Unit traderUnit)
		{
			if (traderUnit != null && traderUnit.IsDockable && !faction.AutopilotExcludedSectors.Contains(traderUnit.Sector.UniqueId) && traderUnit.Faction != null && (faction.IsPlayerFaction || faction.TradeIllegalGoods || traderUnit.UnitClass.Legal) && !traderUnit.Faction.IsHostileToOrAlwaysHostileTo(faction) && !faction.IsHostileToOrAlwaysHostileTo(traderUnit.Faction) && traderUnit.Faction.GetOpinion(faction) >= engine.GameSettings.AIMinTradeOpinion)
			{
				if (!faction.IsPlayerFaction && EngineASX.Instance.IsUnitExcludedFromNpcTraderTargets(traderUnit))
				{
					return false;
				}
				return true;
			}
			return false;
		}
	}
}
