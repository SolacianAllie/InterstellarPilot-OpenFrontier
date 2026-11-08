using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ProjectileTurretClass : TurretClass
	{
		[Tooltip("Ammunition this weapon can fire. Empty means it can use any.")]
		public List<ProjectileClass> CompatibleProjectiles;

		[Tooltip("Marks this as a projectile weapon for the UI and the AI's weapon handling.")]
		public TurretType ProjectileTurretType;

		[Tooltip("Aims each shot at where the target will be rather than where it is.")]
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
