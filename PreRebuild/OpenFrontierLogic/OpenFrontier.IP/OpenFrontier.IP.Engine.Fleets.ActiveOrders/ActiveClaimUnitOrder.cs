using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Claiming;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class ActiveClaimUnitOrder : ActiveFleetOrder
	{
		public ClaimUnitOrder ClaimUnitOrder;

		public override bool IsComplete
		{
			get
			{
				if (ClaimUnitOrder.TargetUnit != null && ClaimUnitOrder.TargetUnit.Faction != null)
				{
					return ClaimUnitOrder.TargetUnit.Faction == fleet.Faction;
				}
				return false;
			}
		}

		public override bool IsValid
		{
			get
			{
				if (ClaimUnitHelper.IsClaimableUnitType(ClaimUnitOrder.TargetUnit))
				{
					if (!(ClaimUnitOrder.TargetUnit.Faction == null))
					{
						return ClaimUnitOrder.TargetUnit.Faction == fleet.Faction;
					}
					return true;
				}
				return false;
			}
		}

		protected override void resetTargetPosition()
		{
			base.resetTargetPosition();
			if (ClaimUnitOrder.TargetUnit != null && ClaimUnitOrder.TargetUnit.IsValidAndNotDestroyed && ClaimUnitOrder.TargetUnit.Faction == null && ClaimUnitOrder.TargetUnit.GetPilot() == null)
			{
				fleet.SetTargetToUnit(this, ClaimUnitOrder.TargetUnit);
				fleet.NavTarget.ArrivalThreshold = ClaimUnitOrder.TargetUnit.Radius + 50f;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (IsValid && ClaimUnitOrder.TargetUnit.Faction == null)
			{
				fleet.Faction.ClaimUnit(ClaimUnitOrder.TargetUnit, fleet);
			}
		}
	}
}
