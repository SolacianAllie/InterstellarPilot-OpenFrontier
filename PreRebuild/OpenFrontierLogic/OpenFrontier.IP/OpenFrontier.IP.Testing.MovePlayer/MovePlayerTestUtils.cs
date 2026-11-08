using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Testing.MovePlayer
{
	public static class MovePlayerTestUtils
	{
		public static void MoveLocalUnitToSectorPosition(Sector sector, Vector3 newSectorPosition)
		{
			Unit playerUnit = EngineASX.Instance.PlayerUnit;
			if (!MoveUtils.CanMoveUnitToSector(playerUnit, sector, out var errorMessage))
			{
				throw new Exception("Cannot move: $" + errorMessage);
			}
			Sector sector2 = playerUnit.Sector;
			playerUnit.UndockIfDocked();
			if (sector != EngineASX.Instance.ActiveSector)
			{
				EngineASX.Instance.CreateIntroFader();
				EngineASX.Instance.ActiveSector = null;
				playerUnit.Sector = sector;
				playerUnit.transform.localPosition = newSectorPosition;
				playerUnit.RemoveForcesAndControlInputs();
				playerUnit.transform.rotation = Quaternion.identity;
				UIController.Instance.ScreenNavigator.RemoveAllInStack(null);
				UIController.Instance.QuickMsg.ClearMessages();
				EngineASX.Instance.ActiveSector = playerUnit.Sector;
				EngineASX.Instance.SetUIFromPlayerStatus();
				if (EngineASX.Instance.HudCamera.isActiveAndEnabled)
				{
					EngineASX.Instance.SnapHudCameraToTarget();
				}
				MoveUtils.OnUnitMoved(playerUnit, sector2);
			}
			else
			{
				playerUnit.transform.localPosition = newSectorPosition;
				playerUnit.RemoveForcesAndControlInputs();
				playerUnit.transform.rotation = Quaternion.identity;
				MoveUtils.OnUnitMoved(playerUnit, sector2);
			}
		}

		public static void MoveToNearestStationOfPurpose(StationPurpose stationPurpose)
		{
			Unit playerUnit = EngineASX.Instance.PlayerUnit;
			Unit nearestUnit = null;
			float nearestDistance = 0f;
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				float num = 0f;
				num = ((!(unit.Sector == playerUnit.Sector)) ? ((float)unit.Sector.GetJumpDistanceTo(playerUnit.Sector) * EngineASX.Instance.World.GateDistance) : (num + Vector3.Distance(playerUnit.transform.position, unit.transform.position)));
				if (nearestUnit == null || num < nearestDistance)
				{
					nearestDistance = num;
					nearestUnit = unit;
				}
			}, (Unit unit) => unit != null && unit.UnitType == UnitType.Station && unit.IsValidAndNotDestroyed && unit.UnitClass.StationPurpose == stationPurpose);
			if (nearestUnit != null)
			{
				MoveLocalUnitToSectorPosition(nearestUnit.Sector, nearestUnit.SectorPosition);
			}
		}

		public static void MoveToRandomShipUnderAttack()
		{
			MoveToRandomUnit((Unit unit) => unit.UnitType == UnitType.Ship && unit.IsUnderAttack());
		}

		public static void MoveToRandomForeignCloakedShip()
		{
			MoveToRandomUnit((Unit unit) => unit.UnitType == UnitType.Ship && unit.IsFullyCloaked);
		}

		public static void MoveToRandomUnitUnderAttack()
		{
			MoveToRandomUnit((Unit unit) => unit.IsUnderAttack());
		}

		public static void MoveToRandomLab()
		{
			MoveToRandomUnit((Unit unit) => unit.UnitType == UnitType.Station && unit.UnitClass.StationPurpose == StationPurpose.Factory && unit.ClassName.Contains("Lab", StringComparison.InvariantCultureIgnoreCase));
		}

		public static void MoveToRandomBar()
		{
			MoveToRandomUnit((Unit unit) => unit.UnitType == UnitType.Station && unit.UnitClass.StationPurpose == StationPurpose.Bar);
		}

		public static void MoveToRandomShipOfFactionType(FactionType factionType)
		{
			Unit randomShipFromFactionType = GetRandomShipFromFactionType(factionType);
			if (randomShipFromFactionType != null)
			{
				MoveLocalUnitToSectorPosition(randomShipFromFactionType.Sector, randomShipFromFactionType.SectorPosition + Vector3.back * 100f);
			}
		}

		public static void MoveToUnitUnderConstruction()
		{
			MoveToRandomUnit((Unit unit) => unit.UnitType == UnitType.Station && unit.IsUnderConstruction);
		}

		public static void MoveToRandomNonGenericBanditShip()
		{
			Unit randomShipFromFactionType = GetRandomShipFromFactionType(FactionType.Bandit, (Unit e) => e.Faction != null && e.Faction.ShortName != "Bandits");
			if (randomShipFromFactionType != null)
			{
				MoveLocalUnitToSectorPosition(randomShipFromFactionType.Sector, randomShipFromFactionType.SectorPosition + Vector3.back * 100f);
			}
		}

		public static void MoveToRandomShipOfFactionStrategy(FactionStrategy factionStrategy)
		{
			Unit randomShipFromFactionStrategy = GetRandomShipFromFactionStrategy(factionStrategy);
			if (randomShipFromFactionStrategy != null)
			{
				MoveLocalUnitToSectorPosition(randomShipFromFactionStrategy.Sector, randomShipFromFactionStrategy.SectorPosition + Vector3.back * 100f);
			}
		}

		public static void MoveToRandomUnitOfType(UnitType unitType)
		{
			MovePlayerToUnit(GetRandomUnitOfType(unitType));
		}

		public static void MoveToRandomUnit(Func<Unit, bool> predicate)
		{
			Unit randomUnit = GetRandomUnit(predicate);
			if (randomUnit != null)
			{
				MoveLocalUnitToSectorPosition(randomUnit.Sector, randomUnit.SectorPosition - Vector3.back * 200f);
			}
		}

		public static void MovePlayerToUnit(Unit unit)
		{
			if (unit != null)
			{
				float num = EngineASX.Instance.PlayerUnit.Radius + unit.Radius + 5f;
				MoveLocalUnitToSectorPosition(unit.Sector, unit.SectorPosition - Vector3.back * num);
			}
		}

		public static void MoveToAbandonedShip()
		{
			MoveToRandomUnit((Unit unit) => unit.UnitType == UnitType.Ship && unit.Faction == null);
		}

		public static void MoveToRandomStationPurpose(StationPurpose stationPurpose)
		{
			Unit randomUnitWithStationPurpose = GetRandomUnitWithStationPurpose(stationPurpose);
			if (randomUnitWithStationPurpose != null)
			{
				MoveLocalUnitToSectorPosition(randomUnitWithStationPurpose.Sector, randomUnitWithStationPurpose.SectorPosition - Vector3.back * 200f);
			}
		}

		public static void MoveLocalUnitToCurrentTarget()
		{
			Unit currentTarget = EngineASX.Instance.Hud.CurrentTarget;
			if (!(currentTarget != null))
			{
				return;
			}
			Sector componentInParent = currentTarget.GetComponentInParent<Sector>();
			if (componentInParent != null)
			{
				float num = 100f;
				if (currentTarget.GetComponent<Wormhole>() != null)
				{
					num = 300f;
				}
				MoveLocalUnitToSectorPosition(componentInParent, componentInParent.ToLocalPosition(currentTarget.transform.position + Vector3.back * num));
			}
		}

		public static Unit GetRandomUnitWithStationPurpose(StationPurpose stationPurpose)
		{
			List<Unit> list = new List<Unit>(80);
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				list.Add(unit);
			}, (Unit unit) => unit != null && unit.UnitType == UnitType.Station && unit.UnitClass.StationPurpose == stationPurpose && unit != EngineASX.Instance.LocalUnit);
			return list.GetRandom();
		}

		public static Unit GetRandomUnitOfType(UnitType unitType)
		{
			List<Unit> list = new List<Unit>(80);
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				list.Add(unit);
			}, (Unit unit) => unit != null && unit.UnitType == unitType && unit != EngineASX.Instance.LocalUnit);
			return list.GetRandom();
		}

		public static Unit GetRandomUnit(Func<Unit, bool> predicate)
		{
			List<Unit> list = new List<Unit>(80);
			EngineASX.Instance.EnumerateUnitsWithPredicate((Unit unit) =>
			{
				list.Add(unit);
			}, (Unit unit) => predicate(unit) && unit != EngineASX.Instance.LocalUnit);
			return list.GetRandom();
		}

		public static Unit GetRandomShipFromFactionType(FactionType factionType, Func<Unit, bool> predicate = null)
		{
			_ = EngineASX.Instance.PlayerUnit;
			List<Unit> list = new List<Unit>(100);
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.Faction != null && item.Faction.FactionType == factionType && (predicate == null || predicate(item)))
					{
						list.Add(item);
					}
				}
			}
			return list.GetRandom();
		}

		public static Unit GetRandomShipFromFactionStrategy(FactionStrategy factionStrategy)
		{
			List<Unit> list = new List<Unit>(100);
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.Faction != null && item.GetFleet() != null && item.GetFleet().FleetStrategy == factionStrategy)
					{
						list.Add(item);
					}
				}
			}
			return list.GetRandom();
		}

		public static void MoveLocalUnitToAnotherSector()
		{
			Sector random = EngineASX.Instance.Sectors.Where((Sector e) => e != EngineASX.Instance.ActiveSector).GetRandom();
			if (random != null)
			{
				MoveLocalUnitToSectorPosition(random, random.GetRandomSafeDeploymentSectorPosition(0.8f, 100f));
				return;
			}
			throw new Exception("Couldn't find another sector");
		}

		public static void MoveLocalUnitToFringeSector()
		{
			Sector random = EngineASX.Instance.Sectors.Where((Sector e) => e != EngineASX.Instance.ActiveSector && e.FringeSectorRating > 0.125f).GetRandom();
			if (random != null)
			{
				MoveLocalUnitToSectorPosition(random, random.GetRandomSafeDeploymentSectorPosition(0.8f, 100f));
				return;
			}
			throw new Exception("Couldn't find another sector");
		}

		public static void MoveLocalUnitToPlanetSector()
		{
			Sector random = EngineASX.Instance.Sectors.Where((Sector e) => e != EngineASX.Instance.ActiveSector && (e.SectorType & SectorType.Planet) != 0).GetRandom();
			if (random != null)
			{
				MoveLocalUnitToSectorPosition(random, random.GetRandomSafeDeploymentSectorPosition(0.8f, 100f));
				return;
			}
			throw new Exception("Couldn't find another sector");
		}
	}
}
