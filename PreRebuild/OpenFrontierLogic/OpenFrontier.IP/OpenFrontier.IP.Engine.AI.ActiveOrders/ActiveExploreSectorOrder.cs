using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveExploreSectorOrder : ActiveFleetOrder
	{
		public ExploreSectorOrder ExploreSectorObjective;

		private Vector3? currentTargetSectorPosition;

		private float nextFindTargetTime;

		public override float BaseTargetInterceptionScoreMultiplier => 0.1f;

		public Vector3? CurrentTargetSectorPosition
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

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			ClearExplorationTarget();
			FindNewExplorationTarget();
		}

		private void ClearExplorationTarget()
		{
		}

		protected override void tick(float elapsedTime)
		{
			FindTargetPeriodicallyIfNone();
		}

		private void FindTargetPeriodicallyIfNone()
		{
			if (ExploreSectorObjective.Sector != null && !currentTargetSectorPosition.HasValue && Time.time > nextFindTargetTime)
			{
				FindNewExplorationTarget();
				nextFindTargetTime = Time.time + 10f;
			}
		}

		private void FindNewExplorationTarget()
		{
			SetTargetToRandomPositionInCurrentSector();
		}

		private void SetTargetToRandomPositionInCurrentSector()
		{
			currentTargetSectorPosition = ExploreSectorObjective.Sector.GetRandomSafeDeploymentSectorPosition(0f, EngineASX.Instance.GameSettings.MaxExploreRangeGateDistanceMultiplier, 50f, GameController.Instance.NonOVerlappingUnitsMask);
		}

		protected override void resetTargetPosition()
		{
			if (ExploreSectorObjective.Sector != null && currentTargetSectorPosition.HasValue)
			{
				fleet.SetTargetToSectorPosition(this, ExploreSectorObjective.Sector, currentTargetSectorPosition.Value);
			}
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			ClearExplorationTarget();
		}
	}
}
