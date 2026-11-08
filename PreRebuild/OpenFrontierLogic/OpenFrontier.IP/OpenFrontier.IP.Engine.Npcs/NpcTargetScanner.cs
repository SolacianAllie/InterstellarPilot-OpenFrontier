using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Npcs
{
	public class NpcTargetScanner
	{
		private float nextTargetScanTime;

		private List<AIGroupHostileTarget> hostileTargets = new List<AIGroupHostileTarget>();

		private float nextTargetListUpdate;

		private const float timeBetweenTargetListUpdates = 10f;

		private NpcPilot npcPilot;

		private int scanProgressIndex;

		private int lastScanCount;

		private List<Unit> scannedUnitIdCache = new List<Unit>(40);

		public List<AIGroupHostileTarget> HostileTargets => hostileTargets;

		private bool IsScanInProgress => scanProgressIndex < lastScanCount;

		public NpcTargetScanner(NpcPilot npcPilot)
		{
			this.npcPilot = npcPilot;
		}

		public void Update(float elapsedTime)
		{
			UpdateTargetScanning(elapsedTime);
			if (Time.time > nextTargetListUpdate)
			{
				RemoveHostileTargets();
				nextTargetListUpdate = Time.time + 10f;
			}
		}

		public void RemoveHostileTargets()
		{
			for (int i = 0; i < hostileTargets.Count; i++)
			{
				if (hostileTargets[i].Target == null || Time.time > hostileTargets[i].StaleTime)
				{
					hostileTargets.RemoveAt(i);
					i--;
				}
			}
		}

		private float GetIntelMaxAgeOfNonStaticTarget()
		{
			if (!npcPilot.Person.IsInActiveSector)
			{
				return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime;
			}
			return GameController.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector;
		}

		public void StartScanForTargets()
		{
			lastScanCount = 0;
			scanProgressIndex = 0;
			if (npcPilot.Faction != null)
			{
				scannedUnitIdCache.Clear();
				npcPilot.Faction.Intel.GetDiscoveredUnitIdsInSectorNonAlloc(npcPilot.Sector, scannedUnitIdCache, GetIntelMaxAgeOfNonStaticTarget());
				lastScanCount = scannedUnitIdCache.Count;
			}
		}

		private void ProcessScannedTargets(float elapsedTime)
		{
			int num = lastScanCount - scanProgressIndex;
			if (num <= 0)
			{
				return;
			}
			int num2 = Mathf.Min(num, Mathf.CeilToInt(EngineASX.Instance.PerformanceSettings.NpcTargetScanProcessedUnitsPerSecond * elapsedTime));
			Faction faction = npcPilot.Faction;
			Sector sector = npcPilot.Sector;
			Vector3 ourSectorPosition = npcPilot.SectorPosition;
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

		public void NotifyHostileShipOrStationFound(Unit unit, float distance)
		{
			int num = -1;
			for (int i = 0; i < hostileTargets.Count; i++)
			{
				if (hostileTargets[i].Target == unit)
				{
					num = i;
					break;
				}
			}
			AIGroupHostileTarget aIGroupHostileTarget = new AIGroupHostileTarget
			{
				Target = unit,
				NearestDistanceWhenScanned = distance,
				StaleTime = Time.time + EngineASX.Instance.GameSettings.AIGroupStaleTargetRemoveTime
			};
			if (num == -1)
			{
				hostileTargets.Add(aIGroupHostileTarget);
			}
			else
			{
				aIGroupHostileTarget.NearestDistanceWhenScanned = Mathf.Min(hostileTargets[num].NearestDistanceWhenScanned, distance);
				hostileTargets[num] = aIGroupHostileTarget;
			}
			if (npcPilot.Faction.FactionAI != null)
			{
				npcPilot.Faction.FactionAI.NotifyScannedHostileTargetByNpc(npcPilot, unit, distance);
			}
		}

		private void UpdateTargetScanning(float elapsedTime)
		{
			if (IsScanInProgress)
			{
				ProcessScannedTargets(elapsedTime);
			}
			else
			{
				StartTargetScanPeriodically();
			}
		}

		private float GetHostileTargetScanDelay()
		{
			PerformanceSettings performanceSettings = EngineASX.Instance.PerformanceSettings;
			if (!npcPilot.Person.IsInActiveSector)
			{
				return performanceSettings.NpcTargetScanInactiveFrequency;
			}
			return performanceSettings.NpcTargetScanActiveFrequency;
		}

		private void StartTargetScanPeriodically()
		{
			if (Time.time > nextTargetScanTime)
			{
				if (GameController.Instance.GameSettings.DebugSettings.FleetlessNpcTargettingEnabled)
				{
					StartScanForTargets();
				}
				float hostileTargetScanDelay = GetHostileTargetScanDelay();
				nextTargetScanTime = Time.time + hostileTargetScanDelay;
			}
		}

		private void ProcessScannedTarget(Unit targetUnit, Faction ourFaction, Sector ourSector, ref Vector3 ourSectorPosition)
		{
			if (ourFaction == null)
			{
				Debug.LogError("Not expecting faction to be null", npcPilot);
			}
			else
			{
				if (!(targetUnit != null) || !targetUnit.IsValidAndNotDestroyed)
				{
					return;
				}
				Faction faction = targetUnit.Faction;
				if (faction != ourFaction && targetUnit.Sector == ourSector && targetUnit.IsTargettable(ourFaction))
				{
					Vector3 targetPosition = targetUnit.SectorPosition;
					if (faction != null)
					{
						ourFaction.CreateAttitudeIfNone(faction);
					}
					if (targetUnit.IsStationOrShip() && ourFaction.IsHostileTo(targetUnit))
					{
						float distance2D = GetDistance2D(ref ourSectorPosition, ref targetPosition);
						NotifyHostileShipOrStationFound(targetUnit, distance2D);
					}
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
