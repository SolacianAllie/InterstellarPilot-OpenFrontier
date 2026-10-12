using OpenFrontier.IP.Engine.AI.ActiveOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public abstract class PatrolOrderBase : FleetOrder
	{
		public bool IsLooping = true;

		public int PathDirection = 1;

		public abstract bool IsPathALoop { get; }

		public abstract int NodeCount { get; }

		public override bool IsOffensive => true;

		public int FindNearestNodeIndex(Sector sector, Vector3 sectorPosition)
		{
			int num = -1;
			float num2 = 0f;
			int nodeCount = NodeCount;
			for (int i = 0; i < nodeCount; i++)
			{
				IPatrolPathNode nodeAtIndex = GetNodeAtIndex(i);
				int jumpDistanceTo = sector.GetJumpDistanceTo(nodeAtIndex.Sector);
				float num3 = ((jumpDistanceTo != 0) ? ((float)jumpDistanceTo * 100000f) : Vector3.Distance(sectorPosition, nodeAtIndex.SectorPosition));
				if (num == -1 || num3 < num2)
				{
					num = i;
					num2 = num3;
				}
			}
			return num;
		}

		public bool IsValidIndex(int index)
		{
			if (index >= 0)
			{
				return index < NodeCount;
			}
			return false;
		}

		public abstract IPatrolPathNode GetNodeAtIndex(int index);

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActivePatrolOrder activePatrolOrder = gameObject.AddComponent<ActivePatrolOrder>();
			activePatrolOrder.PatrolObjective = this;
			return activePatrolOrder;
		}

		public override string GetDescription()
		{
			return "Patrol";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
