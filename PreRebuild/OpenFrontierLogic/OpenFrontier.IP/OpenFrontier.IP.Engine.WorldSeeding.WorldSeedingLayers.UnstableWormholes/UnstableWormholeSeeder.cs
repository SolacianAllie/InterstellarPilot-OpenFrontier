using System.Collections.Generic;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.UnstableWormholes
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class UnstableWormholeSeeder : MonoBehaviour
	{
		public class SectorScore : IWeighted
		{
			public Sector Sector { get; set; }

			public float Weight { get; set; }
		}

		public UnstableWormholeSeederSettings Settings;

		private List<SectorScore> scores = new List<SectorScore>();

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Create spawn points...", this, 1);
			}
			Settings = world.Seeder.Settings.UnstableWormholeSeederSettings;
			int num = Mathf.RoundToInt(Settings.UnstableWormholeToSectorRatio * (float)EngineASX.Instance.Sectors.Count);
			if (num > 0)
			{
				CreateWormholes(num, world);
			}
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Wormhole);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.WormholeComponent.IsUnstable)
					{
						item.WormholeComponent.RandomizeTargetAndSetNextChangeTime();
					}
				}
			}
		}

		private void CreateWormholes(int count, WorldBase world)
		{
			List<Unit> list = new List<Unit>();
			for (int i = 0; i < count * 3; i++)
			{
				scores.Clear();
				foreach (Sector sector2 in EngineASX.Instance.Sectors)
				{
					if (CanGenerateUnstableWormholeInSector(sector2))
					{
						scores.Add(ScoreSector(sector2));
					}
				}
				Sector sector = FindSectorForWormhole();
				if (!(sector != null))
				{
					continue;
				}
				Vector3? localPositionForWormhole = GetLocalPositionForWormhole(sector, world);
				if (localPositionForWormhole.HasValue)
				{
					Unit item = CreateWormhole(sector, localPositionForWormhole.Value, Settings.WormholeUnitClass, world);
					list.Add(item);
					if (list.Count == count)
					{
						break;
					}
				}
			}
		}

		private bool CanGenerateUnstableWormholeInSector(Sector sectorToScore)
		{
			return sectorToScore.GetUnstableWormholeCount() < 2;
		}

		private SectorScore ScoreSector(Sector sector)
		{
			float num = sector.DistanceFromUniverseCenter01 * 10f;
			num -= (float)sector.GetUnstableWormholeCount() * 10f;
			return new SectorScore
			{
				Sector = sector,
				Weight = num
			};
		}

		private Sector FindSectorForWormhole()
		{
			return scores.GetRandomWeighted().Sector;
		}

		private Unit CreateWormhole(Sector sector, Vector3 sectorPosition, UnitClass unitClass, WorldBase world)
		{
			Unit unit = WorldHelper.SpawnUnitAndInstallComponents(unitClass.UnitPrefab, sector);
			unit.transform.localPosition = sectorPosition;
			return unit;
		}

		private Vector3? GetLocalPositionForWormhole(Sector sector, WorldBase world)
		{
			float num = Mathf.Max(Settings.MinGateDistanceMultiplier, sector.GateDistanceMultiplier);
			float maxInclusive = Mathf.Max(num, Settings.MaxGateDistanceMultiplier);
			float num2 = Random.Range(num, maxInclusive) * world.GateDistance;
			Vector3 vector = Maths.RandomXZDirection() * num2;
			if (!WorldHelper.AnyOverlappingStationOrWormhole(sector, vector, Settings.MinDistanceFromOtherWormholesAndStations))
			{
				return vector;
			}
			return null;
		}
	}
}
