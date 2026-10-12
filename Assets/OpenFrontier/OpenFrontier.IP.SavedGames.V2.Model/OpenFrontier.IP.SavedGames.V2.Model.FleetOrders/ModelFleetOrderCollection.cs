using System.Collections.Generic;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders
{
	public class ModelFleetOrderCollection
	{
		public List<ModelFleetOrder> Orders { get; set; } = new List<ModelFleetOrder>();

		public List<ModelFleetOrder> QueuedOrders { get; set; } = new List<ModelFleetOrder>();

		public ModelActiveFleetOrder CurrentOrder { get; set; }
	}
}
