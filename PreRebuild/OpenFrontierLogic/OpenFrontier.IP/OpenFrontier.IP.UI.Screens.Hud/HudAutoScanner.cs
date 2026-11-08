using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class HudAutoScanner : MonoBehaviour
	{
		public delegate void ScanCompleteHandler(HudAutoScanner sender);

		private List<HudScannerUnit> scannedUnitCache = new List<HudScannerUnit>(100);

		private TargetScanResult lastScanResult;

		public float AutoSearchTargetsInterval = 2f;

		public HudScanner HudScanner;

		public HudScreen HudScreen;

		private double nextAutoSearchTargetsTime;

		public TargetScanResult LastScanResult => lastScanResult;

		public List<HudScannerUnit> ScannedUnitCache => scannedUnitCache;

		public event ScanCompleteHandler ScanComplete;

		private void Update()
		{
			Unit playerUnit = EngineASX.Instance.PlayerUnit;
			if (playerUnit != null && playerUnit.ActiveUnit != null)
			{
				UpdateTargetting(playerUnit);
			}
		}

		public void UpdateTargetting(Unit playerUnit)
		{
			if (Time.realtimeSinceStartupAsDouble > nextAutoSearchTargetsTime)
			{
				PerformTargetScan(playerUnit);
				nextAutoSearchTargetsTime = Time.realtimeSinceStartupAsDouble + (double)AutoSearchTargetsInterval;
			}
			ClearCurrentTargetWhenInvalid(playerUnit);
		}

		private void ClearCurrentTargetWhenInvalid(Unit playerUnit)
		{
			if (HudScreen.CurrentTarget != null)
			{
				HudScreen.ClearTargetIfInvalid();
			}
		}

		private void PerformTargetScan(Unit playerUnit)
		{
			if (!playerUnit.IsDocked)
			{
				lastScanResult = PerformTargetScan(GameController.Instance.SelectableMask, scannedUnitCache);
				if (lastScanResult.HostileTargetsInCombat)
				{
					EngineASX.Instance.NotifyPlayerInCombat();
				}
			}
			else
			{
				lastScanResult = default;
			}
			if (ScanComplete != null)
			{
				ScanComplete(this);
			}
		}

		public TargetScanResult PerformTargetScan(LayerMask layerMask, List<HudScannerUnit> cache)
		{
			TargetScanResult result = default;
			Faction localFaction = EngineASX.Instance.LocalFaction;
			HudScanner.PopulateSceneUnitCache(layerMask, cache);
			foreach (HudScannerUnit item in cache)
			{
				Unit unit = item.Unit;
				result.UnitTypeMask |= unit.UnitType;
				switch (unit.UnitType)
				{
				case UnitType.Wormhole:
					result.GateTargets = true;
					break;
				case UnitType.Station:
					result.StationTargets = true;
					break;
				case UnitType.Ship:
					result.ShipTargets = true;
					break;
				}
				if (unit.UnitType == UnitType.Cargo)
				{
					result.CargoTargets = true;
					if (unit.Faction == null || unit.Faction == localFaction)
					{
						result.TractorableCargoCount++;
					}
				}
				else if (unit.UnitType != UnitType.NavBuoy && unit.UnitType != UnitType.None)
				{
					result.NonCargoCount++;
				}
				if (localFaction != null && unit.Faction != localFaction && unit.IsHostileTo(localFaction))
				{
					if (unit.IsStationOrShip())
					{
						result.HostileShipAndStationCount++;
					}
					if (unit.IsArmed)
					{
						if (!result.HostileTargetsInCombat && item.DistanceFromLocalUnitIgnoringY < GameController.Instance.GameSettings.GameplaySettings.NotifyPlayerTargettedMaxDistanceToTriggerInCombat)
						{
							result.HostileTargetsInCombat = true;
						}
						if (unit.NpcPilot != null && unit.NpcPilot.HasCombatTargetOrGroupInCombat && unit.NpcPilot.CombatTarget != null && unit.NpcPilot.CombatTarget.IsOwnedByPlayer)
						{
							result.HostileTargetsInCombatTargettingPlayerFaction = true;
						}
					}
					result.HostileTargets = true;
				}
				else if (unit.IsOwnedByPlayer)
				{
					result.OwnedTargets = true;
				}
			}
			return result;
		}

		public bool IsUnitInScanCache(Unit unit)
		{
			foreach (HudScannerUnit item in scannedUnitCache)
			{
				if (item.Unit == unit)
				{
					return true;
				}
			}
			return false;
		}

		public void ClearTargetCache()
		{
			scannedUnitCache.Clear();
		}
	}
}
