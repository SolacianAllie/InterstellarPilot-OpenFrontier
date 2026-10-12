using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveMoveToSectorOrder : ActiveFleetOrder
	{
		public MoveToSectorOrder MoveToSectorOrder;

		public override bool IsValid => MoveToSectorOrder.TargetSector != null;

		protected override void onInit()
		{
			base.onInit();
			if (IsTargetSectorExcludedFromNavigation())
			{
				OnInvalid("Target sector [" + MoveToSectorOrder.TargetSector.Name + "] is excluded from navigation");
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			OnComplete();
		}

		private bool IsTargetSectorExcludedFromNavigation()
		{
			if (fleet.Faction.AutopilotExcludedSectors.Count == 0)
			{
				return false;
			}
			if (fleet.Faction.AutopilotExcludedSectors.Contains(MoveToSectorOrder.TargetSector.UniqueId))
			{
				return true;
			}
			return false;
		}

		protected override void tick(float elapsedTime)
		{
			if (!fleet.NavTarget.IsActive)
			{
				IsIdle = true;
			}
			if (fleet.AllUnitsInSector(MoveToSectorOrder.TargetSector))
			{
				OnComplete();
			}
		}

		protected override void resetTargetPosition()
		{
			if (MoveToSectorOrder.TargetSector != null)
			{
				fleet.SetTargetToSectorPosition(this, MoveToSectorOrder.TargetSector, Vector3.zero);
			}
		}
	}
}
