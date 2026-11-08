using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class MineOrder : FleetOrder
	{
		public Asteroid ManualMineTarget;

		public float FullCargoThreshold = 0.9f;

		public Sector TargetSector;

		public CollectCargoOwnerMode CollectOwnerMode;

		public override FleetOrderType OrderType => FleetOrderType.Mine;

		public override string GetDescription()
		{
			if (TargetSector != null)
			{
				return "Mine " + PluralizeAsteroid() + " in " + TargetSector.Name;
			}
			return "Mine " + PluralizeAsteroid();
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (TargetSector != null)
			{
				return "Mine " + PluralizeAsteroid() + " in " + UnitNamer.GetSectorNameAndDistanceForFaction(faction, currentSector, TargetSector);
			}
			return "Mine " + PluralizeAsteroid();
		}

		private string PluralizeAsteroid()
		{
			if (ManualMineTarget != null)
			{
				return "asteroid";
			}
			return "asteroids";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveMineOrder activeMineOrder = gameObject.AddComponent<ActiveMineOrder>();
			activeMineOrder.MineObjective = this;
			return activeMineOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
