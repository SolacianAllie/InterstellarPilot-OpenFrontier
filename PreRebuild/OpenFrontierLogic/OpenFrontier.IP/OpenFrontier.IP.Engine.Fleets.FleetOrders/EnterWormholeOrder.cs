using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class EnterWormholeOrder : FleetOrder
	{
		public Wormhole TargetWormhole;

		public override FleetOrderType OrderType => FleetOrderType.EnterWormhole;

		public override string GetDescription()
		{
			if (TargetWormhole != null)
			{
				return "Enter " + UnitNamer.GetFriendlyNameAndFactionShortNameAndSector(TargetWormhole.Unit);
			}
			return "Enter Wormhole";
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (faction != null && TargetWormhole != null && faction != null)
			{
				return "Enter " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(faction, currentSector, TargetWormhole.Unit);
			}
			return GetDescription();
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveEnterWormholeOrder activeEnterWormholeOrder = gameObject.AddComponent<ActiveEnterWormholeOrder>();
			activeEnterWormholeOrder.EnterWormholeObjective = this;
			return activeEnterWormholeOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
