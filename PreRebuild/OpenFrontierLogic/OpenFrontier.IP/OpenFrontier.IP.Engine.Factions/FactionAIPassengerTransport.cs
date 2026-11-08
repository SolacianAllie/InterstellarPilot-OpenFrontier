using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionAIPassengerTransport : FactionAIBase
	{
		public override FactionAIType AIType => FactionAIType.PassengerTransport;

		protected override void AssignOrdersToTimedOutFleet(Fleet fleet, Unit groupLeaderUnit)
		{
			if (fleet.FleetStrategy == FactionStrategy.PassengerTransport)
			{
				OrderExplore(fleet);
			}
			else
			{
				base.AssignOrdersToTimedOutFleet(fleet, groupLeaderUnit);
			}
		}
	}
}
