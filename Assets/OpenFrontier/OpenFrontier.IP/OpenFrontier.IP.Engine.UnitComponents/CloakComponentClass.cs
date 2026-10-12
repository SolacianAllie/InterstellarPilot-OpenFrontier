using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class CloakComponentClass : TurretClass
	{
		public float CloakTime = 3f;

		public float DecloakTime = 2f;

		public float DetectionMultiplier = 0.2f;

		public override bool IsWeapon => false;

		protected override ComponentBase createComponent(GameObject target)
		{
			CloakComponent cloakComponent = target.AddComponent<CloakComponent>();
			cloakComponent.CloakComponentClass = this;
			cloakComponent.TurretClass = this;
			return cloakComponent;
		}
	}
}
