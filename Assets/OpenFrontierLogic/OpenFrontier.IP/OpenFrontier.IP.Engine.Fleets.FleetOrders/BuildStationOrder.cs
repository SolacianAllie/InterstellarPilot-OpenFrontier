using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class BuildStationOrder : FleetOrder
	{
		public InsufficientCreditsMode InsufficientCreditsMode;

		public UnitClass UnitClass;

		public Sector Sector;

		public Vector3 SectorPosition = Vector3.zero;

		public override FleetOrderType OrderType => FleetOrderType.BuildStation;

		public override bool CanSpendCredits => true;

		public override string GetDescription()
		{
			if (UnitClass != null)
			{
				if (Sector != null)
				{
					return "Build " + UnitClass.GetClassAndSeriesName() + " in " + Sector.Name;
				}
				return "Build " + UnitClass.GetClassAndSeriesName();
			}
			return "Build station";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveBuildStationOrder activeBuildStationOrder = gameObject.AddComponent<ActiveBuildStationOrder>();
			activeBuildStationOrder.BuildStationOrder = this;
			return activeBuildStationOrder;
		}

		public override bool IsRepeatable()
		{
			return false;
		}
	}
}
