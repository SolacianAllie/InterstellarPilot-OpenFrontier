using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.ActiveObjectives
{
	public static class CollectCargoHelper
	{
		public static bool CheckFullCargoBay(Fleet fleet, float carogFullThreshold)
		{
			float num = 0f;
			float num2 = 0f;
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (ship != null && ship.Unit != null && ship.Unit.IsValidAndNotDestroyed && ship.TractorTurret != null)
				{
					num += ship.CargoBayComponent.Capacity;
					num2 += ship.CargoBayComponent.Usage;
				}
			}
			return num2 / num > carogFullThreshold;
		}

		public static bool IsCargoOwnershipCompatible(CargoOwnership cargoOwnership, CollectCargoOwnerMode collectionMode)
		{
			if (collectionMode == CollectCargoOwnerMode.AnyOwnership)
			{
				return true;
			}
			switch (cargoOwnership)
			{
			case CargoOwnership.NotOwned:
			case CargoOwnership.OwnedByUs:
				return true;
			case CargoOwnership.OwnedByHostile:
				return collectionMode == CollectCargoOwnerMode.OwnedNoFactionOrHostile;
			default:
				return false;
			}
		}

		public static CargoOwnership CalculateCargoOwnership(Cargo cargo, Faction ourFaction)
		{
			Faction faction = cargo.Unit.Faction;
			if (faction == null)
			{
				return CargoOwnership.NotOwned;
			}
			if (faction == ourFaction)
			{
				return CargoOwnership.OwnedByUs;
			}
			if (ourFaction.IsHostileTo(faction))
			{
				return CargoOwnership.OwnedByHostile;
			}
			return CargoOwnership.OwnedByOther;
		}
	}
}
