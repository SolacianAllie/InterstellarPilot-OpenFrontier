namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders
{
	public class ModelActiveFleetOrder
	{
		public double TimeoutTime { get; set; }

		public double StartTime { get; set; }

		public ModelFleetOrder Order { get; set; }
	}
}
