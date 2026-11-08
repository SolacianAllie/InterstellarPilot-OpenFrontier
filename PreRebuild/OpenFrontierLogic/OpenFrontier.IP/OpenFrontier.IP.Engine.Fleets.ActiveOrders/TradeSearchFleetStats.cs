namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class TradeSearchFleetStats
	{
		public float FleetTotalCargoSpace { get; set; }

		public float FleetFreeCargoSpace { get; set; }

		public float DistanceScorePerMetre { get; set; }

		public static TradeSearchFleetStats Create(Fleet fleet)
		{
			TradeSearchFleetStats tradeSearchFleetStats = new TradeSearchFleetStats();
			tradeSearchFleetStats.Update(fleet);
			return tradeSearchFleetStats;
		}

		public void Update(Fleet fleet)
		{
			FleetTotalCargoSpace = fleet.CalculateTotalCargoCapacity();
			FleetFreeCargoSpace = fleet.GetFreeCargoSpace();
			float cachedMinMoveSpeed = fleet.GetCachedMinMoveSpeed();
			DistanceScorePerMetre = fleet.GetDistanceScorePerMetreTravelled(cachedMinMoveSpeed);
		}
	}
}
