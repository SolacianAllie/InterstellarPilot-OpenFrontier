using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActivePatrolOrder : ActiveFleetOrder
	{
		public int NodeIndex;

		public int StartNodeIndex;

		public int PathDirection = 1;

		public PatrolOrderBase PatrolObjective;

		public override bool IsValid => PatrolObjective.NodeCount > 0;

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			if (PatrolObjective.NodeCount > 0)
			{
				if (NodeIndex >= 0 && NodeIndex < PatrolObjective.NodeCount)
				{
					IPatrolPathNode nodeAtIndex = PatrolObjective.GetNodeAtIndex(NodeIndex);
					return $"Move to waypoint {NodeIndex + 1} in {nodeAtIndex.Sector.Name}: {{{TextFormattingHelper.FormatSectorPosition(nodeAtIndex.SectorPosition)}}}";
				}
				return "Move to waypoint";
			}
			return base.GetStatusTextInternal(localFaction);
		}

		protected override void onInit()
		{
			base.onInit();
			if (!(fleet.Sector != null))
			{
				return;
			}
			NodeIndex = PatrolObjective.FindNearestNodeIndex(fleet.Sector, fleet.SectorPosition);
			StartNodeIndex = NodeIndex;
			if (NodeIndex > -1 && !PatrolObjective.IsPathALoop)
			{
				if (NodeIndex < PatrolObjective.NodeCount / 2)
				{
					PathDirection = 1;
				}
				else
				{
					PathDirection = -1;
				}
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			NodeIndex += PathDirection;
			if (NodeIndex < 0 || NodeIndex >= PatrolObjective.NodeCount)
			{
				if (NodeIndex >= PatrolObjective.NodeCount)
				{
					if (PatrolObjective.IsPathALoop)
					{
						NodeIndex = 0;
					}
					else
					{
						PathDirection *= -1;
						NodeIndex = PatrolObjective.NodeCount - 1;
					}
				}
				else if (PatrolObjective.IsPathALoop)
				{
					NodeIndex = PatrolObjective.NodeCount - 1;
				}
				else
				{
					PathDirection *= -1;
					NodeIndex = 0;
				}
				if (!PatrolObjective.IsLooping && NodeIndex == StartNodeIndex)
				{
					OnComplete();
				}
				else
				{
					ResetTargetPosition();
				}
			}
			else
			{
				ResetTargetPosition();
			}
		}

		protected override void resetTargetPosition()
		{
			if (PatrolObjective.IsValidIndex(NodeIndex))
			{
				IPatrolPathNode nodeAtIndex = PatrolObjective.GetNodeAtIndex(NodeIndex);
				fleet.SetTargetToSectorPosition(this, nodeAtIndex.Sector, nodeAtIndex.SectorPosition);
			}
		}

		private void OnNodeChanged()
		{
			Debug.Log($"Change aipath node index to {NodeIndex}", this);
			ResetTargetPosition();
		}
	}
}
