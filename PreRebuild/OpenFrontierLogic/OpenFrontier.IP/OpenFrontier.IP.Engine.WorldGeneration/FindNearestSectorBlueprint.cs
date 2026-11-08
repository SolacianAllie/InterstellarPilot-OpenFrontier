using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.WorldGeneration.Models;

namespace OpenFrontier.IP.Engine.WorldGeneration
{
	public class FindNearestSectorBlueprint
	{
		private struct SectorBlueprintNode
		{
			public SectorBlueprint SectorBlueprint;

			public int JumpDistance;

			public SectorBlueprintNode(SectorBlueprint sectorBlueprint, int jumpDistance)
			{
				this = default;
				SectorBlueprint = sectorBlueprint;
				JumpDistance = jumpDistance;
			}
		}

		private static Queue<SectorBlueprintNode> nodeQueue = new Queue<SectorBlueprintNode>(1000);

		private static HashSet<int> visitedNodes = new HashSet<int>(20);

		public static (SectorBlueprint, int?)? Find(SectorBlueprint startSector, IEnumerable<SectorBlueprint> sectors, Func<SectorBlueprint, bool> predicate, int maxJumpDistance)
		{
			if (predicate(startSector))
			{
				return (startSector, 0);
			}
			visitedNodes.Clear();
			nodeQueue.Clear();
			nodeQueue.Enqueue(new SectorBlueprintNode(startSector, 0));
			while (nodeQueue.Count > 0)
			{
				SectorBlueprintNode sectorBlueprintNode = nodeQueue.Dequeue();
				SectorBlueprint sectorBlueprint = sectorBlueprintNode.SectorBlueprint;
				if (predicate(sectorBlueprint))
				{
					return (sectorBlueprint, sectorBlueprintNode.JumpDistance);
				}
				if (sectorBlueprintNode.JumpDistance >= maxJumpDistance)
				{
					continue;
				}
				foreach (SectorConnection connection in sectorBlueprint.Connections)
				{
					if (!visitedNodes.Contains(connection.TargetSector.Id))
					{
						visitedNodes.Add(connection.TargetSector.Id);
						nodeQueue.Enqueue(new SectorBlueprintNode(connection.TargetSector, sectorBlueprintNode.JumpDistance + 1));
					}
				}
			}
			return null;
		}
	}
}
