using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveCollectCargoOrder : ActiveFleetOrder
	{
		public CollectCargoOrder CollectCargoObjective;

		protected override void tick(float elapsedTime)
		{
			if (CollectCargoObjective.TargetUnit == null || !CollectCargoObjective.TargetUnit.IsValidAndNotDestroyed || CollectCargoObjective.TargetUnit.CargoComponent == null || CollectCargoObjective.TargetUnit.CargoComponent.Quantity == 0)
			{
				OnInvalid();
			}
			else if (fleet.GetCachedFreeCargoSpace() / CollectCargoObjective.TargetUnit.CargoComponent.Volume == 0f)
			{
				OnInvalid("No free cargo space");
			}
		}

		protected override void resetTargetPosition()
		{
			base.resetTargetPosition();
			if (CollectCargoObjective.TargetUnit != null && CollectCargoObjective.TargetUnit.IsValidAndNotDestroyed)
			{
				fleet.SetTargetToUnit(this, CollectCargoObjective.TargetUnit);
				fleet.NavTarget.ArrivalThreshold = 50f;
			}
		}

		public override void OnCargoCollectedByShip(Unit collectingUnit, Cargo cargo)
		{
			base.OnCargoCollectedByShip(collectingUnit, cargo);
			if (cargo != null && cargo.Unit == CollectCargoObjective.TargetUnit)
			{
				OnComplete();
			}
		}

		public override float ScoreCargoToCollect(NpcPilot aIUnitController, Unit cargoUnit, CargoClass cargoClass, int availableQuantity, CargoOwnership cargoOwnership)
		{
			if (cargoUnit != CollectCargoObjective.TargetUnit)
			{
				return float.MinValue;
			}
			return float.MaxValue;
		}

		public override FleetOrderCloakPreference GetCloakPreference()
		{
			if (CollectCargoObjective.TargetUnit != null && CollectCargoObjective.TargetUnit.Sector != fleet.Sector)
			{
				return FleetOrderCloakPreference.DontCare;
			}
			return FleetOrderCloakPreference.None;
		}
	}
}
