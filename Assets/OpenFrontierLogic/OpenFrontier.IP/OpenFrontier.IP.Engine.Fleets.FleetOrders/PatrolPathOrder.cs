using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class PatrolPathOrder : PatrolOrderBase
	{
		public AIPatrolPath PatrolPath;

		public override FleetOrderType OrderType => FleetOrderType.PatrolPath;

		public override int NodeCount => PatrolPath.NodeCount;

		public override bool IsPathALoop => PatrolPath.IsLoop;

		public override IPatrolPathNode GetNodeAtIndex(int index)
		{
			return PatrolPath.GetNodeAtIndex(index);
		}

		public override string GetDescription()
		{
			return "Patrolling";
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
