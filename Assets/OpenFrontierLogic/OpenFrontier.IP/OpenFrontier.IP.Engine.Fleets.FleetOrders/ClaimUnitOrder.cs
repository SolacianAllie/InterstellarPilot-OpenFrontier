using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ClaimUnitOrder : FleetOrder
	{
		public Unit TargetUnit;

		public override FleetOrderType OrderType => FleetOrderType.ClaimUnit;

		public override bool CanSpendCredits => false;

		public override string GetDescription()
		{
			if (TargetUnit != null)
			{
				if (TargetUnit.Sector != null)
				{
					return "Claim " + TargetUnit.GetClassAndSeriesName() + " in " + TargetUnit.Sector.Name;
				}
				return "Claim " + TargetUnit.GetClassAndSeriesName();
			}
			return "Claim unit";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveClaimUnitOrder activeClaimUnitOrder = gameObject.AddComponent<ActiveClaimUnitOrder>();
			activeClaimUnitOrder.ClaimUnitOrder = this;
			return activeClaimUnitOrder;
		}

		public override bool IsRepeatable()
		{
			return false;
		}
	}
}
