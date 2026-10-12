using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public class ActiveWaitForAutoRepairOrder : ActiveFleetOrder
	{
		public WaitForAutoRepairOrder WaitForAutoRepairOrder;

		private float nextCheckConditionTime;

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (Time.time > nextCheckConditionTime)
			{
				if (!AnyFleetUnitRequiresRepair())
				{
					OnComplete();
				}
				nextCheckConditionTime = Time.time + 2f;
			}
		}

		private bool AnyFleetUnitRequiresRepair()
		{
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (ship != null && DoesUnitNeedRepair(ship.Unit))
				{
					return true;
				}
			}
			return false;
		}

		public bool DoesUnitNeedRepair(Unit unit)
		{
			return DoesUnitNeedRepair(unit, WaitForAutoRepairOrder.HullConditionThreshold, WaitForAutoRepairOrder.ComponentsConditionThreshold, WaitForAutoRepairOrder.ShieldConditionThreshold);
		}

		public static bool DoesUnitNeedRepair(Unit unit, float hullConditionThreshold, float componentsConditionThreshold, float shieldConditionThreshold)
		{
			if (unit.Destructable.HealthNormalized < hullConditionThreshold)
			{
				return true;
			}
			if (unit.Components.AnyComponentDamaged(1f - componentsConditionThreshold))
			{
				return true;
			}
			if (unit.Components.ShieldComponent != null && unit.Components.ShieldComponent.GetNormalizedShieldCharge() < shieldConditionThreshold)
			{
				return true;
			}
			return false;
		}

		public override void OnComplete(bool silent = false)
		{
			if (!silent && fleet.ShouldRaiseDialogEvent())
			{
				fleet.Leader.Person.RaiseDialogEventRandomly(EngineASX.Instance.DialogEvents.FleetRepairOrderComplete);
			}
			base.OnComplete(silent);
		}
	}
}
