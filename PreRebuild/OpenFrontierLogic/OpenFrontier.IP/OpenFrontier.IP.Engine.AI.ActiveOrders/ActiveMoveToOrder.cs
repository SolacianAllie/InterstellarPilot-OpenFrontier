using System;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveMoveToOrder : ActiveFleetOrder
	{
		public MoveToOrder MoveToObjective;

		public override bool IsValid
		{
			get
			{
				if (MoveToObjective.Target != null)
				{
					return MoveToObjective.Target.IsValid();
				}
				return false;
			}
		}

		protected override void onInit()
		{
			base.onInit();
			if (MoveToObjective.Target != null && MoveToObjective.Target.TargetObject != null && !MoveToObjective.Target.HadSceneObject)
			{
				MoveToObjective.Target.HadSceneObject = true;
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (MoveToObjective.CompleteOnReachTarget)
			{
				OnComplete();
			}
		}

		public override void OnFleetFailedToFindPathToTarget()
		{
			base.OnFleetFailedToFindPathToTarget();
			IsIdle = true;
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			if (fleet.CurState == FleetState.MoveToTarget && fleet.NavTarget.IsActive && !fleet.NavTarget.HasAttemptedToCalculatedPath && fleet.NavTarget.GetTargetSector() != fleet.Sector)
			{
				return "Looking for path";
			}
			return base.GetStatusTextInternal(localFaction);
		}

		private bool IsTargetSectorExcludedFromNavigation()
		{
			if (fleet.Faction.AutopilotExcludedSectors.Count == 0)
			{
				return false;
			}
			if (fleet.Faction.AutopilotExcludedSectors.Contains(MoveToObjective.Target.GetTargetSector().UniqueId))
			{
				return true;
			}
			return false;
		}

		protected override void resetTargetPosition()
		{
			if (IsValid)
			{
				SectorObject targetObject = MoveToObjective.Target.TargetObject;
				if (targetObject != null && targetObject.IsValid)
				{
					fleet.SetTargetToSectorObject(this, targetObject);
					fleet.NavTarget.RelativeTargetPosition = GetRelativePositionFromTarget(targetObject);
					fleet.NavTarget.DockIfTargetUnitDocks = true;
				}
				else
				{
					fleet.SetTargetToSectorPosition(this, MoveToObjective.Target.GetTargetSector(), MoveToObjective.Target.GetTargetSectorPosition());
				}
				if (MoveToObjective.ArrivalThreshold > 0f)
				{
					fleet.NavTarget.ArrivalThreshold = MoveToObjective.ArrivalThreshold;
				}
				fleet.NavTarget.MatchTargetOrienation = MoveToObjective.MatchTargetOrientation;
			}
		}

		private Vector3 GetRelativePositionFromTarget(SectorObject targetObject)
		{
			Vector3 zero = Vector3.zero;
			if (MoveToObjective.PreferredRelativeVectorFromTarget.HasValue)
			{
				zero = MoveToObjective.PreferredRelativeVectorFromTarget.Value;
			}
			else
			{
				UnityEngine.Random.InitState(fleet.UniqueId);
				zero = Geometry.RandomXZUnitVector();
				UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
			}
			float num = fleet.VeryBasicFleetRadiusCalculation();
			num += MoveToObjective.Target.GetTargetRadius() * EngineASX.Instance.GameSettings.DistanceSettings.MoveToOrderTargetRadiusMultiplier + 75f;
			if (targetObject is Unit)
			{
				if (((Unit)targetObject).WormholeComponent != null)
				{
					return Vector3.forward * EngineASX.Instance.GameSettings.DistanceSettings.AIMoveToWormholePreferredDistance;
				}
				return zero * num;
			}
			return zero * num;
		}
	}
}
