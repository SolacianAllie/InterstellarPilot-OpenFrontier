using System;

namespace OpenFrontier.IP.Engine.Factions
{
	[Serializable]
	public class FactionAIBuildOrder
	{
		public double CreationTime;

		public int RpCost;

		public UnitShipTrader ShipTrader;

		public Unit ShipyardUnit;

		public int CreditsCost;

		public UnitClass UnitClass;

		public static FactionAIBuildOrder Create(EngineASX engine)
		{
			return new FactionAIBuildOrder
			{
				CreationTime = engine.ScenarioElapsedTime
			};
		}

		public bool IsValid(Faction builder)
		{
			if (ShipTrader != null && ShipyardUnit != null && UnitClass != null && ShipyardUnit.IsValidAndNotDestroyed && ShipyardUnit.Faction != null && !ShipyardUnit.Faction.IsHostileTo(builder))
			{
				return !builder.IsHostileTo(ShipyardUnit.Faction);
			}
			return false;
		}
	}
}
