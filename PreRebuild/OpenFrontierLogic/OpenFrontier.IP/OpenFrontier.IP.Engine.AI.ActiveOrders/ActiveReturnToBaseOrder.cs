using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveReturnToBaseOrder : ActiveFleetOrder
	{
		public ReturnToBaseOrder RTBObjective;

		public override bool IsValid
		{
			get
			{
				if (fleet.IsHomeBaseValid)
				{
					if (fleet.HomeBaseUnit != null)
					{
						return Fleet.IsHomeBaseUnitValid(fleet.HomeBaseUnit, fleet);
					}
					return true;
				}
				return false;
			}
		}

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (IsValid)
			{
				if (fleet.HomeBaseUnit != null && fleet.Settings.PreferToDock == DockedPreference.Dock && fleet.AllUnitsAtDock(fleet.HomeBaseUnit))
				{
					OnComplete();
				}
			}
			else
			{
				OnInvalid();
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (fleet.HomeBaseUnit == null || fleet.Settings.PreferToDock != DockedPreference.Dock)
			{
				OnComplete();
			}
		}

		protected override void resetTargetPosition()
		{
			if (IsValid)
			{
				if (fleet.HomeBaseUnit != null && fleet.Settings.PreferToDock == DockedPreference.Dock)
				{
					fleet.SetTargetToDock(this, fleet.HomeBaseUnit);
				}
				else if (fleet.HomeBaseUnit != null)
				{
					fleet.SetTargetToSectorPosition(this, fleet.HomeBaseUnit.Sector, fleet.HomeBaseUnit.SectorPosition);
					fleet.NavTarget.ArrivalThreshold = fleet.HomeBaseUnit.UnitClass.ShieldRingRadius * 2.5f;
				}
				else
				{
					fleet.SetTargetToSectorPosition(this, fleet.HomeSector, fleet.HomeSectorPosition);
				}
			}
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			if (fleet.IsHomeBaseValid)
			{
				if (fleet.HomeBaseUnit != null)
				{
					return "Move to " + UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, fleet.HomeBaseUnit);
				}
				return "Move to " + TextFormattingHelper.FormatSectorAndPosition(fleet.HomeSector, fleet.HomeSectorPosition);
			}
			return base.GetStatusTextInternal(localFaction);
		}

		public override void OnNpcUnableToDockAtNavpoint(NpcPilot npc, Unit dock, UnableToDockReason unableToDockReason)
		{
			if (dock == fleet.HomeBaseUnit)
			{
				OnComplete();
			}
			else
			{
				base.OnNpcUnableToDockAtNavpoint(npc, dock, unableToDockReason);
			}
		}
	}
}
