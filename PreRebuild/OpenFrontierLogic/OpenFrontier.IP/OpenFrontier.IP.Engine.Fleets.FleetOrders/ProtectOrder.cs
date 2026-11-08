using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Fleets.FleetOrders
{
	public class ProtectOrder : MoveToOrder
	{
		public override FleetOrderType OrderType => FleetOrderType.Protect;

		public override string GetDescriptionForFaction(Faction faction, Sector currentSector)
		{
			if (faction != null && Target != null)
			{
				if (Target.TargetUnit != null)
				{
					return "Protect " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(faction, currentSector, Target.TargetUnit);
				}
				if (Target.TargetFleet != null)
				{
					return "Protect [" + Target.TargetFleet.GetFriendlyName() + "]";
				}
				if (Target.Sector != null)
				{
					return "Protect [" + TextFormattingHelper.FormatSectorAndPosition(Target.Sector, Target.SectorPosition) + "]";
				}
			}
			return GetDescription();
		}

		public override string GetDescription()
		{
			return "Protect";
		}

		protected override ActiveFleetOrder createActiveFleetOrder()
		{
			ActiveProtectOrder activeProtectOrder = gameObject.AddComponent<ActiveProtectOrder>();
			activeProtectOrder.MoveToObjective = this;
			activeProtectOrder.ProtectObjective = this;
			return activeProtectOrder;
		}

		public override bool IsRepeatable()
		{
			return true;
		}

		public override string Validate(Fleet fleet)
		{
			if (Target == null)
			{
				return "Null sector target";
			}
			if (Target.GetTargetSector() == null)
			{
				return "Null target sector";
			}
			if (Target.TargetUnit != null && Target.TargetUnit.GetFleet() == fleet)
			{
				return $"Should not be assigning order to protect unit {Target.TargetUnit} in same fleet";
			}
			return null;
		}
	}
}
