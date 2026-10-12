using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveWaitOrder : ActiveFleetOrder
	{
		public double WaitExpiryTime;

		public WaitOrder WaitObjective;

		public override bool IsComplete => Engine.ScenarioElapsedTime > WaitExpiryTime;

		protected override void onInit()
		{
			base.onInit();
			WaitExpiryTime = Engine.ScenarioElapsedTime + (double)WaitObjective.WaitTime;
		}

		public override DockedPreference GetPreferToDockWhenIdle()
		{
			return DockedPreference.DontCare;
		}
	}
}
