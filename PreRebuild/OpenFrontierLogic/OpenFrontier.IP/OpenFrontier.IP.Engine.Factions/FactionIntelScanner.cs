using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionIntelScanner : MonoBehaviour
	{
		private Faction faction;

		private float lastScanStartTime;

		private Queue<Unit> scanQueue = new Queue<Unit>(100);

		public float LastScanStartTime
		{
			get
			{
				return lastScanStartTime;
			}
			set
			{
				lastScanStartTime = value;
			}
		}

		public void Init(Faction faction)
		{
			this.faction = faction;
		}

		internal void OnNewUnitScanned(Unit scanner, Unit scanned, DiscoverUnitResult discoverUnitResult)
		{
			if (faction.IsAIFactionType)
			{
				if (faction.FactionAI == null)
				{
					Debug.LogError("Expecting faction AI component", faction);
				}
				else
				{
					faction.FactionAI.OnNewUnitScanned(scanner, scanned, discoverUnitResult);
				}
			}
			else if (faction.FactionType == FactionType.Player)
			{
				if (scanned.IsHostileTo(EngineASX.Instance.LocalFaction))
				{
					EngineASX.Instance.OnPlayerScannedNewHostile(scanner, scanned, discoverUnitResult);
				}
				else if (scanned.Faction == null)
				{
					EngineASX.Instance.OnPlayerScannedNewAbandonedUnit(scanner, scanned, discoverUnitResult);
				}
			}
		}

		public void PerformFullScanImmediate()
		{
			StartScan();
			while (scanQueue.Count > 0)
			{
				ScanNextInQueue();
			}
		}

		private void ScanNextInQueue()
		{
			Unit unit = scanQueue.Dequeue();
			if (CanScanFromUnit(unit))
			{
				faction.Intel.PerformScan(unit.Sector, unit.SectorPosition, unit.Components.ScanRange + unit.UnitClass.ShieldRingRadius, unit);
			}
		}

		private bool CanScanFromUnit(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed && !unit.IsUnderConstructionOrDismantling && !unit.IsDocked && unit.Components != null && unit.Components.ScanRange > 0f && Time.time > unit.LastTimeEnteredWormhole + GameController.Instance.GameSettings.GameplaySettings.WormholeEntryScanCooldownTime)
			{
				return true;
			}
			return false;
		}

		private void StartScan()
		{
			scanQueue.Clear();
			List<Unit> unitsByType = faction.GetUnitsByType(UnitType.Ship);
			if (unitsByType != null)
			{
				foreach (Unit item in unitsByType)
				{
					scanQueue.Enqueue(item);
				}
			}
			List<Unit> unitsByType2 = faction.GetUnitsByType(UnitType.Station);
			if (unitsByType2 != null)
			{
				foreach (Unit item2 in unitsByType2)
				{
					scanQueue.Enqueue(item2);
				}
			}
			if (scanQueue.Count > 0)
			{
				lastScanStartTime = Time.time;
			}
		}
	}
}
