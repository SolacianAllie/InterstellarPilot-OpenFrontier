using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionIntelProcessor
	{
		private struct QueueItem
		{
			public Unit Detected;

			public Unit Scanner;

			public float Distance;

			public QueueItem(Unit detected, Unit scanner, float distance)
			{
				this = default;
				Detected = detected;
				Scanner = scanner;
				Distance = distance;
			}
		}

		private Queue<QueueItem> scanQueue = new Queue<QueueItem>(30);

		private Dictionary<int, float> queuedUnitIdsWithDistance = new Dictionary<int, float>(30);

		private Faction faction;

		public FactionIntelProcessor(Faction faction)
		{
			this.faction = faction;
		}

		public void Update()
		{
			if (scanQueue.Count > 0)
			{
				int num = Mathf.Min(scanQueue.Count, Mathf.CeilToInt((float)EngineASX.Instance.PerformanceSettings.FactionIntelMaxScansProcessedPerSecond * Time.deltaTime));
				for (int i = 0; i < num; i++)
				{
					ScanNextInQueue();
				}
			}
		}

		public void QueueUnitForProcessing(Unit detectedUnit, Unit scannerUnit)
		{
			Vector3 p = detectedUnit.SectorPosition;
			Vector3 p2 = scannerUnit.SectorPosition;
			float distanceIgnoreY = Maths.GetDistanceIgnoreY(ref p, ref p2);
			float value;
			if (scanQueue.Count > 1000)
			{
				ProcessScannedItem(detectedUnit, scannerUnit, distanceIgnoreY);
			}
			else if (!queuedUnitIdsWithDistance.TryGetValue(detectedUnit.UniqueId, out value) || distanceIgnoreY < value)
			{
				queuedUnitIdsWithDistance[detectedUnit.UniqueId] = distanceIgnoreY;
				scanQueue.Enqueue(new QueueItem(detectedUnit, scannerUnit, distanceIgnoreY));
			}
		}

		private void ScanNextInQueue()
		{
			QueueItem queueItem = scanQueue.Dequeue();
			Unit detected = queueItem.Detected;
			Unit scanner = queueItem.Scanner;
			queuedUnitIdsWithDistance.Remove(detected.UniqueId);
			ProcessScannedItem(detected, scanner, queueItem.Distance);
		}

		public static bool IsTargetOutOfDetectionRange(Unit scanner, Unit unit, float dist)
		{
			float num = unit.GetDetectionRange() + scanner.Components.ScanRange + unit.Radius + scanner.Radius;
			if (dist > num)
			{
				return true;
			}
			return false;
		}

		public void ProcessScannedItem(Unit unit, Unit scanner, float dist, bool callOnNewUnitScanned = true)
		{
			if (!(unit == null) && (!(unit.Faction != null) || !unit.Faction.IsIgnoredByAI) && (!unit.IsFullyCloaked || GameController.Instance.GameSettings.GameplaySettings.CanDetectCloakedUnits) && !(Time.time < unit.LastTimeEnteredWormhole + GameController.Instance.GameSettings.GameplaySettings.WormholeEntryScanCooldownTime) && scanner != null && unit.Faction != faction && unit.Sector == scanner.Sector && !IsTargetOutOfDetectionRange(scanner, unit, dist))
			{
				DiscoverUnitResult discoverUnitResult = faction.Intel.DiscoverUnit(unit);
				if ((discoverUnitResult != DiscoverUnitResult.Ignored) & callOnNewUnitScanned)
				{
					faction.IntelScanner.OnNewUnitScanned(scanner, unit, discoverUnitResult);
				}
			}
		}

		internal void ProcessAll()
		{
			while (scanQueue.Count > 0)
			{
				ScanNextInQueue();
			}
		}
	}
}
