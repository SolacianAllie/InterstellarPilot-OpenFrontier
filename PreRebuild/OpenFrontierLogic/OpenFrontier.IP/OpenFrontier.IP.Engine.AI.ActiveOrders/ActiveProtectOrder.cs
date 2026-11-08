using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Fleets;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveProtectOrder : ActiveMoveToOrder
	{
		public ProtectOrder ProtectObjective;

		protected override void onInit()
		{
			base.onInit();
		}

		public override void OnNpcUnableToDockAtNavpoint(NpcPilot npc, Unit dock, UnableToDockReason unableToDockReason)
		{
		}

		protected override void onFleetReachedTarget()
		{
		}

		public override float GetAdditionalCombatTargetPriority(Unit combatTarget)
		{
			if (ProtectObjective.Target != null && ProtectObjective.Target.TargetUnit != null && combatTarget.IsAttackingUnit(ProtectObjective.Target.TargetUnit))
			{
				float? lastTimeOfAttack = combatTarget.Engine.UnitRecentAttacksLogsByInflictor.GetLastTimeOfAttack(combatTarget, ProtectObjective.Target.TargetUnit);
				if (lastTimeOfAttack.HasValue)
				{
					float num = Time.time - lastTimeOfAttack.Value;
					float num2 = 30f;
					return 10f + (1f - Mathf.Clamp01(num / num2)) * 15f;
				}
			}
			else if (ProtectObjective.Target != null && ProtectObjective.Target.TargetFleet != null && combatTarget.IsAttackingFleet(ProtectObjective.Target.TargetFleet))
			{
				return 20f;
			}
			return base.GetAdditionalCombatTargetPriority(combatTarget);
		}

		public override bool RequestInterception(NpcPilot requestor, Unit target, float distance, float requestorsTargetScore, bool priorityTarget)
		{
			if (ProtectObjective.Target != null && ProtectObjective.Target.GetTargetSector() != target.Sector)
			{
				return false;
			}
			bool priorityTarget2 = priorityTarget || (ProtectObjective.Target.TargetUnit != null && target.IsAttackingUnit(ProtectObjective.Target.TargetUnit));
			return base.RequestInterception(requestor, target, distance, requestorsTargetScore, priorityTarget2);
		}

		private bool TryResetTargetToDock()
		{
			if (fleet.Settings.PreferToDock == DockedPreference.Dock)
			{
				Unit dockableTargetUnit = GetDockableTargetUnit();
				if (dockableTargetUnit != null)
				{
					fleet.SetTargetToDock(this, dockableTargetUnit);
					return true;
				}
			}
			return false;
		}

		private Unit GetDockableTargetUnit()
		{
			if (MoveToObjective.Target != null && MoveToObjective.Target.TargetObject is Unit)
			{
				Unit unit = (Unit)MoveToObjective.Target.TargetObject;
				if (unit.IsDockable)
				{
					return unit;
				}
			}
			return null;
		}

		protected override void resetTargetPosition()
		{
			if (!TryResetTargetToDock())
			{
				base.resetTargetPosition();
			}
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
