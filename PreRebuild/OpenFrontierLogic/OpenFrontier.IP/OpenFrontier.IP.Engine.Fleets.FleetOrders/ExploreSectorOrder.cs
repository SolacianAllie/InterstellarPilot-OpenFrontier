using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ExploreSectorOrder : FleetOrder
	{
		public Sector Sector;

		public override FleetOrderType OrderType => FleetOrderType.ExploreSector;

		public override string GetDescription()
		{
			if (Sector?.Name != null)
			{
				return "Explore " + Sector.Name;
			}
			return "Explore";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveExploreSectorOrder activeExploreSectorOrder = gameObject.AddComponent<ActiveExploreSectorOrder>();
			activeExploreSectorOrder.ExploreSectorObjective = this;
			return activeExploreSectorOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
