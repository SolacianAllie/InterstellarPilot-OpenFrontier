using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class DockOrder : FleetOrder
	{
		public Unit TargetDock;

		public override FleetOrderType OrderType => FleetOrderType.Dock;

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (faction != null && TargetDock != null && TargetDock.Sector != null)
			{
				TargetDock.GetFriendlyName();
				return "Dock at " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(faction, currentSector, TargetDock);
			}
			return "Dock";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveDockOrder activeDockOrder = gameObject.AddComponent<ActiveDockOrder>();
			activeDockOrder.DockObjective = this;
			return activeDockOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
