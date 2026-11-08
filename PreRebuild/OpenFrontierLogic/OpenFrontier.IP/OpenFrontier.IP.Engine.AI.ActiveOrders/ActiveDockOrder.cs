using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveDockOrder : ActiveFleetOrder
	{
		public DockOrder DockObjective;

		private float lastTimeRequestedDockPermission;

		private const float requestDockPermissionFrequency = 5f;

		public override bool IsValid
		{
			get
			{
				if (DockObjective.TargetDock != null)
				{
					return DockObjective.TargetDock.IsDockable;
				}
				return false;
			}
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (Time.time > lastTimeRequestedDockPermission + 5f)
			{
				lastTimeRequestedDockPermission = Time.time;
				if (!DockObjective.TargetDock.Faction.RequestDock(DockObjective.TargetDock, fleet.Faction))
				{
					OnInvalidBecauseNoDockPermission();
				}
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (fleet.AllUnitsAtDock(DockObjective.TargetDock))
			{
				OnComplete();
			}
		}

		protected override void resetTargetPosition()
		{
			base.resetTargetPosition();
			if (DockObjective.TargetDock != null)
			{
				fleet.SetTargetToDock(this, DockObjective.TargetDock);
			}
		}

		public override void OnNpcUnableToDockAtNavpoint(NpcPilot controller, Unit unit, UnableToDockReason unableToDockReason)
		{
			if (unableToDockReason == UnableToDockReason.Refused)
			{
				OnInvalidBecauseNoDockPermission();
			}
			else
			{
				base.OnNpcUnableToDockAtNavpoint(controller, unit, unableToDockReason);
			}
		}

		private void OnInvalidBecauseNoDockPermission()
		{
			OnInvalid("Denied docking permission");
		}
	}
}
