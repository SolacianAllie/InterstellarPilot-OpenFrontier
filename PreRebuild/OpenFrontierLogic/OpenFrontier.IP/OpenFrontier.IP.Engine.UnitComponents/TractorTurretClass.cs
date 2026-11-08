using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class TractorTurretClass : LaserTurretClass
	{
		public override bool IsWeapon => false;

		public override bool CanFireAt(Unit target)
		{
			if ((target.Engine.GameSettings.TractorBeamSettings.TractorableUnitTypes & target.UnitType) != 0)
			{
				if (!(target.Components == null))
				{
					return target.NpcPilot == null;
				}
				return true;
			}
			return false;
		}

		public override float GetMinFireRequiredEnergy()
		{
			return EnergyCost;
		}

		protected override ComponentBase createComponent(GameObject target)
		{
			TractorTurretComponent tractorTurretComponent = target.AddComponent<TractorTurretComponent>();
			tractorTurretComponent.AutoChargeEnabled = true;
			tractorTurretComponent.LaserTurretClass = this;
			tractorTurretComponent.TractorTurretClass = this;
			tractorTurretComponent.TurretClass = this;
			return tractorTurretComponent;
		}
	}
}
