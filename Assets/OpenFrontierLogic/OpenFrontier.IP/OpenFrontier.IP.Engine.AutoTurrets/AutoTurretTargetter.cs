using System.Collections.Generic;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AutoTurrets
{
	public class AutoTurretTargetter
	{
		private struct UnitTarget
		{
			public Unit Unit;

			public float BaseScore;

			public UnitTarget(Unit unit, float baseScore)
			{
				this = default;
				Unit = unit;
				BaseScore = baseScore;
			}
		}

		private AutoTurret autoTurret;

		private bool isSearching;

		private Unit bestTarget;

		private float bestScore;

		private Queue<UnitTarget> targetQueue = new Queue<UnitTarget>(16);

		public bool IsSearching => isSearching;

		public Unit BestTarget => bestTarget;

		public float BestTargetScore => bestScore;

		public AutoTurretTargetter(AutoTurret autoTurret)
		{
			this.autoTurret = autoTurret;
		}

		private void CheckHostileTarget(ref Unit bestTarget, ref float bestScore, ref Vector3 ourSectorPosition, ref UnitTarget target)
		{
			if (!autoTurret.IsTargetValid(target.Unit) || !autoTurret.Unit.IsHostileTo(target.Unit))
			{
				return;
			}
			Vector3 targetPosition = target.Unit.SectorPosition;
			float distance2D = GetDistance2D(ref ourSectorPosition, ref targetPosition);
			if (autoTurret.TurretComponent.IsTargetInFiringRange(target.Unit, out var _) && autoTurret.TurretComponent.IsPositionInFiringArc(target.Unit.transform.position))
			{
				float num = target.BaseScore + GetHostileTargetScore(target.Unit, distance2D);
				if (num > bestScore)
				{
					bestScore = num;
					bestTarget = target.Unit;
				}
			}
		}

		internal void StartNewSearch()
		{
			isSearching = true;
			bestTarget = null;
			bestScore = float.MinValue;
			targetQueue.Clear();
			if (autoTurret.ComponentHolder.AutoTurretModule == null)
			{
				return;
			}
			foreach (Unit hostileTarget in autoTurret.ComponentHolder.AutoTurretModule.HostileTargets)
			{
				targetQueue.Enqueue(new UnitTarget(hostileTarget, 0f));
			}
		}

		public void ProcessSearch(float elapsedTime)
		{
			if (targetQueue.Count > 0)
			{
				int num = Mathf.Min(targetQueue.Count, Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.AutoTurretTargetScoresPerSecond * elapsedTime));
				for (int i = 0; i < num; i++)
				{
					UnitTarget target = targetQueue.Dequeue();
					Vector3 ourSectorPosition = autoTurret.Unit.SectorPosition;
					CheckHostileTarget(ref bestTarget, ref bestScore, ref ourSectorPosition, ref target);
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
				LogWrapper.Log($"{this}: AutoTurret Found best target: {bestTarget} Score: {bestScore}", autoTurret.Unit, 2);
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
			AutoTurretTargetSearchSettings autoTurretTargetSearchSettings = EngineASX.Instance.GameSettings.AutoTurretTargetSearchSettings;
			float num = 0f;
			if (target == autoTurret.ComponentHolder.AutoTurretModule.PreferredTurretTarget)
			{
				num = autoTurretTargetSearchSettings.PreferredTurretTargetScore;
			}
			return num - Mathf.Clamp01(targetDistance / autoTurretTargetSearchSettings.ReferenceMaxDistance) * autoTurretTargetSearchSettings.DistanceScore;
		}
	}
}
