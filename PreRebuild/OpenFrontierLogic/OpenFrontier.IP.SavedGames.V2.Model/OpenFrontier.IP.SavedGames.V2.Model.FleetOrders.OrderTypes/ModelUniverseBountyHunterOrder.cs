using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelUniverseBountyHunterOrder : ModelFleetOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.AutonomousBountyHunterObjective;
	}
}
