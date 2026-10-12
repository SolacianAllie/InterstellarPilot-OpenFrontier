using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ScavengeOrder : FleetOrder
	{
		public CollectCargoOwnerMode CollectOwnerMode;

		public float FullCargoThreshold = 0.9f;

		public Sector TargetSector;

		public override FleetOrderType OrderType => FleetOrderType.Scavenge;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveScavengeOrder activeScavengeOrder = gameObject.AddComponent<ActiveScavengeOrder>();
			activeScavengeOrder.ScavengeObjective = this;
			return activeScavengeOrder;
		}

		public override string GetDescription()
		{
			if (TargetSector != null)
			{
				return "Collect cargo in " + TargetSector.Name;
			}
			return "Collect Cargo";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
