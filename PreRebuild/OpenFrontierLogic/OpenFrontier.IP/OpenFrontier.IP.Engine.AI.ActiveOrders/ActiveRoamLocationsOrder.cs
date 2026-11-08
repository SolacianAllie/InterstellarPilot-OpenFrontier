using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveRoamLocationsOrder : ActiveFleetOrder
	{
		public AutonomousRoamLocationsOrder AutonomousRoamLocationsObjective;

		private Vector3 currentTargetSectorPosition = Vector3.zero;

		private Sector currentTargetSector;

		private float nextFindTargetTime;

		public Vector3 CurrentTargetSectorPosition
		{
			get
			{
				return currentTargetSectorPosition;
			}
			set
			{
				currentTargetSectorPosition = value;
			}
		}

		public Sector CurrentTargetSector
		{
			get
			{
				return currentTargetSector;
			}
			set
			{
				currentTargetSector = value;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			FindNewTarget();
		}

		protected override void tick(float elapsedTime)
		{
			FindTargetPeriodically();
		}

		private void FindTargetPeriodically()
		{
			if (currentTargetSector == null && Time.time > nextFindTargetTime)
			{
				FindNewTarget();
				nextFindTargetTime = Time.time + 10f;
			}
		}

		private void FindNewTarget()
		{
			SectorFinder.FindNavigableDiscoveredSectorsWithinJumpDistanceOf(fleet.GetHomeSectorOrCurrent(), GetActualMaxJumpDist(), fleet.Faction);
			if (SectorFinder.Results.Count > 0)
			{
				currentTargetSector = SectorFinder.Results.GetRandom().Sector;
			}
			else
			{
				currentTargetSector = null;
			}
			if (currentTargetSector != null)
			{
				currentTargetSectorPosition = currentTargetSector.GetRandomSafeDeploymentSectorPosition(0f, 0.8f, 50f, GameController.Instance.NonOVerlappingUnitsMask);
			}
		}

		protected override void resetTargetPosition()
		{
			if (currentTargetSector != null)
			{
				fleet.SetTargetToSectorPosition(this, currentTargetSector, currentTargetSectorPosition);
			}
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			currentTargetSector = null;
		}
	}
}
