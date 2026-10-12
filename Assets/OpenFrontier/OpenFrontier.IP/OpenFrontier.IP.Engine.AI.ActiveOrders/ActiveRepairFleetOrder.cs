using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveRepairFleetOrder : ActiveFleetOrder
	{
		private bool repairHasInsufficientCredits;

		private Unit currentRepairLocation;

		public RepairFleetOrder RepairGroupObjective;

		private ActiveRepairFleetOrderState state;

		private const float attemptRepairsInterval = 3f;

		private float nextAttemptRepairsTime;

		public Unit CurrentRepairLocation
		{
			get
			{
				return currentRepairLocation;
			}
			set
			{
				if (currentRepairLocation != value)
				{
					currentRepairLocation = value;
					if (currentRepairLocation != null)
					{
						ResetTimeoutTime();
					}
					ResetTargetPosition();
				}
			}
		}

		public ActiveRepairFleetOrderState RepairState
		{
			get
			{
				return state;
			}
			set
			{
				if (state != value)
				{
					state = value;
					switch (state)
					{
					case ActiveRepairFleetOrderState.MoveToRepairStation:
						IsIdle = false;
						break;
					case ActiveRepairFleetOrderState.None:
						OnIdle();
						break;
					case ActiveRepairFleetOrderState.Repairing:
						OnIdle();
						break;
					}
					repairHasInsufficientCredits = false;
				}
			}
		}

		protected override void tick(float elapsedTime)
		{
			switch (state)
			{
			case ActiveRepairFleetOrderState.None:
				if (currentRepairLocation != null && IsRepairLocationValid(currentRepairLocation))
				{
					RepairState = ActiveRepairFleetOrderState.MoveToRepairStation;
				}
				else
				{
					OnRepairLocationNullOrInvalid();
				}
				break;
			case ActiveRepairFleetOrderState.MoveToRepairStation:
				if (currentRepairLocation == null || !IsRepairLocationValid(currentRepairLocation))
				{
					OnRepairLocationNullOrInvalid();
				}
				else if (fleet.AllUnitsAtDock(currentRepairLocation))
				{
					RepairState = ActiveRepairFleetOrderState.Repairing;
				}
				break;
			case ActiveRepairFleetOrderState.Repairing:
				if (currentRepairLocation != null && IsRepairLocationValid(currentRepairLocation))
				{
					if (!(Time.time > nextAttemptRepairsTime))
					{
						break;
					}
					if (fleet.AllUnitsAtDock(currentRepairLocation))
					{
						int num = 0;
						for (int i = 0; i < fleet.NpcPilots.Count; i++)
						{
							NpcPilot npcPilot = fleet.NpcPilots[i];
							if (!(npcPilot.CurrentUnit != null) || !UnitRequiresRepair(npcPilot.CurrentUnit))
							{
								continue;
							}
							num++;
							if (HasEnoughCreditsToRepairUnit(npcPilot.CurrentUnit, currentRepairLocation))
							{
								PerformRepairs(npcPilot.CurrentUnit);
								num--;
								continue;
							}
							repairHasInsufficientCredits = true;
							if (RepairGroupObjective.InsufficientCreditsMode == InsufficientCreditsMode.Abort)
							{
								OnInvalid("Insufficient Credits");
								return;
							}
						}
						if (num == 0)
						{
							OnComplete();
						}
					}
					nextAttemptRepairsTime = Time.time + 3f;
				}
				else
				{
					OnRepairLocationNullOrInvalid();
				}
				break;
			}
		}

		private bool UnitRequiresRepair(Unit unit)
		{
			return unit.RequiresRepair();
		}

		private void PerformRepairs(Unit unit)
		{
			RepairHelper.RepairAll(currentRepairLocation.Faction, fleet.Faction, unit, currentRepairLocation);
		}

		protected virtual void OnRepairLocationNullOrInvalid()
		{
			CurrentRepairLocation = null;
			RepairState = ActiveRepairFleetOrderState.None;
		}

		private int GetCostToRepairUnit(Unit unit, Unit repairingUnit)
		{
			return RepairHelper.GetCostToRepairAllDamage(currentRepairLocation.Faction, fleet.Faction, repairingUnit, unit);
		}

		private bool HasEnoughCreditsToRepairUnit(Unit unit, Unit repairingUnit)
		{
			int costToRepairUnit = GetCostToRepairUnit(unit, repairingUnit);
			return IsAffordableConsideringReserve(costToRepairUnit);
		}

		protected override void resetTargetPosition()
		{
			base.resetTargetPosition();
			if (currentRepairLocation != null && IsRepairLocationValid(currentRepairLocation))
			{
				fleet.SetTargetToDock(this, currentRepairLocation);
			}
		}

		public bool IsRepairLocationValid(Unit unit)
		{
			if (unit != null && unit.IsDockable && unit.UnitClass.HasRepairFacilities && HasGoodRelationsWithRepairLocation(unit))
			{
				return true;
			}
			return false;
		}

		private bool HasGoodRelationsWithRepairLocation(Unit unit)
		{
			if (unit.Faction == fleet.Faction)
			{
				return true;
			}
			Faction faction = fleet.Faction;
			if (unit.IsHostileToOrAlwaysHostileToTwoWay(faction))
			{
				return false;
			}
			if (unit.Faction.GetOpinion(faction) > -0.2f)
			{
				return true;
			}
			return false;
		}

		protected override string GetStatusTextInternal(Faction localFaction)
		{
			switch (state)
			{
			case ActiveRepairFleetOrderState.None:
				return "Looking for repair station";
			case ActiveRepairFleetOrderState.MoveToRepairStation:
				if (currentRepairLocation != null)
				{
					return $"Moving to {UnitNamer.GetFriendlyNameInBracketsAndFactionShortNameAndLastKnownSector(localFaction, fleet.Sector, currentRepairLocation)}";
				}
				break;
			case ActiveRepairFleetOrderState.Repairing:
				if (repairHasInsufficientCredits)
				{
					return "Insufficient credits";
				}
				return "Repair In progress";
			}
			return base.GetStatusTextInternal(localFaction);
		}

		public override DockedPreference GetPreferToDockWhenIdle()
		{
			if (state == ActiveRepairFleetOrderState.Repairing)
			{
				return DockedPreference.Dock;
			}
			return base.GetPreferToDockWhenIdle();
		}

		public override bool RequestUndock(NpcPilot npcPilot)
		{
			if (state == ActiveRepairFleetOrderState.Repairing)
			{
				return false;
			}
			return true;
		}
	}
}
