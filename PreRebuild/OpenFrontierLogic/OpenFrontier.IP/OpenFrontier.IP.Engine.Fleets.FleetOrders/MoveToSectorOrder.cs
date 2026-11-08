using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class MoveToSectorOrder : FleetOrder
	{
		public Sector TargetSector;

		public override FleetOrderType OrderType => FleetOrderType.MoveToSector;

		public override string GetDescription()
		{
			if (TargetSector != null)
			{
				return "Move to " + TargetSector.Name;
			}
			return "Move to [Unknown sector]";
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			Sector targetSector = TargetSector;
			if (targetSector != null)
			{
				return "Move to " + UnitNamer.GetSectorNameAndDistanceForFaction(faction, currentSector, targetSector);
			}
			return GetDescription();
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveMoveToSectorOrder activeMoveToSectorOrder = gameObject.AddComponent<ActiveMoveToSectorOrder>();
			activeMoveToSectorOrder.MoveToSectorOrder = this;
			return activeMoveToSectorOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}
	}
}
