using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveAttackTargetOrder : ActiveFleetOrder
	{
		public AttackTargetOrder AttackTargetObjective;

		private Unit targetUnit;

		private Vector3? currentDockSectorPosition;

		private Faction originalTargetFaction;

		public override bool IsValid => targetUnit != null;

		public Unit Target
		{
			get
			{
				return targetUnit;
			}
			set
			{
				if (targetUnit != value)
				{
					targetUnit = value;
					ResetTargetPosition();
				}
			}
		}

		public bool IsTargetDocked
		{
			get
			{
				if (targetUnit != null && targetUnit.IsValidAndNotDestroyed && targetUnit.Components != null)
				{
					return targetUnit.Components.DockUnit;
				}
				return false;
			}
		}

		public override bool IsComplete
		{
			get
			{
				if (!(targetUnit == null))
				{
					return targetUnit.IsDestroyed;
				}
				return true;
			}
		}

		public Faction OriginalTargetFaction
		{
			get
			{
				return originalTargetFaction;
			}
			set
			{
				originalTargetFaction = value;
			}
		}

		public override float GetAdditionalCombatTargetPriority(Unit combatTarget)
		{
			if (targetUnit != null && targetUnit.IsValidAndNotDestroyed && targetUnit.IsDocked)
			{
				Unit dockUnit = targetUnit.Components.DockUnit;
				if (combatTarget == dockUnit && fleet.Faction.IsHostileTo(dockUnit))
				{
					return 5f;
				}
			}
			return base.GetAdditionalCombatTargetPriority(combatTarget);
		}

		public override void AddSpecificTargets(List<AISpecificTarget> targets)
		{
			if (targetUnit != null && !targetUnit.IsDocked)
			{
				targets.Add(new AISpecificTarget
				{
					Unit = targetUnit,
					AdditionalPriority = AttackTargetObjective.AttackPriority
				});
			}
		}

		protected override void onInit()
		{
			base.onInit();
			Target = AttackTargetObjective.TargetUnit;
			if (Target != null)
			{
				originalTargetFaction = Target.Faction;
			}
		}

		protected override void tick(float elapsedTime)
		{
			if (AttackTargetObjective.CompleteWhenNotHostile && (fleet.Faction == null || !fleet.Faction.IsHostileTo(targetUnit)))
			{
				OnComplete();
			}
			else if (AttackTargetObjective.CompleteWhenChangedFaction && targetUnit.Faction != originalTargetFaction)
			{
				OnComplete();
			}
			else if (fleet.IsIdle && targetUnit != null)
			{
				ResetTargetPosition();
			}
		}

		protected override void onFleetReachedTarget()
		{
			base.onFleetReachedTarget();
			if (IsTargetDocked)
			{
				fleet.Faction.SetNeutralityWith(targetUnit.Faction, Neutrality.Hostile);
				if (fleet.LeaderUnit.IsInActiveSector)
				{
					fleet.Leader.Person.RaiseDialogEventRandomly(Engine.DialogEvents.AttackTargetIsDockedEvent);
				}
				AssignRandomPositionNearDockedTarget();
			}
		}

		private void AssignRandomPositionNearDockedTarget()
		{
			currentDockSectorPosition = GetRandomSectorPositionNearTargetDock();
		}

		private Vector3 GetRandomSectorPositionNearTargetDock()
		{
			Unit rootUnit = targetUnit.GetRootUnit();
			Vector3 checkSectorPosition = rootUnit.SectorPosition + Geometry.RandomXZUnitVector() * Mathf.Max(200f, rootUnit.UnitClass.ShieldRingRadius * 2f);
			return PhysicsNonOverlappingPositionFinder.FindSectorPosition(targetUnit.Sector, checkSectorPosition, 20f, GameController.Instance.NonOVerlappingUnitsMask);
		}

		protected override void resetTargetPosition()
		{
			if (!(targetUnit != null) || !targetUnit.IsValidAndNotDestroyed)
			{
				return;
			}
			if (targetUnit.IsDocked && targetUnit.Sector == fleet.Sector && Vector3.Distance(fleet.SectorPosition, targetUnit.SectorPosition) < 500f)
			{
				if (!currentDockSectorPosition.HasValue)
				{
					AssignRandomPositionNearDockedTarget();
				}
				fleet.SetTargetToSectorPosition(this, targetUnit.Sector, currentDockSectorPosition.Value);
				fleet.NavTarget.PreferredMoveSpeedMultiplier = 0.2f;
			}
			else
			{
				fleet.SetTargetToUnit(this, targetUnit);
			}
		}

		public override float ScoreCargoToCollect(NpcPilot aIUnitController, Unit cargoUnit, CargoClass cargoClass, int availableQuantity, CargoOwnership cargoOwnership)
		{
			if (cargoUnit == AttackTargetObjective.TargetUnit)
			{
				return -1000f;
			}
			return base.ScoreCargoToCollect(aIUnitController, cargoUnit, cargoClass, availableQuantity, cargoOwnership);
		}

		public override bool RequestInterception(NpcPilot requestor, Unit target, float distance, float requestorsTargetScore, bool priorityTarget)
		{
			if (targetUnit != null)
			{
				if (target == targetUnit)
				{
					return true;
				}
				if (AttackTargetObjective.AttackPriority > 20f)
				{
					if (targetUnit.Sector != target.Sector)
					{
						return false;
					}
					if (requestorsTargetScore > 1000f)
					{
						return false;
					}
				}
			}
			return base.RequestInterception(requestor, target, distance, requestorsTargetScore, priorityTarget);
		}

		public override bool CanFleetAttack()
		{
			return true;
		}

		public override bool CanFleetIntercept()
		{
			return true;
		}
	}
}
