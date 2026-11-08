using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.Models;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelPatrolOrder : ModelFleetOrder
	{
		public int PathDirection { get; set; }

		public bool IsLooping { get; set; }

		public List<ModelPatrolPathNode> Nodes { get; set; } = new List<ModelPatrolPathNode>();

		public bool IsLoop { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.Patrol;
	}
}
