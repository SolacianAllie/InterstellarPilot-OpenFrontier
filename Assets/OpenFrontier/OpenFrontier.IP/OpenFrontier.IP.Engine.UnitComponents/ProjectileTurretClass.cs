using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ProjectileTurretClass : TurretClass
	{
		public List<ProjectileClass> CompatibleProjectiles;

		public TurretType ProjectileTurretType;

		public bool UseSubTargetPositions;

		protected override ComponentBase createComponent(GameObject target)
		{
			ProjectileTurretComponent projectileTurretComponent = target.AddComponent<ProjectileTurretComponent>();
			projectileTurretComponent.AutoChargeEnabled = true;
			projectileTurretComponent.ProjectileTurretClass = this;
			projectileTurretComponent.TurretClass = this;
			return projectileTurretComponent;
		}
	}
}
