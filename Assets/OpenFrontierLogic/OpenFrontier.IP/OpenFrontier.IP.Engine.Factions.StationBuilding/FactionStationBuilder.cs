using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.WorldPopulation;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions.StationBuilding
{
	public class FactionStationBuilder
	{
		private FactionAIBase factionAI;

		public FactionAIBase FactionAI
		{
			get
			{
				return factionAI;
			}
			set
			{
				factionAI = value;
			}
		}

		public Faction Faction => factionAI.Faction;

		public void BuildStations()
		{
			if (factionAI.CanRequestToBuildNewStations() && factionAI.Faction.Fleets.Count != 0)
			{
				WorldStationSeeder.StationBuild nextStationBuild = GetNextStationBuild(factionAI.Faction.Credits - factionAI.Faction.CreditsReserve, considerShipRatio: true);
				if (nextStationBuild != null && !factionAI.RequestToBuildNewStation(nextStationBuild.UnitClass, nextStationBuild.Sector, nextStationBuild.SectorPosition))
				{
					EngineASX.Instance.DebugInfo.NumFactionAIBuildStationRequestsDeclined++;
				}
			}
		}

		public WorldStationSeeder.StationBuild GetNextStationBuild(int? availableCredits, bool considerShipRatio)
		{
			if (factionAI.FactionTypeInfo != null)
			{
				if (factionAI.AllowNewStationBuild())
				{
					int maxBuildDistanceFromHomeSector = factionAI.AISettings.MaxStationBuildDistanceFromHomeSector;
					if (factionAI.AISettings.MaxJumpDistanceFromHomeSector >= 0)
					{
						maxBuildDistanceFromHomeSector = Mathf.Min(factionAI.AISettings.MaxStationBuildDistanceFromHomeSector, factionAI.AISettings.MaxJumpDistanceFromHomeSector);
					}
					return WorldStationSeeder.GetNewStationBuild(EngineASX.Instance, Faction, availableCredits, factionAI.FactionTypeInfo.AllowableStationPurposesFlags, considerShipRatio, maxBuildDistanceFromHomeSector);
				}
			}
			else
			{
				Debug.LogErrorFormat(factionAI, "{0}: Cannot determine station to build. Not faction type defined", this);
			}
			return null;
		}

		public static Unit BuildStation(WorldStationSeeder.StationBuild stationToBuild, bool cargoLoadout = true, bool underConstruction = true)
		{
			return BuildStation(stationToBuild.UnitClass, stationToBuild.Sector, stationToBuild.SectorPosition, stationToBuild.Faction, cargoLoadout, underConstruction);
		}

		public static Unit BuildStation(UnitClass unitClass, Sector sector, Vector3 sectorPosition, Faction faction, bool cargoLoadout = true, bool underConstruction = true)
		{
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, sector, cargoLoadout, underConstruction);
			unit.transform.localPosition = sectorPosition;
			unit.Faction = faction;
			if (faction.FactionType == FactionType.Bar && unit.UnitClass.StationPurpose == StationPurpose.Bar)
			{
				unit.UnitName = faction.Name;
				unit.UnitShortName = faction.ShortName;
			}
			return unit;
		}

		public void ValidateNewStationBuild(UnitClass unitClass, Sector sector, Vector3 sectorPosition)
		{
			List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
			if (unitClass.StationPurpose == StationPurpose.Defence && unitsByType != null && unitsByType.Count((Unit e) => e.Faction == Faction && e.UnitClass.StationPurpose != StationPurpose.Defence) == 0)
			{
				Debug.LogError($"Faction {Faction} is building turret where no station to defend", factionAI);
			}
			if (Faction.Intel.GetUniversePath(Faction.HomeSector, sector) == null)
			{
				Debug.LogError($"Faction {Faction} is building a station that it cannot get to from the home sector", factionAI);
			}
			if (!UnitOutOfBoundsValidator.IsLocalPositionWithinBounds(sectorPosition))
			{
				Debug.LogErrorFormat(factionAI, "New station build \"{0}\" in {1} for faction {2} out of sector bounds", unitClass.name, sector.Name, Faction);
			}
		}
	}
}
