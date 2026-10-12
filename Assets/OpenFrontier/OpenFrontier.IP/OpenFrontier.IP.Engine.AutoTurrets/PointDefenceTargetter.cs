using System.Collections.Generic;
using OpenFrontier.IP.Engine.Settings;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AutoTurrets
{
	public class PointDefenceTargetter
	{
		private struct ProjectileTarget
		{
			public Projectile Projectile;

			public float BaseScore;

			public Unit Unit;

			public ProjectileTarget(Projectile projectile, float baseScore)
			{
				this = default;
				Projectile = projectile;
				BaseScore = baseScore;
				Unit = Projectile.Unit;
			}
		}

		private PointDefenceAutoTurret autoTurret;

		private bool isSearching;

		private Unit bestTarget;

		private float bestScore;

		private Queue<ProjectileTarget> targetQueue = new Queue<ProjectileTarget>(16);

		public bool IsSearching => isSearching;

		public Unit BestTarget => bestTarget;

		public float BestTargetScore => bestScore;

		public PointDefenceTargetter(PointDefenceAutoTurret autoTurret)
		{
			this.autoTurret = autoTurret;
		}

		private void CheckHostileTarget(ref Unit bestTarget, ref float bestScore, ref Vector3 ourSectorPosition, ref ProjectileTarget target)
		{
			if (!autoTurret.IsTargetValid(target.Unit, ourSectorPosition) || !autoTurret.Unit.IsHostileTo(target.Unit))
			{
				return;
			}
			Vector3 targetPosition = target.Unit.SectorPosition;
			float distance2D = GetDistance2D(ref ourSectorPosition, ref targetPosition);
			if (!autoTurret.TurretComponent.IsTargetInFiringRange(target.Unit, out var _) || !autoTurret.TurretComponent.IsPositionInFiringArc(target.Unit.transform.position))
			{
				return;
			}
			float? hostileTargetScore = GetHostileTargetScore(target.Unit, ourSectorPosition, distance2D);
			if (hostileTargetScore.HasValue)
			{
				float num = target.BaseScore + hostileTargetScore.Value;
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
			if (autoTurret.ComponentHolder.PointDefenceTurretModule == null)
			{
				return;
			}
			foreach (Projectile hostileTarget in autoTurret.ComponentHolder.PointDefenceTurretModule.HostileTargets)
			{
				bool flag = hostileTarget.IsMissile && hostileTarget.Missile.IsLockedOnTo(autoTurret.Unit);
				targetQueue.Enqueue(new ProjectileTarget(hostileTarget, flag ? 100f : 0f));
			}
		}

		public void ProcessSearch(float elapsedTime)
		{
			if (targetQueue.Count > 0)
			{
				int num = Mathf.Min(targetQueue.Count, Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.AutoTurretTargetScoresPerSecond * elapsedTime));
				for (int i = 0; i < num; i++)
				{
					ProjectileTarget target = targetQueue.Dequeue();
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

		public float? GetHostileTargetScore(Unit target, Vector3 ourSectorPosition, float targetDistance)
		{
			if (Vector3.Dot(Vector3.Normalize(ourSectorPosition - target.SectorPosition), target.transform.forward) < 0.6f)
			{
				return null;
			}
			AutoTurretTargetSearchSettings autoTurretTargetSearchSettings = EngineASX.Instance.GameSettings.AutoTurretTargetSearchSettings;
			return 0f - Mathf.Clamp01(targetDistance / autoTurretTargetSearchSettings.ReferenceMaxDistance) * autoTurretTargetSearchSettings.DistanceScore;
		}
	}
}
