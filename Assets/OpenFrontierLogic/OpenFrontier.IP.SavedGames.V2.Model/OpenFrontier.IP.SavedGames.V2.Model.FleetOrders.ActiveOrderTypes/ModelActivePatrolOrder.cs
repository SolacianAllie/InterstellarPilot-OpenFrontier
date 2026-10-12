namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActivePatrolOrder : ModelActiveFleetOrder
	{
		public int PathDirection { get; set; }

		public int NodeIndex { get; set; }

		public int StartNodeIndex { get; set; }
	}
}
