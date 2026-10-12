using System.Collections.Generic;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveAttackFleetOrder : ActiveFleetOrder
	{
		public AttackFleetOrder AttackGroupObjective;

		private Fleet target;

		public override bool IsValid
		{
			get
			{
				if (target != null)
				{
					return target.IsValid;
				}
				return false;
			}
		}

		public Fleet Target
		{
			get
			{
				return target;
			}
			set
			{
				if (target != value)
				{
					target = value;
					ResetTargetPosition();
				}
			}
		}

		public override bool IsComplete
		{
			get
			{
				if (!(target == null))
				{
					return !target.IsValid;
				}
				return true;
			}
		}

		public override void AddSpecificTargets(List<AISpecificTarget> targets)
		{
			if (!(target != null))
			{
				return;
			}
			foreach (NpcPilot npcPilot in target.NpcPilots)
			{
				if (npcPilot.CurrentUnit != null)
				{
					targets.Add(new AISpecificTarget
					{
						Unit = npcPilot.CurrentUnit,
						AdditionalPriority = AttackGroupObjective.AttackPriority
					});
				}
			}
		}

		protected override void onInit()
		{
			base.onInit();
			Target = AttackGroupObjective.Target;
		}

		protected override void tick(float elapsedTime)
		{
			if (fleet.IsIdle && target != null)
			{
				ResetTargetPosition();
			}
		}

		protected override void resetTargetPosition()
		{
			if (IsValid)
			{
				fleet.SetTargetToFleet(this, target);
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
