using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI;

namespace OpenFrontier.IP.Engine.Intel
{
	public static class IntelHelper
	{
		public static int GetCostToDiscoverUnits(IEnumerable<Unit> units, Faction buyingFaction)
		{
			int num = 0;
			if (units != null)
			{
				foreach (Unit unit in units)
				{
					num += GetCostToDiscoverUnit(unit, buyingFaction);
				}
			}
			return num;
		}

		public static int GetCostToDiscoverUnit(Unit unit, Faction buyingFaction)
		{
			if (unit.Faction != buyingFaction)
			{
				SectorIntelSettings sectorIntelSettings = EngineASX.Instance.GameSettings.SectorIntelSettings;
				if (unit.UnitType == UnitType.Asteroid)
				{
					return sectorIntelSettings.BuyIntelCostPerAsteroid;
				}
				if (unit.UnitClass.IsMinorStation())
				{
					return sectorIntelSettings.BuyIntelCostPerTurret;
				}
				if (unit.UnitType == UnitType.Station)
				{
					return sectorIntelSettings.BuyIntelCostPerStation;
				}
				if (unit.UnitType == UnitType.Wormhole)
				{
					return sectorIntelSettings.BuyIntelCostPerWormhole;
				}
				return sectorIntelSettings.BuyIntelCostMiscCost;
			}
			return 0;
		}

		public static void AddIntelPurchasedQuickMessage()
		{
			UIController.Instance.QuickMsg.AddMessage("Intel added to database");
		}
	}
}
