using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class CollectCargoOrder : FleetOrder
	{
		public Unit TargetUnit;

		public override FleetOrderType OrderType => FleetOrderType.CollectCargo;

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveCollectCargoOrder activeCollectCargoOrder = gameObject.AddComponent<ActiveCollectCargoOrder>();
			activeCollectCargoOrder.CollectCargoObjective = this;
			return activeCollectCargoOrder;
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (faction != null && TargetUnit != null && TargetUnit.IsValidAndNotDestroyed)
			{
				return "Collect " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(faction, currentSector, TargetUnit);
			}
			return "Collect cargo";
		}

		public override string GetDescription()
		{
			if (TargetUnit != null && TargetUnit.IsValidAndNotDestroyed)
			{
				return $"Collect {TargetUnit.CargoComponent.Quantity}x {TargetUnit.CargoComponent.CargoClass.ClassName} in {TargetUnit.Sector.Name}";
			}
			return "Collect cargo";
		}
	}
}
