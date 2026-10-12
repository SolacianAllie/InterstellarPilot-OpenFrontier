using System.Collections.Generic;
using OpenFrontier.IP.Engine.WorldGeneration.Models;

namespace OpenFrontier.IP.Engine.WorldGeneration.Pathfinding
{
	public class JumpDistanceCalculator
	{
		private struct ConnectingSceneNode
		{
			public int Jumps;

			public SectorBlueprint Sector;

			public ConnectingSceneNode(SectorBlueprint sector, int jumps)
			{
				Sector = sector;
				Jumps = jumps;
			}
		}

		private static Dictionary<SectorBlueprint, int> connectingSectors = new Dictionary<SectorBlueprint, int>(100);

		private static Queue<ConnectingSceneNode> connectingSceneNodeQueue = new Queue<ConnectingSceneNode>();

		public static int GetJumpDistance(SectorBlueprint sector, SectorBlueprint target)
		{
			connectingSectors.Clear();
			connectingSceneNodeQueue.Clear();
			ConnectingSceneNode item = new ConnectingSceneNode(sector, 0);
			connectingSceneNodeQueue.Enqueue(item);
			while (connectingSceneNodeQueue.Count > 0)
			{
				ConnectingSceneNode connectingSceneNode = connectingSceneNodeQueue.Dequeue();
				if (connectingSceneNode.Sector == target)
				{
					return connectingSceneNode.Jumps;
				}
				if (connectingSectors.ContainsKey(connectingSceneNode.Sector))
				{
					continue;
				}
				connectingSectors.Add(connectingSceneNode.Sector, connectingSceneNode.Jumps);
				foreach (SectorConnection connection in connectingSceneNode.Sector.Connections)
				{
					if (!connectingSectors.ContainsKey(connection.TargetSector))
					{
						ConnectingSceneNode item2 = new ConnectingSceneNode(connection.TargetSector, connectingSceneNode.Jumps + 1);
						connectingSceneNodeQueue.Enqueue(item2);
					}
				}
			}
			return -1;
		}
	}
}
