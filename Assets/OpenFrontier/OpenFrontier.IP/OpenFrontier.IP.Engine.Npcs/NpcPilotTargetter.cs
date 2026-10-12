using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Npcs
{
	public class NpcPilotTargetter
	{
		private struct UnitTarget
		{
			public Unit Unit;

			public float BaseScore;

			public float DistanceFromFleet;

			public UnitTarget(Unit unit, float baseScore, float distanceFromFleet)
			{
				this = default;
				Unit = unit;
				BaseScore = baseScore;
				DistanceFromFleet = distanceFromFleet;
			}
		}

		private static List<AISpecificTarget> SpecificTargetCache = new List<AISpecificTarget>();

		private float nearestHostileTargetDistance = float.MaxValue;

		private NpcPilot npcPilot;

		private bool isSearching;

		private Unit bestTarget;

		private float bestScore;

		private float bestTargetDistance;

		private bool bestTargetIsPriority;

		private Queue<UnitTarget> targetQueue = new Queue<UnitTarget>(16);

		public bool IsSearching => isSearching;

		public Unit BestTarget => bestTarget;

		public float BestTargetDistance => bestTargetDistance;

		public bool BestTargetIsPriority => bestTargetIsPriority;

		public float BestTargetScore => bestScore;

		public float NearestHostileTargetDistance => nearestHostileTargetDistance;

		public NpcPilotTargetter(NpcPilot npcPilot)
		{
			this.npcPilot = npcPilot;
		}

		private float GetIntelMaxAgeOfNonStaticTarget()
		{
			if (!npcPilot.Person.IsInActiveSector)
			{
				return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime;
			}
			return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector;
		}

		private void CheckHostileTarget(ref float bestDistance, ref Unit bestTarget, ref float bestScore, ref bool bestTargetIsPriority, ref Vector3 ourSectorPosition, ref UnitTarget target)
		{
			if (!npcPilot.IsTargetValid(target.Unit) || !npcPilot.CurrentUnit.IsHostileTo(target.Unit))
			{
				return;
			}
			Vector3 targetPosition = target.Unit.SectorPosition;
			float distance2D = GetDistance2D(ref ourSectorPosition, ref targetPosition);
			if (distance2D < nearestHostileTargetDistance)
			{
				nearestHostileTargetDistance = distance2D;
			}
			if (npcPilot.IsUnitInTargettingRange(target.Unit, distance2D))
			{
				float num = target.BaseScore + GetHostileTargetScore(target.Unit, distance2D);
				if (num > bestScore)
				{
					bestScore = num;
					bestTarget = target.Unit;
					bestDistance = distance2D;
					bestTargetIsPriority = false;
				}
			}
		}

		internal void StartNewSearch()
		{
			isSearching = true;
			bestTarget = null;
			bestScore = float.MinValue;
			bestTargetDistance = 0f;
			bestTargetIsPriority = false;
			nearestHostileTargetDistance = float.MaxValue;
			targetQueue.Clear();
			float intelMaxAgeOfNonStaticTarget = GetIntelMaxAgeOfNonStaticTarget();
			FactionIntel intel = npcPilot.Faction.Intel;
			Vector3 sectorPosition = npcPilot.SectorPosition;
			Fleet fleet = npcPilot.Fleet;
			if (fleet != null)
			{
				sectorPosition = fleet.SectorPosition;
				SpecificTargetCache.Clear();
				fleet.AddSpecificTargets(SpecificTargetCache);
				foreach (AISpecificTarget item in SpecificTargetCache)
				{
					Unit unit = item.Unit;
					if (!(unit != null) || !npcPilot.IsTargetValid(unit) || !intel.IsUnitDiscovered(unit, intelMaxAgeOfNonStaticTarget))
					{
						continue;
					}
					Vector3 ourPosition = unit.SectorPosition;
					float additionalPriority = item.AdditionalPriority;
					float distance2D = GetDistance2D(ref ourPosition, ref sectorPosition);
					if (distance2D < nearestHostileTargetDistance)
					{
						nearestHostileTargetDistance = distance2D;
					}
					if (npcPilot.IsUnitInTargettingRange(unit, distance2D))
					{
						additionalPriority += GetHostileTargetScore(unit, distance2D);
						if (additionalPriority > bestScore)
						{
							bestTargetDistance = distance2D;
							bestTarget = unit;
							bestScore = additionalPriority;
							bestTargetIsPriority = true;
						}
					}
				}
				for (int i = 0; i < fleet.HostileTargets.Count; i++)
				{
					AIGroupHostileTarget aIGroupHostileTarget = fleet.HostileTargets[i];
					targetQueue.Enqueue(new UnitTarget(aIGroupHostileTarget.Target, aIGroupHostileTarget.BaseScore, aIGroupHostileTarget.NearestDistanceWhenScanned));
				}
			}
			else
			{
				if (npcPilot.TargetScanner == null)
				{
					return;
				}
				foreach (AIGroupHostileTarget hostileTarget in npcPilot.TargetScanner.HostileTargets)
				{
					targetQueue.Enqueue(new UnitTarget(hostileTarget.Target, hostileTarget.BaseScore, hostileTarget.NearestDistanceWhenScanned));
				}
			}
		}

		public void ProcessSearch(float elapsedTime)
		{
			if (targetQueue.Count > 0)
			{
				int num = Mathf.Min(targetQueue.Count, Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.NpcPilotTargetScoresPerSecond * elapsedTime));
				for (int i = 0; i < num; i++)
				{
					UnitTarget target = targetQueue.Dequeue();
					Vector3 ourSectorPosition = npcPilot.SectorPosition;
					CheckHostileTarget(ref bestTargetDistance, ref bestTarget, ref bestScore, ref bestTargetIsPriority, ref ourSectorPosition, ref target);
				}
			}
			if (targetQueue.Count == 0)
			{
				OnSearchFinished();
			}
		}

		private void OnSearchFinished()
		{
			isSearching = false;
			if (bestTarget != null && LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Found best target: {bestTarget} Score: {bestScore}", npcPilot, 2);
			}
		}

		private static float GetDistance2D(Vector3 ourPosition, Vector3 targetPosition)
		{
			return Vector2.Distance(new Vector2(ourPosition.x, ourPosition.z), new Vector2(targetPosition.x, targetPosition.z));
		}

		private static float GetDistance2D(ref Vector3 ourPosition, ref Vector3 targetPosition)
		{
			return Vector2.Distance(new Vector2(ourPosition.x, ourPosition.z), new Vector2(targetPosition.x, targetPosition.z));
		}

		public float GetHostileTargetScore(Unit target, float targetDistance)
		{
			NpcTargetSearchSettings npcTargetSearchSettings = EngineASX.Instance.GameSettings.NpcTargetSearchSettings;
			float num = 0f;
			if (target == npcPilot.CombatTarget)
			{
				num += npcTargetSearchSettings.AITargetSearchExistingScore;
			}
			Unit currentUnit = npcPilot.CurrentUnit;
			if (npcPilot.Fleet != null)
			{
				num = DecreaseHostileTargetScoreBasedOnCombatRating(target, npcTargetSearchSettings, num);
			}
			if (targetDistance > npcTargetSearchSettings.AITargetSearchNegativeScoreLowerDistance)
			{
				float num2 = npcTargetSearchSettings.AITargetSearchNegativeDistanceScore * (targetDistance - npcTargetSearchSettings.AITargetSearchNegativeScoreLowerDistance) / (npcTargetSearchSettings.AITargetSearchNegativeScoreUpperDistance - npcTargetSearchSettings.AITargetSearchNegativeScoreLowerDistance);
				num += num2;
			}
			else
			{
				num += Mathf.Clamp01((targetDistance - 50f) / (npcTargetSearchSettings.AITargetSearchNegativeScoreLowerDistance - 50f)) * npcTargetSearchSettings.AITargetSearchNegativeDistanceScore;
			}
			if (targetDistance < 500f)
			{
				Vector3 rhs = Vector3.Normalize(target.SectorPosition - currentUnit.SectorPosition);
				float num3 = Vector3.Dot(currentUnit.transform.forward, rhs);
				if (num3 > 0f)
				{
					num += npcTargetSearchSettings.AITargetSearchInFrontScore * num3;
				}
			}
			if (target.IsArmed)
			{
				if (target.UnitClass.IsTurret)
				{
					num += npcTargetSearchSettings.AITargetSearchTurretScore;
				}
				num += npcTargetSearchSettings.AITargetSearchArmedScore;
				if (target.Components != null && target.Components.PilotPerson != null)
				{
					num += npcTargetSearchSettings.AITargetSearchArmedPilottedScore;
				}
			}
			if (target.UnitClass.HullType == currentUnit.UnitClass.HullType)
			{
				num += npcTargetSearchSettings.AITargetSearchClassScore;
			}
			if (EngineASX.Instance.UnitRecentAttacksLogsByInflictor.IsUnitAttackingUnit(target, currentUnit))
			{
				num += npcTargetSearchSettings.AITargetSearchRecentAttacksBaseScore;
			}
			num += Random.value * npcTargetSearchSettings.AITargetSearchScoreRandomness;
			if (num < 0f)
			{
				return 0f;
			}
			return num;
		}

		private float DecreaseHostileTargetScoreBasedOnCombatRating(Unit target, NpcTargetSearchSettings gs, float curScore)
		{
			float targetCombatRating = GetTargetCombatRating(target);
			float simpleFleetProbabilityAgainstRating = AICombatProbabilityCalculator.GetSimpleFleetProbabilityAgainstRating(npcPilot.Fleet, targetCombatRating);
			float num = 0.5f;
			if (simpleFleetProbabilityAgainstRating < num)
			{
				float num2 = (num - simpleFleetProbabilityAgainstRating) / num;
				curScore += gs.AITargetSearchLowCombatRatingNegativeScore * num2;
			}
			return curScore;
		}

		private float GetTargetCombatRating(Unit target)
		{
			Fleet fleet = target.GetFleet();
			if (fleet != null)
			{
				return fleet.GetCachedSimpleCombatRating();
			}
			return target.CombatRating;
		}
	}
}
