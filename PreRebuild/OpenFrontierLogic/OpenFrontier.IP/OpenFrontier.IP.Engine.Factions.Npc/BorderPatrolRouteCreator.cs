using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.Factions.Npc
{
	public static class BorderPatrolRouteCreator
	{
		private struct BorderSector
		{
			public Sector Sector;

			public int JumpDist;

			public BorderSector(Sector sector, int jumpDist)
			{
				Sector = sector;
				JumpDist = jumpDist;
			}
		}

		public static PatrolOrder TryCreate(Faction faction, Sector startSector, int minPatrolScenesCount, int maxPatrolScenesCount, int minNodesInScene, int maxNodesInScene, int maxJumpDistFromStartScene, bool considerStationsAsNodes, Func<Unit, bool> stationNodeChecker = null)
		{
			List<BorderSector> list = (from e in faction.ControlledSectors
				where e.IsSectorAControlledBorderSector(faction)
				select new BorderSector(e, e.GetJumpDistanceTo(startSector)) into e
				orderby e.JumpDist
				select e).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			PatrolOrder patrolOrder = UnityObjectHelper.NewGameObject<PatrolOrder>();
			patrolOrder.CompletionMode = FleetOrderCompletionMode.Destroy;
			patrolOrder.IsLooping = true;
			patrolOrder.MaxDuration = 600f;
			patrolOrder.IsLoop = false;
			int minInclusive = Mathf.Max(minPatrolScenesCount, 1);
			int num = Mathf.Min(list.Count, Mathf.Max(minPatrolScenesCount, maxPatrolScenesCount));
			int num2 = UnityEngine.Random.Range(minInclusive, num + 1);
			Sector currentSector = startSector;
			List<Sector> list2 = new List<Sector>();
			do
			{
				BorderSector borderSector = list.OrderBy((BorderSector e) => e.Sector.GetJumpDistanceTo(currentSector)).First();
				list2.Add(borderSector.Sector);
				for (int num3 = 0; num3 < list.Count; num3++)
				{
					if (list[num3].Sector == borderSector.Sector)
					{
						list.RemoveAt(num3);
						break;
					}
				}
				currentSector = borderSector.Sector;
			}
			while (list.Count > 0 && list2.Count < num2);
			foreach (Sector item in list2)
			{
				int num4 = UnityEngine.Random.Range(minNodesInScene, maxNodesInScene + 1);
				List<Vector3> possibleSectorPosition = PatrolRouteCreator.GetPossibleSectorPosition(item, faction, num4, considerStationsAsNodes, stationNodeChecker);
				for (int num5 = 0; num5 < num4; num5++)
				{
					AIPatrolPathNode aIPatrolPathNode = new AIPatrolPathNode();
					aIPatrolPathNode.Sector = item;
					int index = UnityEngine.Random.Range(0, possibleSectorPosition.Count);
					aIPatrolPathNode.SectorPosition = possibleSectorPosition[index];
					possibleSectorPosition.RemoveAt(index);
					patrolOrder.Nodes.Add(aIPatrolPathNode);
				}
			}
			return patrolOrder;
		}
	}
}
