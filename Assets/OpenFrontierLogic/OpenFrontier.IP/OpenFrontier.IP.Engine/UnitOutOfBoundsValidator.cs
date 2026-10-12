using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class UnitOutOfBoundsValidator
	{
		public static void ValidateAll()
		{
			ValidateFleets();
			ValidateUnits();
			ValidateWormholeTargets();
		}

		private static void ValidateWormholeTargets()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (!IsLocalPositionWithinBounds(item.WormholeComponent.GetTargetSectorPosition()))
					{
						Debug.LogWarning($"Wormhole {item} has target sector position out of bounds: {item.WormholeComponent.GetTargetSectorPosition()}", item);
					}
				}
			}
		}

		public static void ValidateUnit(Unit unit, float maxDistance)
		{
			if (!(unit != null) || !unit.IsValidAndNotDestroyed || unit.UnitType == UnitType.Planet)
			{
				return;
			}
			if (!unit.IsDocked)
			{
				Vector3 localPosition = unit.transform.localPosition;
				if (!IsLocalPositionWithinBounds(maxDistance, localPosition))
				{
					OutputUnitOutOfBoundsError(unit, localPosition);
				}
			}
			else
			{
				Vector3 localPosition2 = unit.transform.localPosition;
				if (localPosition2.magnitude > 1000f)
				{
					Debug.LogWarning($"Unit {unit} is docked but local position is very high: Local position: {localPosition2}", unit);
				}
			}
		}

		public static void ValidateUnits()
		{
			float maxDistance = EngineASX.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginUpperBound;
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				ValidateUnit(unit, maxDistance);
			});
		}

		public static void ValidateFleets()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				foreach (Fleet fleet in faction.Fleets)
				{
					if (fleet.IsValid)
					{
						ValidateFleet(fleet);
					}
				}
			}
		}

		public static void ValidateFleet(Fleet fleet)
		{
			float maxUnitDistanceFromOriginUpperBound = EngineASX.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginUpperBound;
			Vector3 localPosition = fleet.transform.localPosition;
			if (!IsLocalPositionWithinBounds(maxUnitDistanceFromOriginUpperBound, localPosition))
			{
				OutputFleetOutOfBoundsError(fleet, localPosition);
			}
		}

		public static void OutputFleetOutOfBoundsError(Fleet fleet, Vector3 localPosition)
		{
			Debug.LogError($"Fleet \"{fleet}\" out of sector {fleet.Sector} bounds. LocalPos: {localPosition}", fleet);
		}

		public static void OutputUnitOutOfBoundsError(Unit unit, Vector3 localPosition)
		{
			Debug.LogError($"Unit \"{unit}\" out of sector {unit.Sector} bounds. LocalPos: {localPosition}", unit);
		}

		public static bool IsLocalPositionWithinBounds(float maxDistance, Vector3 localPosition)
		{
			if (localPosition.x < 0f - maxDistance || localPosition.x > maxDistance)
			{
				return false;
			}
			if (localPosition.z < 0f - maxDistance || localPosition.z > maxDistance)
			{
				return false;
			}
			return true;
		}

		public static bool IsLocalPositionWithinBounds(Vector3 localPosition)
		{
			return IsLocalPositionWithinBounds(EngineASX.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginUpperBound, localPosition);
		}

		public static bool IsLocalPositionWithinLowerBounds(Vector3 localPosition)
		{
			return IsLocalPositionWithinBounds(EngineASX.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginLowerBound, localPosition);
		}
	}
}
