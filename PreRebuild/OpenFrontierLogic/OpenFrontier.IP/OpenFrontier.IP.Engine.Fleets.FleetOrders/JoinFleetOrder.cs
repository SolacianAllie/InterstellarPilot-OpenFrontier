using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class JoinFleetOrder : FleetOrder
	{
		public float ArrivalThreshold = 300f;

		[FormerlySerializedAs("TargetGroup")]
		public Fleet TargetFleet;

		public override FleetOrderType OrderType => FleetOrderType.JoinFleet;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveJoinFleetOrder activeJoinFleetOrder = gameObject.AddComponent<ActiveJoinFleetOrder>();
			activeJoinFleetOrder.JoinFleetOrder = this;
			return activeJoinFleetOrder;
		}

		public override string GetDescription()
		{
			if (TargetFleet != null)
			{
				if (TargetFleet.Sector != null)
				{
					return "Merge with " + TargetFleet.GetFriendlyName() + " in " + TargetFleet.Sector.Name;
				}
				return "Merge with " + TargetFleet.GetFriendlyName();
			}
			return "Merge with fleet";
		}

		protected override bool CanBeStackedOnInternal()
		{
			return false;
		}

		public override string Validate(Fleet fleet)
		{
			if (TargetFleet != null && TargetFleet == fleet)
			{
				return "Target fleet is the same as this fleet";
			}
			if (TargetFleet.Faction != fleet.Faction)
			{
				return $"Target fleet belongs to a different faction {TargetFleet.Faction} than this faction";
			}
			return null;
		}
	}
}
