namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class TradeSearchOptions
	{
		private bool ignoreDistanceCost;

		private bool ignoreProfitability;

		public bool IgnoreProfitability
		{
			get
			{
				return ignoreProfitability;
			}
			set
			{
				ignoreProfitability = value;
			}
		}

		public bool IgnoreDistanceCost
		{
			get
			{
				return ignoreDistanceCost;
			}
			set
			{
				ignoreDistanceCost = value;
			}
		}
	}
}
