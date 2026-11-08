using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveEnterWormholeOrder : ActiveFleetOrder
	{
		public const float NearbyWormholeThresholdDistance = 500f;

		public EnterWormholeOrder EnterWormholeObjective;

		public EnterWormholeState State;

		public override bool IsValid
		{
			get
			{
				if (base.IsValid && EnterWormholeObjective.TargetWormhole != null && EnterWormholeObjective.TargetWormhole.Unit.IsValidAndNotDestroyed)
				{
					return EnterWormholeObjective.TargetWormhole.IsEnterable();
				}
				return false;
			}
		}

		public override bool IsComplete => State == EnterWormholeState.Complete;

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (State == EnterWormholeState.Approach)
			{
				State = EnterWormholeState.Enter;
			}
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (State == EnterWormholeState.Approach && fleet.Sector == EnterWormholeObjective.TargetWormhole.Sector && Maths.GetDistanceIgnoreY(fleet.SectorPosition, EnterWormholeObjective.TargetWormhole.Unit.SectorPosition) < 500f)
			{
				State = EnterWormholeState.Enter;
				fleet.SetTargetToWormhole(this, EnterWormholeObjective.TargetWormhole);
			}
		}

		protected override void resetTargetPosition()
		{
			switch (State)
			{
			case EnterWormholeState.Approach:
				fleet.SetTargetToSectorObject(this, EnterWormholeObjective.TargetWormhole.Unit);
				break;
			case EnterWormholeState.Enter:
			{
				Wormhole targetWormhole = EnterWormholeObjective.TargetWormhole;
				if ((object)targetWormhole != null && targetWormhole.IsEnterable())
				{
					fleet.SetTargetToWormhole(this, EnterWormholeObjective.TargetWormhole);
				}
				break;
			}
			}
		}

		public override void OnFleetEnteredWormhole(Wormhole wormholeBeingEntered)
		{
			base.OnFleetEnteredWormhole(wormholeBeingEntered);
			if (EnterWormholeObjective.TargetWormhole == wormholeBeingEntered)
			{
				State = EnterWormholeState.Complete;
			}
		}
	}
}
