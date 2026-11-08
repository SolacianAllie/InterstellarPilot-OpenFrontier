using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.MissionSpecs;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionGenerators
{
	public class BreakdownMissionGenerator : MissionGenerator
	{
		public int MinFactionShips = 1;

		public BreakdownMissionSpec MissionSpecPrefab;

		public override MissionManagerMissionType MissionType => MissionManagerMissionType.Breakdown;

		protected override MissionSpec generateMissionSpec(Unit missionLocationUnit, Faction faction, EngineASX engine)
		{
			UnitClass breakdownUnitClass = GetBreakdownUnitClass(missionLocationUnit);
			if (breakdownUnitClass != null)
			{
				BreakdownMissionSpec breakdownMissionSpec = UnityObjectHelper.InstantiateAndGetComponent(MissionSpecPrefab, missionLocationUnit.transform);
				breakdownMissionSpec.BreakdownUnitClass = breakdownUnitClass;
				Vector3 sectorPosition = Vector3.zero;
				Sector sector = null;
				if (GetBreakdownSectorAndPosition(missionLocationUnit, out sector, out sectorPosition))
				{
					breakdownMissionSpec.BreakdownDestinationSector = sector;
					breakdownMissionSpec.BreakdownDestinationSectorPosition = sectorPosition;
					return breakdownMissionSpec;
				}
				Debug.LogWarning($"Cannot generate courier mission for faction: {faction}. Failed to find sector position.", this);
			}
			else
			{
				Debug.LogWarning($"Cannot generate courier mission for faction: {faction}. No breakdown unitClasses found.", this);
			}
			return null;
		}

		public override bool CanGenerateMission(Unit missionLocationUnit, Faction missionLocationFaction)
		{
			if (missionLocationFaction.FactionAI != null)
			{
				List<Unit> unitsByType = missionLocationFaction.GetUnitsByType(UnitType.Ship);
				if (unitsByType != null)
				{
					return unitsByType.Count > MinFactionShips;
				}
			}
			return false;
		}

		private bool GetBreakdownSectorAndPosition(Unit missionLocationUnit, out Sector sector, out Vector3 sectorPosition)
		{
			sectorPosition = Vector3.zero;
			int maxJumpDist = Maths.RandomIntWithPower(0, GameController.Instance.GameSettings.MissionSettings.BreakdownMissionSettings.MaxJumpDistanceFromMissionGiver, GameController.Instance.GameSettings.MissionSettings.BreakdownMissionSettings.MaxJumpDistancePower);
			SectorFinder.FindSectorsWithinJumpDistanceOfSimple(missionLocationUnit.Sector, maxJumpDist, includeUnstableWormholes: false);
			sector = SectorFinder.Results.Select((SectorFinder.SectorResult e) => e.Sector).GetRandom();
			Vector3? randomBreakdownPositionInSector = GetRandomBreakdownPositionInSector(missionLocationUnit, sector);
			if (randomBreakdownPositionInSector.HasValue)
			{
				sectorPosition = randomBreakdownPositionInSector.Value;
				return true;
			}
			return false;
		}

		private Vector3? GetRandomBreakdownPositionInSector(Unit missionLocationUnit, Sector sector)
		{
			float radius = 50f;
			if (sector == missionLocationUnit.Sector)
			{
				Vector3 randomSectorPositionWithinBounds = sector.GetRandomSectorPositionWithinBounds(0.95f);
				return PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, randomSectorPositionWithinBounds, radius, GameController.Instance.NonOVerlappingUnitsMask);
			}
			float minDistanceFromMissionUnit = GameController.Instance.GameSettings.MissionSettings.BreakdownMissionSettings.MinDistanceFromMissionUnit;
			for (int i = 0; i < 5; i++)
			{
				Vector3 randomSectorPositionWithinBounds2 = sector.GetRandomSectorPositionWithinBounds(0.95f);
				Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(sector, randomSectorPositionWithinBounds2, radius, GameController.Instance.NonOVerlappingUnitsMask);
				if (vector.HasValue && Vector3.Distance(vector.Value, missionLocationUnit.SectorPosition) > minDistanceFromMissionUnit)
				{
					return vector.Value;
				}
			}
			return null;
		}

		private UnitClass GetBreakdownUnitClass(Unit destination)
		{
			return destination.Faction.GetUnitsByType(UnitType.Ship)?.Where((Unit e) => e.UnitClass.SeedInSandbox).GetRandom().UnitClass;
		}
	}
}
