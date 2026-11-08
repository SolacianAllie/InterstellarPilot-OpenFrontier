using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.WorldPopulation
{
	public static class SectorCreator
	{
		public static void CreateSectorsAndWormholes(Transform root, IEnumerable<SectorBlueprint> nodes, EngineASX engine, IEnumerable<Sector> sectorPrefabs, Wormhole wormholePrefab, float sectorMapPositionScaleFudge, SectorLightingSettings sectorLightingSettings)
		{
			Dictionary<SectorBlueprint, Sector> dictionary = new Dictionary<SectorBlueprint, Sector>();
			Dictionary<SectorConnection, Wormhole> dictionary2 = new Dictionary<SectorConnection, Wormhole>();
			foreach (SectorBlueprint node in nodes)
			{
				foreach (SectorConnection connection in node.Connections)
				{
					if (!nodes.Contains(connection.TargetSector))
					{
						throw new Exception($"Node {node} has a connection pointing to a node that doesn't exist in the collection ({connection.TargetSector})");
					}
				}
			}
			foreach (SectorBlueprint node2 in nodes)
			{
				Sector sector = CreateSector(node2, sectorPrefabs, sectorMapPositionScaleFudge, sectorLightingSettings);
				if (root != null)
				{
					sector.transform.SetParent(root, worldPositionStays: true);
				}
				dictionary[node2] = sector;
				foreach (SectorConnection connection2 in node2.Connections)
				{
					Wormhole value = CreateWormhole(wormholePrefab, sector);
					dictionary2[connection2] = value;
				}
			}
			SectorPositioner.PositionSectors(engine.Sectors, GameController.Instance.GameSettings.UniverseBoundsSettings);
			foreach (SectorBlueprint node3 in nodes)
			{
				_ = dictionary[node3];
				foreach (SectorConnection connection3 in node3.Connections)
				{
					LinkWormhole(dictionary, dictionary2, node3, connection3);
				}
			}
			foreach (Sector sector2 in engine.Sectors)
			{
				List<Unit> unitsByType = sector2.GetUnitsByType(UnitType.Wormhole);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					item.WormholeComponent.AutoTransformGate();
				}
			}
		}

		private static void LinkWormhole(Dictionary<SectorBlueprint, Sector> nodeSceneMap, Dictionary<SectorConnection, Wormhole> connectionWormholeMap, SectorBlueprint node, SectorConnection connection)
		{
			SectorBlueprint targetSector = connection.TargetSector;
			_ = nodeSceneMap[targetSector];
			Wormhole wormhole = connectionWormholeMap[connection];
			SectorConnection sectorConnection = targetSector.Connections.FirstOrDefault((SectorConnection e) => e.TargetSector == node);
			if (sectorConnection != null)
			{
				Wormhole targetGate = connectionWormholeMap[sectorConnection];
				wormhole.TargetGate = targetGate;
				wormhole.UpdateActualTargetScene();
			}
		}

		public static Sector CreateSector(SectorBlueprint node, IEnumerable<Sector> sectorPrefabs, float sectorMapPositionScaleFudge, SectorLightingSettings sectorLightingSettings)
		{
			Sector sector = UnityObjectHelper.InstantiateAndGetComponent(GetSectorPrefab(sectorPrefabs));
			sector.AssignRandomSeed();
			sector.Init();
			sector.MapPosition = node.Position * sectorMapPositionScaleFudge;
			sector.GateDistanceMultiplier = node.GateDistanceMultiplier;
			sector.Description = null;
			sector.BackgroundRotation = Quaternion.identity;
			sector.LightRotation = SetSectorLightDirectionFromSeed(sector);
			sector.LightDirectionFudge = UnityEngine.Random.value;
			sector.SectorType = node.SectorType;
			sector.AsteroidType = node.AsteroidType;
			// Open Frontier: seed this sector's star color into its direction
			// light color (never set by the generator otherwise). The field
			// is serialized with saves, so the star is created with and saved
			// into the universe itself. Full brightness (no 0.8 cap) - why not.
			sector.DirectionLightColor = StarColorGenerator.ForSector(sector.RandomSeed);
			sector.SkyExposure = Maths.RandomFloatWithPower(sectorLightingSettings.SkyMinExposure, sectorLightingSettings.SkyMaxExposure, sectorLightingSettings.SkyExposurePower);
			sector.SkyTintColor = UnityEngine.Random.ColorHSV(sectorLightingSettings.SkyMinHue, sectorLightingSettings.SkyMaxHue, sectorLightingSettings.SkyMinSaturation, sectorLightingSettings.SkyMaxSaturation, sectorLightingSettings.SkyMinValue, sectorLightingSettings.SkyMaxValue);
			sector.AmbientLightColor = Color.Lerp(new Color(0.03f, 0.03f, 0.03f), new Color(0.3f, 0.3f, 0.3f), UnityEngine.Random.value);
			if (!string.IsNullOrWhiteSpace(node.Name))
			{
				sector.Name = node.Name;
			}
			return sector;
		}

		public static Quaternion SetSectorLightDirectionFromSeed(Sector sector)
		{
			System.Random random = new System.Random(sector.RandomSeed);
			return Quaternion.Euler(Mathf.Lerp(30f, 68f, (float)random.NextDouble()), 0f, 0f);
		}

		public static Sector GetSectorPrefab(IEnumerable<Sector> sectorPrefabs)
		{
			return sectorPrefabs.GetRandom();
		}

		private static Wormhole CreateWormhole(Wormhole wormholePrefab, Sector scene)
		{
			Wormhole wormhole = UnityObjectHelper.InstantiateAndGetComponent(wormholePrefab);
			wormhole.transform.SetParent(scene.transform, worldPositionStays: true);
			wormhole.GetComponent<Unit>().Init();
			return wormhole;
		}
	}
}
