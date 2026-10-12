using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.WorldGeneration.Models;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.WorldGeneration
{
	public class WorldBlueprintSectorGenerator
	{
		private struct WeightedNode : IWeighted
		{
			public SectorBlueprint Node { get; set; }

			public float Weight { get; set; }
		}

		private List<SectorBlueprint> sectorBlueprints = new List<SectorBlueprint>();

		private List<Line> sectorLines = new List<Line>();

		private HashSet<string> existingSectorNames = new HashSet<string>(64);

		private Queue<string> availableSectorNames = new Queue<string>(32);

		public bool UseRandomSectorNames { get; set; }

		public List<string> CustomSectorNames { get; set; }

		public System.Random Random { get; }

		public WorldBlueprintSectorGenerator(System.Random random)
		{
			Random = random;
		}

		private SectorBlueprint GetNextNode(List<SectorBlueprint> nodes, WorldGeneratorSettings settings)
		{
			return nodes.Select((SectorBlueprint e) => new WeightedNode
			{
				Node = e,
				Weight = WeightNodeBySnakinessSetting(nodes, e, settings.Snakiness)
			}).ToList().GetRandomWeighted(Random)
				.Node;
		}

		private float WeightNodeBySnakinessSetting(IEnumerable<SectorBlueprint> nodes, SectorBlueprint node, float snakiness)
		{
			int num = nodes.Max((SectorBlueprint e) => e.Connections.Count);
			float num2 = (float)Mathf.Max(0, node.Connections.Count - 1) / (float)num;
			float num3 = (1f - snakiness) * num2;
			return 1f + num3 * 15f;
		}

		public List<SectorBlueprint> Generate(WorldGeneratorSettings settings)
		{
			Clear();
			int num = Random.Next(settings.MinNumScenes, settings.MaxNumScenes + 1);
			if (!UseRandomSectorNames && (CustomSectorNames == null || CustomSectorNames.Count < num))
			{
				Debug.LogError("Forcing to use random sector names as no name list was provided or name list did not have enough names");
				UseRandomSectorNames = true;
			}
			if (!UseRandomSectorNames)
			{
				List<string> list = CustomSectorNames.ToList();
				for (int i = 0; i < num; i++)
				{
					int index = Random.Next(0, list.Count);
					availableSectorNames.Enqueue(list[index]);
					list.RemoveAt(index);
				}
			}
			CreateAndAddNewSector(settings, Vector3.zero);
			List<SectorBlueprint> list2 = sectorBlueprints.ToList();
			int num2 = num * 30;
			int num3 = 0;
			while (sectorBlueprints.Count < num && list2.Count > 0 && num3 < num2)
			{
				SectorBlueprint nextNode = GetNextNode(list2, settings);
				if (nextNode.Connections.Count < settings.MaxConnections)
				{
					Vector3 vector = Geometry.RandomXZUnitVector(Random);
					if (!nextNode.ConnectionExistsAtDirection(vector, MathF.PI / 180f * settings.MinAngleBetweenGates))
					{
						float num4 = Random.NextFloat(settings.MinDistanceBetweenSectors, settings.MaxDistanceBetweenSectors);
						Vector3 vector2 = nextNode.Position + vector * num4;
						SectorBlueprint sectorBlueprint = null;
						if (settings.SnapNodesEnabled)
						{
							sectorBlueprint = GetSnapNode(settings, nextNode, vector2);
						}
						if (sectorBlueprint == null)
						{
							if (!SectorIntersectsArea(sectorBlueprints.Select((SectorBlueprint s) => s.Position), vector2, settings.MinSectorDistance) && !ConnectionIntersects(nextNode.Position, vector2, settings.LineOvershootDistance, 0f))
							{
								SectorBlueprint sectorBlueprint2 = CreateAndAddNewSector(settings, vector2);
								if (LogWrapper.LogMsgs)
								{
									LogWrapper.Log("Created new sector node " + sectorBlueprint2.Name + " connected to " + nextNode.Name, null, 1);
								}
								list2.Add(sectorBlueprint2);
								SectorConnection sectorConnection = new SectorConnection(sectorBlueprint2);
								ApplyConnection(nextNode, sectorConnection);
								if (nextNode.Connections.Count >= settings.MaxConnections)
								{
									list2.Remove(nextNode);
								}
								if (sectorConnection.TargetSector.Connections.Count >= settings.MaxConnections)
								{
									list2.Remove(sectorConnection.TargetSector);
								}
							}
						}
						else
						{
							SectorConnection sectorConnection2 = new SectorConnection(sectorBlueprint);
							ApplyConnection(nextNode, sectorConnection2);
							if (LogWrapper.LogMsgs)
							{
								LogWrapper.Log("Created new connection between " + sectorBlueprint.Name + " and " + nextNode.Name, null, 1);
							}
							if (nextNode.Connections.Count >= settings.MaxConnections)
							{
								list2.Remove(nextNode);
							}
							if (sectorConnection2.TargetSector.Connections.Count >= settings.MaxConnections)
							{
								list2.Remove(sectorConnection2.TargetSector);
							}
						}
					}
				}
				num3++;
			}
			if (num3 >= num2)
			{
				Debug.LogWarning("Exceeed maximum number of attempts when tryingn to generate sector");
			}
			for (int num5 = 0; num5 < sectorBlueprints.Count; num5++)
			{
				sectorBlueprints[num5].Id = num5;
			}
			return sectorBlueprints;
		}

		private SectorBlueprint CreateAndAddNewSector(WorldGeneratorSettings settings, Vector3 newSectorPosition)
		{
			SectorBlueprint sectorBlueprint = CreateNewSector(newSectorPosition, settings, Random);
			NameSector(sectorBlueprint);
			AddSector(sectorBlueprint);
			return sectorBlueprint;
		}

		private void AddSector(SectorBlueprint newSectorNode)
		{
			sectorBlueprints.Add(newSectorNode);
			existingSectorNames.Add(newSectorNode.Name);
		}

		private void Clear()
		{
			sectorBlueprints.Clear();
			sectorLines.Clear();
			existingSectorNames.Clear();
		}

		private void ApplyConnection(SectorBlueprint node, SectorConnection connection)
		{
			node.Connections.Add(connection);
			connection.TargetSector.Connections.Add(new SectorConnection(node));
			Line item = new Line(new Vector2(node.Position.x, node.Position.z), new Vector2(connection.TargetSector.Position.x, connection.TargetSector.Position.z));
			sectorLines.Add(item);
		}

		private static bool SectorIntersectsArea(IEnumerable<Vector3> sectorPositions, Vector3 newPosition, float sectorRadius)
		{
			foreach (Vector3 sectorPosition in sectorPositions)
			{
				if (Vector3.Distance(sectorPosition, newPosition) < sectorRadius * 2f)
				{
					return true;
				}
			}
			return false;
		}

		private static bool AreConnected(SectorBlueprint node1, SectorBlueprint node2)
		{
			SectorBlueprint[] source = node1.Connections.Select((SectorConnection c) => c.TargetSector).ToArray();
			SectorBlueprint[] source2 = node2.Connections.Select((SectorConnection c) => c.TargetSector).ToArray();
			if (!source.Contains(node2))
			{
				return source2.Contains(node1);
			}
			return true;
		}

		private bool ConnectionIntersects(Vector3 pos1, Vector3 pos2, float lengthAddition, float otherLineLengthAddition)
		{
			Vector2 result = new Vector2(pos1.x, pos1.z);
			Vector2 result2 = new Vector2(pos2.x, pos2.z);
			if (lengthAddition > 0f)
			{
				ExtendLine(result, result2, lengthAddition, out result, out result2);
			}
			foreach (Line sectorLine in sectorLines)
			{
				Vector2 result3 = sectorLine.P1;
				Vector2 result4 = sectorLine.P2;
				if (otherLineLengthAddition > 0f)
				{
					ExtendLine(result3, result4, otherLineLengthAddition, out result3, out result4);
				}
				if (Line.Intersection(result3, result4, result, result2).HasValue)
				{
					return true;
				}
			}
			return false;
		}

		private static void ExtendLine(Vector2 p1, Vector2 p2, float lengthAddition, out Vector2 result1, out Vector2 result2)
		{
			Vector2 normalized = (p2 - p1).normalized;
			result1 = p1 - normalized * lengthAddition;
			result2 = p2 + normalized * lengthAddition;
		}

		private SectorBlueprint GetSnapNode(WorldGeneratorSettings settings, SectorBlueprint currentNode, Vector3 position)
		{
			return sectorBlueprints.Where((SectorBlueprint e) => CanSnapToNode(settings, currentNode, e)).GetRandom(Random);
		}

		private bool CanSnapToNode(WorldGeneratorSettings settings, SectorBlueprint currentNode, SectorBlueprint snappedNode)
		{
			if (snappedNode != currentNode && snappedNode.Connections.Count < settings.MaxConnections && !AreConnected(snappedNode, currentNode))
			{
				float num = Vector3.Distance(snappedNode.Position, currentNode.Position);
				float num2 = Mathf.Lerp(settings.MinSectorSnapRadius, settings.MaxSectorSnapRadius, 1f - settings.Snakiness);
				if (num < num2)
				{
					float tolerance = settings.MinAngleBetweenGates * (MathF.PI / 180f);
					if (!currentNode.ConnectionExistsAtDirection(snappedNode, tolerance) && !snappedNode.ConnectionExistsAtDirection(currentNode, tolerance) && !ConnectionIntersects(currentNode.Position, snappedNode.Position, 0f, settings.LineOvershootDistance))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void NameSector(SectorBlueprint sector)
		{
			if (UseRandomSectorNames)
			{
				sector.Name = GetRandomSectorName(Random, existingSectorNames);
				return;
			}
			string name = availableSectorNames.Dequeue();
			sector.Name = name;
		}

		public static SectorBlueprint CreateNewSector(Vector3 position, WorldGeneratorSettings settings, System.Random random)
		{
			return new SectorBlueprint
			{
				Position = position,
				GateDistanceMultiplier = random.NextFloat(settings.MinGateDistanceMultiplier, settings.MaxGateDistanceMultiplier)
			};
		}

		public static string GetRandomSectorName(System.Random random, IEnumerable<string> existingNames = null)
		{
			if (GameController.Instance.SectorNamer.TryGenerateName(existingNames, 20, GameController.Instance.SectorNamer.PrefixProbability, GameController.Instance.SectorNamer.NumberPosfixProbability, random, out var name))
			{
				return name;
			}
			return "Unknown " + random.Next(101, 873);
		}
	}
}
