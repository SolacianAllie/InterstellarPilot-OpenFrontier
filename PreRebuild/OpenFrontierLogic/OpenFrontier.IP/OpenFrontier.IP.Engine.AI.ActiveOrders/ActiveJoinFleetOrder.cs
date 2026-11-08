using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveJoinFleetOrder : ActiveFleetOrder
	{
		public JoinFleetOrder JoinFleetOrder;

		public override bool IsValid
		{
			get
			{
				if (JoinFleetOrder.TargetFleet != null && JoinFleetOrder.TargetFleet.IsValid)
				{
					return JoinFleetOrder.TargetFleet.Faction == fleet.Faction;
				}
				return false;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (JoinFleetOrder.TargetFleet != null && JoinFleetOrder.TargetFleet.ActiveOrder is ActiveProtectOrder activeProtectOrder && activeProtectOrder.ProtectObjective != null && activeProtectOrder.ProtectObjective.Target != null)
			{
				if (activeProtectOrder.ProtectObjective.Target.TargetFleet == fleet)
				{
					activeProtectOrder.OnInvalid("Fleet has been merged into ours");
				}
				else if (activeProtectOrder.ProtectObjective.Target.TargetUnit != null && activeProtectOrder.ProtectObjective.Target.TargetUnit.GetFleet() == fleet)
				{
					activeProtectOrder.OnInvalid("Protected unit " + activeProtectOrder.ProtectObjective.Target.TargetUnit.GetFriendlyName() + " has joined our fleet");
				}
			}
			if (IsValid)
			{
				fleet.SetPilotsFleet(JoinFleetOrder.TargetFleet);
				OnComplete();
			}
			else
			{
				OnInvalid();
			}
		}

		protected override void resetTargetPosition()
		{
			if (IsValid)
			{
				fleet.SetTargetToFleet(this, JoinFleetOrder.TargetFleet);
				if (JoinFleetOrder.ArrivalThreshold > 0f)
				{
					fleet.NavTarget.ArrivalThreshold = JoinFleetOrder.ArrivalThreshold;
				}
			}
		}
	}
}
