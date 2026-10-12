using System;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveMoveToNearestFriendlyStationOrder : ActiveFleetOrder
	{
		public MoveToNearestFriendlyStationOrder MoveToNearestFriendlyStationObjective;

		private Unit targetStation;

		private MoveToNearestFriendlyStationSearch search;

		private Vector3? offsetFromTarget;

		public Unit TargetStation
		{
			get
			{
				return targetStation;
			}
			set
			{
				if (targetStation != value)
				{
					targetStation = value;
				}
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (MoveToNearestFriendlyStationObjective.CompleteOnReachTarget)
			{
				OnComplete();
			}
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (targetStation == null)
			{
				if (search != null)
				{
					if (search.IsComplete)
					{
						TargetStation = search.BestUnit;
						search = null;
					}
					else
					{
						search.Search();
					}
				}
				else
				{
					search = new MoveToNearestFriendlyStationSearch();
					search.Init(fleet, GetActualMaxJumpDist());
				}
			}
			else if (!IsTargetStationValid(targetStation))
			{
				TargetStation = null;
			}
		}

		private bool IsTargetStationValid(Unit targetStation)
		{
			if (targetStation != null && targetStation.IsDockable && targetStation.Faction != null)
			{
				if (!(targetStation.Faction == fleet.Faction))
				{
					return !targetStation.IsHostileToOrAlwaysHostileToTwoWay(fleet.Faction);
				}
				return true;
			}
			return false;
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			if (targetStation != null)
			{
				return "Moving to " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, targetStation);
			}
			return base.GetStatusTextInternal(localFaction);
		}

		protected override void resetTargetPosition()
		{
			if (targetStation != null && targetStation.IsValidAndNotDestroyed)
			{
				fleet.SetTargetToSectorObject(this, targetStation);
				if (!offsetFromTarget.HasValue)
				{
					offsetFromTarget = GetRelativePositionFromTarget();
				}
				fleet.NavTarget.RelativeTargetPosition = offsetFromTarget.Value;
			}
		}

		private Vector3 GetRelativePositionFromTarget()
		{
			UnityEngine.Random.InitState(fleet.UniqueId);
			float num = UnityEngine.Random.Range(150f, 300f);
			Vector3 result = Geometry.RandomXZUnitVector() * (TargetStation.UnitClass.DisplayData.Radius + num);
			UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
			return result;
		}
	}
}
