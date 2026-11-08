using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class TractorTurretComponent : LaserTurretComponent
	{
		public TractorTurretClass TractorTurretClass;

		private Unit tractorTarget;

		public bool IsPullingUnit
		{
			get
			{
				if (tractorTarget != null)
				{
					return tractorTarget.UnitType != UnitType.Cargo;
				}
				return false;
			}
		}

		public override bool IsFiring
		{
			get
			{
				if (!base.IsFiring)
				{
					return IsPullingUnit;
				}
				return true;
			}
		}

		public Unit TractorTarget
		{
			get
			{
				return tractorTarget;
			}
			set
			{
				if (tractorTarget != value)
				{
					Unit unit = tractorTarget;
					tractorTarget = value;
					if (unit != null && unit.Tractorer == this)
					{
						unit.Tractorer = null;
					}
					if (tractorTarget != null)
					{
						tractorTarget.Tractorer = this;
					}
				}
			}
		}

		public void PullUnit(Unit unit)
		{
			StopPullingUnit();
			TractorTarget = unit;
		}

		public bool CanPullUnitThroughWormhole(Unit unit)
		{
			if (unit.UnitType == UnitType.Cargo || unit.UnitType == UnitType.Debris || unit.UnitType == UnitType.Ship)
			{
				if (!(unit.Components == null))
				{
					return unit.Components.PilotPerson == null;
				}
				return true;
			}
			return false;
		}

		public void StopPullingUnit()
		{
			TractorTarget = null;
		}

		public override bool CanCancelFire()
		{
			return true;
		}

		public void TractorInCargo(Cargo cargo)
		{
			Unit unit = Unit;
			Faction faction = cargo.Unit.Faction;
			int quantity = cargo.Quantity;
			int transferredUnits = cargo.CollectAndDestroyIfEmpty(unit.CargoBayComponent);
			Engine.HandleCargoTractoredByUnit(cargo, faction, cargo.CargoClass, unit, quantity, transferredUnits);
			cargo.DestroyIfEmpty();
			TractorTarget = null;
		}

		protected override ActiveTurret AddActiveTurretComponent(GameObject g)
		{
			ActiveTractorTurret activeTractorTurret = g.AddComponent<ActiveTractorTurret>();
			activeTractorTurret.TractorTurret = this;
			return activeTractorTurret;
		}

		protected override void InactiveFire(Unit target)
		{
			base.InactiveFire(target);
			if (target != null)
			{
				Cargo cargoComponent = target.CargoComponent;
				if (cargoComponent != null)
				{
					TractorInCargo(cargoComponent);
				}
			}
		}

		protected override void OnFiringCancelled()
		{
			base.OnFiringCancelled();
			StopPullingUnit();
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.TractorTurret = this;
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			StopPullingUnit();
			if (unit.TractorTurret == this)
			{
				unit.TractorTurret = null;
			}
		}
	}
}
