using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Npcs
{
	public class FleetTargetScanner
	{
		private float nextTargetScanTime;

		private List<AIGroupHostileTarget> hostileTargets = new List<AIGroupHostileTarget>();

		private List<AIGroupHostileTarget> hostileTargetsStaging = new List<AIGroupHostileTarget>();

		private const float timeBetweenTargetListUpdates = 10f;

		private Fleet fleet;

		private int scanProgressIndex;

		private int lastScanCount;

		private List<Unit> scannedUnitIdCache = new List<Unit>(40);

		public List<AIGroupHostileTarget> HostileTargets => hostileTargets;

		private bool IsScanInProgress => scanProgressIndex < lastScanCount;

		public FleetTargetScanner(Fleet fleet)
		{
			this.fleet = fleet;
		}

		public void Update(float elapsedTime)
		{
			UpdateTargetScanning(elapsedTime);
		}

		private float GetIntelMaxAgeOfNonStaticTarget()
		{
			if (!fleet.IsInActiveSector)
			{
				return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime;
			}
			return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector;
		}

		public void StartScanForTargets()
		{
			lastScanCount = 0;
			scanProgressIndex = 0;
			hostileTargetsStaging.Clear();
			if (fleet.Faction != null)
			{
				scannedUnitIdCache.Clear();
				fleet.Faction.Intel.GetDiscoveredUnitIdsInSectorNonAlloc(fleet.Sector, scannedUnitIdCache, GetIntelMaxAgeOfNonStaticTarget());
				lastScanCount = scannedUnitIdCache.Count;
			}
		}

		public void PerformScanImmediate()
		{
			StartScanForTargets();
			while (IsScanInProgress)
			{
				ProcessScannedTargets(0.1f);
			}
			OnScanFinished();
		}

		private void ProcessScannedTargets(float elapsedTime)
		{
			int num = lastScanCount - scanProgressIndex;
			if (num <= 0)
			{
				return;
			}
			int num2 = Mathf.Min(num, Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.NpcTargetScanProcessedUnitsPerSecond * elapsedTime));
			Faction faction = fleet.Faction;
			Sector sector = fleet.Sector;
			Vector3 ourSectorPosition = fleet.SectorPosition;
			for (int i = 0; i < num2; i++)
			{
				if (scanProgressIndex < scannedUnitIdCache.Count)
				{
					Unit targetUnit = scannedUnitIdCache[scanProgressIndex];
					ProcessScannedTarget(targetUnit, faction, sector, ref ourSectorPosition);
				}
				scanProgressIndex++;
			}
		}

		public void NotifyHostileShipOrStationFound(Unit unit, float distanceFromFleet)
		{
			AIGroupHostileTarget item = new AIGroupHostileTarget
			{
				Target = unit,
				NearestDistanceWhenScanned = distanceFromFleet,
				StaleTime = Time.time + EngineASX.Instance.GameSettings.AIGroupStaleTargetRemoveTime,
				BaseScore = 0f
			};
			if (unit.IsAttackingFleet(fleet))
			{
				item.BaseScore += EngineASX.Instance.GameSettings.NpcTargetSearchSettings.AITargetSearchAttackingFleetScore;
			}
			else if (unit.IsAttackingFaction(fleet.Faction))
			{
				item.BaseScore += EngineASX.Instance.GameSettings.NpcTargetSearchSettings.AITargetSearchAttackingFactionScore;
			}
			item.BaseScore += fleet.GetAdditionalCombatTargetPriority(unit);
			hostileTargetsStaging.Add(item);
			if (fleet.Faction.FactionAI != null)
			{
				fleet.Faction.FactionAI.NotifyScannedHostileTargetByFleet(fleet, unit, distanceFromFleet);
			}
		}

		private void UpdateTargetScanning(float elapsedTime)
		{
			if (IsScanInProgress)
			{
				ProcessScannedTargets(elapsedTime);
				if (!IsScanInProgress)
				{
					OnScanFinished();
				}
			}
			else
			{
				StartTargetScanPeriodically();
			}
		}

		private void OnScanFinished()
		{
			hostileTargets.Clear();
			hostileTargets.AddRange(hostileTargetsStaging);
		}

		private float GetHostileTargetScanDelay()
		{
			PerformanceSettings performanceSettings = EngineASX.Instance.PerformanceSettings;
			if (!fleet.IsInActiveSector)
			{
				return performanceSettings.NpcTargetScanInactiveFrequency;
			}
			return performanceSettings.NpcTargetScanActiveFrequency;
		}

		private void StartTargetScanPeriodically()
		{
			if (Time.time > nextTargetScanTime)
			{
				if (GameController.Instance.GameSettings.DebugSettings.FleetTargettingEnabled)
				{
					StartScanForTargets();
				}
				float hostileTargetScanDelay = GetHostileTargetScanDelay();
				nextTargetScanTime = Time.time + hostileTargetScanDelay;
			}
		}

		public void SetNextTargetScanTime()
		{
			float npcTargetScanActiveFrequency = EngineASX.Instance.PerformanceSettings.NpcTargetScanActiveFrequency;
			nextTargetScanTime = Time.time + Random.value * npcTargetScanActiveFrequency;
		}

		private void ProcessScannedTarget(Unit targetUnit, Faction ourFaction, Sector ourSector, ref Vector3 ourSectorPosition)
		{
			if (!(targetUnit != null))
			{
				return;
			}
			UnitType unitType = targetUnit.UnitType;
			if ((uint)(unitType - 1) > 1u)
			{
				return;
			}
			Faction faction = targetUnit.Faction;
			if (faction != null && faction != ourFaction && targetUnit.Sector == ourSector && targetUnit.IsTargettableQuick())
			{
				Vector3 targetPosition = targetUnit.SectorPosition;
				FactionAttitude attitude = ourFaction.GetAttitude(faction);
				if (attitude != null && attitude.Neutrality == Neutrality.Hostile)
				{
					float distance2D = GetDistance2D(ref ourSectorPosition, ref targetPosition);
					NotifyHostileShipOrStationFound(targetUnit, distance2D);
				}
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
	}
}
