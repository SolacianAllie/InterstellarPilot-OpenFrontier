using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class UnitExtensions
	{
		private static List<UnitComponentHolder> dockedShipCache = new List<UnitComponentHolder>();

		public static int GetCountOfStationPurposeWithinRadius(this Unit unit, StationPurpose stationPurpose)
		{
			return EngineASX.Instance.CountUnitsInArea(unit.Sector, unit.SectorPosition, unit.Radius, GameController.Instance.StationsMask, (Unit unit2) => unit2.UnitClass.StationPurpose == stationPurpose);
		}

		public static bool IsOwnedByPlayerOrAllied(this Unit unit)
		{
			return unit.IsOwnedByFactionOrAlliedTo(EngineASX.Instance.LocalFaction);
		}

		public static bool IsOwnedByFactionOrAlliedTo(this Unit unit, Faction faction)
		{
			if (faction == null)
			{
				return false;
			}
			if (unit.Faction != null)
			{
				if (unit.Faction == faction)
				{
					return true;
				}
				if (unit.Faction.IsAlliedTo(faction))
				{
					return true;
				}
			}
			return false;
		}

		public static bool CanFitInDockIgnoreOccupancy(this Unit unit, Unit dock)
		{
			if (unit.Components != null)
			{
				UnitHangar hangar = dock.GetHangar();
				if (hangar != null)
				{
					return hangar.CanUnitFitInHangarIgnoreOccupancy(unit.Components);
				}
			}
			return false;
		}

		public static bool IsInSameFleetAs(this Unit unit, Unit otherUnit)
		{
			Fleet fleet = unit.GetFleet();
			Fleet fleet2 = otherUnit.GetFleet();
			if (fleet != null)
			{
				return fleet == fleet2;
			}
			return false;
		}

		public static int GetCargoCountOf(this Unit unit, CargoClass cargoClass)
		{
			if (unit.Components != null)
			{
				return unit.Components.GetCargoCountOf(cargoClass);
			}
			return 0;
		}

		public static bool IsWithinDistanceOf(this Unit unit, Unit otherUnit, float requiredDistance)
		{
			float num = Vector3.Distance(unit.transform.position, otherUnit.transform.position);
			if (unit.UnitClass.DisplayData != null)
			{
				num -= unit.UnitClass.DisplayData.Radius;
			}
			if (otherUnit.UnitClass.DisplayData != null)
			{
				num -= otherUnit.UnitClass.DisplayData.Radius;
			}
			return num < requiredDistance;
		}

		public static bool IsPilottedByPlayer(this Unit unit)
		{
			if (unit.Components != null && unit.Components.PilotPerson != null)
			{
				return unit.Components.PilotPerson.IsLocalPlayer;
			}
			return false;
		}

		public static bool StationShouldShowDesignation(this Unit unit)
		{
			if (unit.UnitClass.StationPurpose != StationPurpose.Bar)
			{
				return unit.UnitClass.StationPurpose != StationPurpose.SectorControl;
			}
			return false;
		}

		public static bool UnitPilotHasBounty(this Unit unit)
		{
			return BountyHelper.IsBountyPlacedOnUnitPilot(unit);
		}

		public static IOrderedEnumerable<Unit> OrderByDistanceFromUnit(this IEnumerable<Unit> units, Unit sourceUnit)
		{
			return units.OrderByDistanceFromSector(sourceUnit.Sector);
		}

		public static IOrderedEnumerable<Unit> OrderByDistanceFromSector(this IEnumerable<Unit> units, Sector sourceSector)
		{
			return units.OrderBy((Unit e) => sourceSector.GetJumpDistanceTo(e.Sector));
		}

		public static string GetDesignationOrClassAndSeries(this Unit unit, bool shortName = false)
		{
			if (!string.IsNullOrEmpty(unit.UnitName))
			{
				if (shortName && !string.IsNullOrEmpty(unit.UnitShortName))
				{
					return unit.UnitShortName;
				}
				return unit.UnitName;
			}
			return unit.GetClassAndSeriesName(shortName);
		}

		public static void UndockIfDocked(this Unit unit)
		{
			UnitComponentHolder components = unit.Components;
			if (components != null)
			{
				components.UndockIfDocked();
			}
		}

		public static Fleet FindAIGroupFromAnyCrewMember(this Unit unit)
		{
			if (unit.Components != null)
			{
				foreach (Person item in unit.Components.Crew)
				{
					if (item != null && item.NpcPilot != null)
					{
						return item.NpcPilot.Fleet;
					}
				}
			}
			return null;
		}

		public static bool IsImmediateDockOwnFaction(this Unit unit)
		{
			if (unit.Faction != null && unit.IsDocked)
			{
				return unit.Components.DockUnit.Faction == unit.Faction;
			}
			return false;
		}

		public static Unit GetDockUnitOrSelf(this Unit unit)
		{
			if (unit.Components != null && unit.Components.DockUnit != null)
			{
				return unit.Components.DockUnit;
			}
			return unit;
		}

		public static Unit GetDockUnit(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.Components.DockUnit;
			}
			return null;
		}

		public static int GetDockLevelOwnFaction(this Unit unit)
		{
			int num = 0;
			Unit dockUnit = unit.GetDockUnit();
			while (dockUnit != null && dockUnit.Faction == unit.Faction)
			{
				num++;
				dockUnit = dockUnit.GetDockUnit();
			}
			return num;
		}

		public static int GetDockLevel(this Unit unit)
		{
			int num = 0;
			Unit dockUnit = unit.GetDockUnit();
			while (dockUnit != null)
			{
				num++;
				dockUnit = dockUnit.GetDockUnit();
			}
			return num;
		}

		public static bool AtTheSameRootUnitAs(this Unit unit, Unit otherUnit)
		{
			Unit rootUnit = unit.GetRootUnit();
			Unit rootUnit2 = otherUnit.GetRootUnit();
			if (rootUnit == rootUnit2)
			{
				if (!(rootUnit != unit))
				{
					return rootUnit2 != otherUnit;
				}
				return true;
			}
			return false;
		}

		public static void UndockAllShipsOfFaction(this Unit unit, Faction faction)
		{
			if ((unit.UnitType != UnitType.Ship && unit.UnitType != UnitType.Station) || !unit.HasDockedUnits())
			{
				return;
			}
			dockedShipCache.Clear();
			dockedShipCache.AddRange(unit.GetDockedUnits());
			foreach (UnitComponentHolder item in dockedShipCache)
			{
				if (item.Unit.Faction == faction)
				{
					item.UndockIfDocked();
				}
			}
		}

		public static bool CanShowUnitInfo(this Unit unit)
		{
			if (unit.UnitType != UnitType.Ship && unit.UnitType != UnitType.Station && unit.UnitType != UnitType.Wormhole)
			{
				return unit.UnitType == UnitType.Cargo;
			}
			return true;
		}

		public static bool HasDockedUnits(this Unit unit)
		{
			if (unit.Components != null && unit.Components.HangarComponent != null)
			{
				return unit.Components.HangarComponent.DockedUnitCount > 0;
			}
			return false;
		}

		public static IEnumerable<UnitComponentHolder> GetDockedUnits(this Unit unit)
		{
			if (unit.Components != null && unit.Components.HangarComponent != null)
			{
				return unit.Components.HangarComponent.DockedShips;
			}
			return null;
		}

		public static string GetFriendlyName(this Unit unit, bool shortName = false)
		{
			switch (unit.UnitType)
			{
			case UnitType.Station:
				if (!string.IsNullOrEmpty(unit.UnitName))
				{
					if (unit.UnitClass.StationPurpose == StationPurpose.Bar)
					{
						if (shortName && !string.IsNullOrEmpty(unit.UnitShortName))
						{
							return "Bar \"" + unit.UnitShortName + "\"";
						}
						return "Bar \"" + unit.UnitName + "\"";
					}
					if (shortName && !string.IsNullOrEmpty(unit.UnitShortName))
					{
						return unit.UnitShortName;
					}
					return unit.UnitName;
				}
				if (unit.StationShouldShowDesignation())
				{
					return unit.GetDesignationOrClassAndSeries(shortName) + " " + unit.GetShortDesignation();
				}
				break;
			case UnitType.Asteroid:
				return unit.Asteroid.GetFriendlyName();
			case UnitType.Ship:
				return unit.Components.GetClassAndName(shortName);
			case UnitType.Wormhole:
				return unit.WormholeComponent.GetFriendlyName(shortName);
			case UnitType.Cargo:
				return unit.CargoComponent.GetFriendlyName(shortName);
			}
			return unit.GetDesignationOrClassAndSeries(shortName);
		}

		public static string GetFriendlyNameForLocalFaction(this Unit unit, bool shortName = false)
		{
			return unit.GetFriendlyNameForFaction(EngineASX.Instance.LocalFaction, shortName);
		}

		public static string GetFriendlyNameForFaction(this Unit unit, Faction ourFaction, bool shortName = false, bool colourByHostility = false)
		{
			switch (unit.UnitType)
			{
			case UnitType.Wormhole:
				if (ourFaction != null && ourFaction.Intel != null)
				{
					if (ourFaction.Intel.HasWormholeBeenEntered(unit.WormholeComponent))
					{
						return unit.WormholeComponent.GetFriendlyName(shortName);
					}
					return unit.WormholeComponent.GetUnexploredName();
				}
				break;
			case UnitType.Ship:
				if (unit.UnitClass.ShipType == ShipType.Normal && unit.Faction == null)
				{
					string nameOfFactionlessUnit = UnitNamer.GetNameOfFactionlessUnit(unit);
					if (colourByHostility)
					{
						return UnityRichTextHelper.ColorFromOpinion(nameOfFactionlessUnit, ourFaction, unit.Faction);
					}
					return nameOfFactionlessUnit;
				}
				break;
			}
			string friendlyName = unit.GetFriendlyName(shortName);
			if (colourByHostility)
			{
				return UnityRichTextHelper.ColorFromOpinion(friendlyName, ourFaction, unit.Faction);
			}
			return friendlyName;
		}

		public static string GetFriendlyNameAndFactionShortName(this Unit unit)
		{
			string friendlyName = unit.GetFriendlyName();
			if (unit.Faction != null)
			{
				return $"{friendlyName} ({unit.Faction.GetShortNameElseLong()})";
			}
			return friendlyName;
		}

		public static bool RequiresRearm(this Unit unit, float requiredEquipmentLoad)
		{
			if (unit.CargoBayComponent == null)
			{
				return false;
			}
			if (unit.CargoBayComponent.FreeSpace < 1f)
			{
				return false;
			}
			if (unit.CargoBayComponent.EquipmentLoad01 < requiredEquipmentLoad)
			{
				return true;
			}
			return false;
		}

		public static bool RequiresRepair(this Unit unit, bool considerShields = true, bool considerComponents = true, float threshold = 1f)
		{
			if (unit.Destructable != null)
			{
				if (unit.Destructable.HealthNormalized < threshold)
				{
					return true;
				}
				if (unit.Components != null)
				{
					if (considerShields && unit.Components.ShieldComponent != null && unit.Components.ShieldComponent.GetNormalizedShieldCharge() < threshold)
					{
						return true;
					}
					if (considerComponents)
					{
						foreach (ComponentBase unitComponent in unit.Components.UnitComponents)
						{
							if (unitComponent.HealthNormalized < threshold)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public static bool RequiresRepair(this Unit unit, float hullConditionThreshold, float componentsConditionThreshold, float shieldConditionThreshold)
		{
			if (unit.Destructable.HealthNormalized < hullConditionThreshold)
			{
				return true;
			}
			if (unit.Components.AnyComponentDamaged(1f - componentsConditionThreshold))
			{
				return true;
			}
			if (unit.Components.ShieldComponent != null && unit.Components.ShieldComponent.GetNormalizedShieldCharge() < shieldConditionThreshold)
			{
				return true;
			}
			return false;
		}

		public static int GetWeakestOfForwardShield(this Unit unit, out float normalizedValue)
		{
			normalizedValue = float.MaxValue;
			if (unit.Components != null)
			{
				int result = 0;
				ShieldComponent shieldComponent = unit.Components.ShieldComponent;
				if (shieldComponent != null)
				{
					for (int i = -1; i <= 1; i++)
					{
						int num = EngineASX.WrapShieldIndex(i);
						float shieldPointNormalized = shieldComponent.GetShieldPointNormalized(num);
						if (shieldPointNormalized < normalizedValue)
						{
							normalizedValue = shieldPointNormalized;
							result = num;
						}
					}
				}
				return result;
			}
			normalizedValue = 0f;
			return 0;
		}

		public static float GetHighestShieldNormalized(this Unit unit)
		{
			float num = float.MinValue;
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			for (int i = 0; i < 6; i++)
			{
				float shieldPointNormalized = shieldComponent.GetShieldPointNormalized(i);
				if (shieldPointNormalized > num)
				{
					num = shieldPointNormalized;
				}
			}
			return num;
		}

		public static float GetLowestShieldNormalized(this Unit unit)
		{
			float num = float.MaxValue;
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			for (int i = 0; i < 6; i++)
			{
				float shieldPointNormalized = shieldComponent.GetShieldPointNormalized(i);
				if (shieldPointNormalized < num)
				{
					num = shieldPointNormalized;
				}
			}
			return num;
		}

		public static int GetStrongestShield(this Unit unit)
		{
			float num = float.MinValue;
			int num2 = -1;
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			for (int i = 0; i < 6; i++)
			{
				float shieldPoints = shieldComponent.GetShieldPoints(i);
				if (num2 < 0 || shieldPoints > num)
				{
					num2 = i;
					num = shieldPoints;
				}
			}
			return num2;
		}

		public static int GetStrongestOfForwardShields(this Unit unit, out float normalizedValue)
		{
			normalizedValue = float.MinValue;
			if (unit.Components != null)
			{
				int result = 0;
				ShieldComponent shieldComponent = unit.Components.ShieldComponent;
				if (shieldComponent != null)
				{
					for (int i = -1; i <= 1; i++)
					{
						int num = EngineASX.WrapShieldIndex(i);
						float shieldPointNormalized = shieldComponent.GetShieldPointNormalized(num);
						if (shieldPointNormalized > normalizedValue)
						{
							normalizedValue = shieldPointNormalized;
							result = num;
						}
					}
				}
				return result;
			}
			normalizedValue = 0f;
			return 0;
		}

		public static float GetForwardShieldNormallized(this Unit unit)
		{
			if (unit.Components != null && unit.Components.ShieldComponent != null)
			{
				return unit.Components.ShieldComponent.GetShieldPointNormalized(0);
			}
			return 0f;
		}

		public static bool IsShip(this Unit unit)
		{
			return unit.UnitType == UnitType.Ship;
		}

		public static bool IsNormalShip(this Unit unit)
		{
			if (unit.UnitType == UnitType.Ship)
			{
				return unit.UnitClass.ShipType == ShipType.Normal;
			}
			return false;
		}

		public static bool IsStation(this Unit unit)
		{
			return unit.UnitType == UnitType.Station;
		}

		public static bool IsTurret(this Unit unit)
		{
			if (unit.UnitType == UnitType.Station)
			{
				return unit.UnitClass.StationPurpose == StationPurpose.Defence;
			}
			return false;
		}

		public static bool IsMinorStation(this Unit unit)
		{
			if (unit.UnitType == UnitType.Station)
			{
				if (unit.UnitClass.StationPurpose != StationPurpose.Satellite)
				{
					return unit.UnitClass.StationPurpose == StationPurpose.Defence;
				}
				return true;
			}
			return false;
		}

		public static bool IsMajorStation(this Unit unit)
		{
			if (unit.UnitType == UnitType.Station)
			{
				if (unit.UnitClass.StationPurpose != StationPurpose.Satellite)
				{
					return unit.UnitClass.StationPurpose != StationPurpose.Defence;
				}
				return false;
			}
			return false;
		}

		public static bool IsStationOrShip(this Unit unit)
		{
			if (unit.UnitType != UnitType.Ship)
			{
				return unit.UnitType == UnitType.Station;
			}
			return true;
		}

		public static bool HasHangar(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.Components.HangarComponent != null;
			}
			return false;
		}

		public static string CurrentSectorNameOrUnknown(this Unit unit)
		{
			if (unit.Sector != null)
			{
				return unit.Sector.Name;
			}
			return "[Unknown]";
		}

		public static string GetSectorLocationText(Vector3 localSectorPosition)
		{
			return TextFormattingHelper.FormatSectorPosition(localSectorPosition);
		}

		public static bool HasMiningLaser(this Unit unit)
		{
			if (unit.Components != null)
			{
				foreach (TurretComponent turret in unit.Components.Turrets)
				{
					if (turret.ComponentClass.ComponentBayType.BayType == BayType.Mining)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool HasTractorBeam(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.Components.TractorTurret != null;
			}
			return false;
		}

		public static bool HasSameRootUnitAsPlayer(this Unit unit)
		{
			return unit.HasSameRootUnitAs(unit.Engine.PlayerUnit);
		}

		public static bool HasSameRootUnitAs(this Unit unit, Unit otherUnit)
		{
			if (otherUnit != null)
			{
				return unit.GetRootUnit() == otherUnit.GetRootUnit();
			}
			return false;
		}

		public static Quaternion GetUndockRotation(this Unit dockUnit, Unit undockingUnit)
		{
			UnitHangarBay dockedInHangarBay = undockingUnit.Components.DockedInHangarBay;
			if (dockedInHangarBay != null)
			{
				return dockedInHangarBay.transform.rotation;
			}
			return Quaternion.identity;
		}

		public static UnitHangar GetHangar(this Unit unit)
		{
			if (unit != null && unit.Components != null)
			{
				return unit.Components.HangarComponent;
			}
			return null;
		}

		public static Vector3 GetUndockSectorPosition(this Unit dockUnit, Unit undockingUnit, float undockedUnitRadius, float distanceMultiplier = 1f)
		{
			Vector3 zero = Vector3.zero;
			UnitHangarBay dockedInHangarBay = undockingUnit.Components.DockedInHangarBay;
			if (dockedInHangarBay != null)
			{
				zero = dockedInHangarBay.transform.position + dockedInHangarBay.transform.forward * (undockedUnitRadius * distanceMultiplier + dockUnit.Engine.GameSettings.UndockDistance);
			}
			else
			{
				float num = dockUnit.UnitClass.ShieldRingRadius + undockedUnitRadius + dockUnit.Engine.GameSettings.UndockDistance;
				num *= distanceMultiplier;
				zero = dockUnit.transform.TransformPoint(Vector3.forward * num);
			}
			return dockUnit.Sector.ToLocalPosition(zero);
		}

		public static Vector3 GetSafeUndockSectorPosition(this Unit dockUnit, Unit undockingUnit, float distanceMultiplier = 1f)
		{
			float num = 0f;
			if (undockingUnit != null)
			{
				num = undockingUnit.UnitClass.ShieldRingRadius;
			}
			Vector3 undockSectorPosition = dockUnit.GetUndockSectorPosition(undockingUnit, num, distanceMultiplier);
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(dockUnit.Sector, undockSectorPosition, num, GameController.Instance.NonOVerlappingUnitsMask);
		}

		public static bool IsPlayerCustomPathTarget(this Unit unit)
		{
			if (unit.Engine.LocalPlayer != null)
			{
				return unit.Engine.LocalPlayer.CustomUnitWaypoint == unit;
			}
			return false;
		}

		public static bool IsPlayerMissionPathTarget(this Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed)
			{
				return false;
			}
			GamePlayer localPlayer = EngineASX.Instance.LocalPlayer;
			if (localPlayer != null)
			{
				foreach (PlayerWaypointPath missionPath in localPlayer.WaypointController.MissionPaths)
				{
					if (missionPath.HasValidPath && missionPath.Waypoint.Value.TargetUnit == unit)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool IsPlayerMissionPathTargetOrFirstWaypoint(this Unit unit)
		{
			if (unit.UnitType == UnitType.Waypoint)
			{
				UnitWaypoint component = unit.GetComponent<UnitWaypoint>();
				if (component.PlayerWaypointPath != null)
				{
					return !component.PlayerWaypointPath.IsCustomPath;
				}
				return false;
			}
			if (!unit.IsPlayerMissionPathTarget())
			{
				return unit.IsPlayerFirstMissionWaypoint();
			}
			return true;
		}

		public static bool IsPlayerCustomPathMarker(this Unit unit)
		{
			if (unit.UnitType == UnitType.Waypoint)
			{
				UnitWaypoint component = unit.GetComponent<UnitWaypoint>();
				if (component.PlayerWaypointPath != null)
				{
					return component.PlayerWaypointPath.IsCustomPath;
				}
				return false;
			}
			return false;
		}

		public static bool IsPlayerCustomPathTargetOrFirstWaypointOrPathMarker(this Unit unit)
		{
			if (unit.IsPlayerCustomPathMarker())
			{
				return true;
			}
			if (!unit.IsPlayerCustomPathTarget())
			{
				return unit.IsPlayerFirstCustomWaypoint();
			}
			return true;
		}

		public static bool IsPlayerFirstCustomWaypoint(this Unit unit)
		{
			if (unit.Engine.LocalPlayer != null)
			{
				return unit.Engine.WaypointController.CustomPath.IsFirstNavpoint(unit);
			}
			return false;
		}

		public static bool IsPlayerFirstMissionWaypoint(this Unit unit)
		{
			GamePlayer localPlayer = unit.Engine.LocalPlayer;
			if (localPlayer != null)
			{
				foreach (PlayerWaypointPath missionPath in localPlayer.WaypointController.MissionPaths)
				{
					if (missionPath.HasValidPath)
					{
						_ = missionPath.Waypoint.Value;
						if (missionPath.IsFirstNavpoint(unit))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static void ClearZAndXRotation(this Unit unit)
		{
			float yBearing = Unit.GetYBearing(unit.transform.forward);
			unit.transform.localRotation = Quaternion.Euler(new Vector3(0f, yBearing, 0f));
		}

		public static void ClearXRotation(this Unit unit)
		{
			Vector3 eulerAngles = unit.transform.localRotation.eulerAngles;
			eulerAngles.x = 0f;
			unit.transform.localEulerAngles = eulerAngles;
		}

		public static bool IsSellerOfUnitClass(this Unit unit, UnitClass unitClass)
		{
			UnitShipTrader shipTrader = unit.ShipTrader;
			if (shipTrader != null)
			{
				return shipTrader.Sells(unitClass);
			}
			return false;
		}

		public static bool IsAbandonedShip(this Unit unit)
		{
			if (unit.UnitType == UnitType.Ship && unit.Faction == null)
			{
				return unit.Components.PilotPerson == null;
			}
			return false;
		}

		public static Fleet GetFleet(this Unit unit)
		{
			if (unit.NpcPilot != null)
			{
				return unit.NpcPilot.Fleet;
			}
			return null;
		}

		public static Person GetPilot(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.Components.PilotPerson;
			}
			return null;
		}

		public static bool IsInCombat(this Unit unit)
		{
			if (unit.NpcPilot != null)
			{
				return unit.NpcPilot.IsInCombat;
			}
			return false;
		}

		public static void MoveNearTo(this Unit unit, Unit otherUnit)
		{
			if (unit.Sector != otherUnit.Sector)
			{
				Debug.LogWarning("Units are not in the same sector", unit);
			}
			unit.transform.localPosition = otherUnit.transform.localPosition + Geometry.RandomXZUnitVector() * (unit.UnitClass.ShieldRingRadius + otherUnit.UnitClass.ShieldRingRadius + 50f);
		}

		public static void LookAt(this Unit unit, Unit otherUnit)
		{
			Vector3 vector = otherUnit.SectorPosition - unit.SectorPosition;
			unit.transform.localRotation = Quaternion.LookRotation(vector.normalized, Vector3.up);
		}

		public static Vector3 GetRandomSectorPositionWithinRadius(this Unit unit, float radiusMultiplier = 0.9f)
		{
			float num = Random.Range(0f, unit.Radius * radiusMultiplier);
			Vector3 result = unit.SectorPosition + Geometry.RandomXZUnitVector() * num;
			result.y = 0f;
			return result;
		}

		public static bool IsHostileTo(this Unit unit, Fleet fleet)
		{
			if (unit.Faction != null && fleet.Faction != null)
			{
				return unit.Faction.IsHostileTo(fleet.Faction);
			}
			return false;
		}

		public static Faction GetFactionControllingSector(this Unit unit)
		{
			if (unit.Sector != null)
			{
				return unit.Sector.ControllingFaction;
			}
			return null;
		}

		public static bool HasAnyCargo(this Unit unit)
		{
			CargoBayComponent cargoBayComponent = unit.CargoBayComponent;
			if (cargoBayComponent != null)
			{
				return !cargoBayComponent.IsEmpty;
			}
			return false;
		}

		public static bool IsMajorBanditStation(this Unit unit)
		{
			if (unit.IsMajorStation() && unit.Faction != null)
			{
				return unit.Faction.FactionType == FactionType.Bandit;
			}
			return false;
		}

		public static bool IsEquipmentTrader(this Unit unit)
		{
			if (unit.Components != null && unit.Components.CargoTrader != null)
			{
				return unit.Components.CargoTrader.PriceSetter.SetAllEquipmentPrices;
			}
			return false;
		}

		public static bool CouldBeDismantled(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.IsStationOrShip();
			}
			return false;
		}

		public static bool CouldBeSelfDestructed(this Unit unit)
		{
			UnitType unitType = unit.UnitType;
			if ((uint)(unitType - 1) <= 1u || unitType == UnitType.Cargo)
			{
				return true;
			}
			return false;
		}

		public static bool CanDismantleNow(this Unit unit, Faction faction)
		{
			if (!unit.CouldBeDismantled())
			{
				return false;
			}
			if (unit.Faction != faction)
			{
				return false;
			}
			if (unit.Components.ConstructionState != ConstructionState.Constructed)
			{
				return unit.Components.ConstructionState == ConstructionState.Constructing;
			}
			return true;
		}

		public static bool UnitHasPassengerCapacity(this Unit unit)
		{
			if (unit.Components != null && unit.Components.PassengerCapacity > 0)
			{
				return true;
			}
			return false;
		}

		public static bool IsDiscoveredByFaction(this Unit unit, Faction faction, float maxAgeOfDiscovery)
		{
			if (faction != null && faction.Intel != null)
			{
				return faction.Intel.IsUnitDiscoveredOrOwned(unit, maxAgeOfDiscovery);
			}
			return false;
		}

		public static bool IsFleetLeader(this Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet != null)
			{
				return fleet.LeaderUnit == unit;
			}
			return false;
		}

		public static bool IsPilottable(this Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed && unit.UnitClass.ShipType == ShipType.Normal && unit.UnitClass.IsPilottable)
			{
				return !unit.Components.IsUnderConstructionOrDismantling;
			}
			return false;
		}

		public static ActiveFleetOrder GetActiveFleetOrder(this Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (fleet != null)
			{
				return fleet.ActiveOrder;
			}
			return null;
		}

		public static bool IsHullDamaged(this Unit unit)
		{
			if (unit.Destructable != null)
			{
				return unit.Destructable.IsHullDamaged;
			}
			return false;
		}

		public static bool IsShieldDamaged(this Unit unit)
		{
			if (unit.Components == null)
			{
				return false;
			}
			ShieldComponent shieldComponent = unit.Components.ShieldComponent;
			if (shieldComponent == null)
			{
				return false;
			}
			return shieldComponent.IsAnyShieldDamaged();
		}

		public static bool IsShieldDisabled(this Unit unit)
		{
			if (unit.Components == null)
			{
				return false;
			}
			return unit.Components.HasDisabledShield;
		}

		public static CapacitorComponent GetCapacitor(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.Components.Capacitor;
			}
			return null;
		}

		public static ComponentBay GetPowerGeneratorBay(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.Components.Bays.FirstOrDefault((ComponentBay e) => e.BayType.BayType == BayType.PowerGenerator);
			}
			return null;
		}

		public static bool IsPullingUnit(this Unit unit)
		{
			if (unit.Components != null)
			{
				TractorTurretComponent tractorTurret = unit.Components.TractorTurret;
				if (tractorTurret != null)
				{
					return tractorTurret.IsPullingUnit;
				}
				return false;
			}
			return false;
		}

		public static TractorTurretComponent GetTractorTurret(this Unit unit)
		{
			if (unit.Components != null)
			{
				return unit.Components.TractorTurret;
			}
			return null;
		}

		public static bool IsMercenary(this Unit unit)
		{
			if (unit.NpcPilot != null && unit.Faction != null)
			{
				return unit.Faction.FactionType == FactionType.Mercenary;
			}
			return false;
		}

		public static bool HasNoFleetOrders(this Unit unit)
		{
			Fleet fleet = unit.GetFleet();
			if (!(fleet == null))
			{
				return !fleet.HasAnyOrders;
			}
			return true;
		}
	}
}
