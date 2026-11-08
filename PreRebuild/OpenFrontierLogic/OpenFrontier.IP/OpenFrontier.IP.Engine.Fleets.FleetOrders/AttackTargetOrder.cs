using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class AttackTargetOrder : FleetOrder
	{
		public float AttackPriority = 40f;

		public bool CompleteWhenNotHostile = true;

		public bool CompleteWhenChangedFaction = true;

		public Unit TargetUnit;

		public override FleetOrderType OrderType => FleetOrderType.AttackTarget;

		public override bool IsOffensive => true;

		public override string GetDescription()
		{
			return "Attack";
		}

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (TargetUnit != null && faction != null)
			{
				return "Attack " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(faction, currentSector, TargetUnit);
			}
			return GetDescription();
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveAttackTargetOrder activeAttackTargetOrder = gameObject.AddComponent<ActiveAttackTargetOrder>();
			activeAttackTargetOrder.AttackTargetObjective = this;
			return activeAttackTargetOrder;
		}
	}
}
