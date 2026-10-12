using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class CloakComponentClass : TurretClass
	{
		[Tooltip("Seconds to fade in.")]
		public float CloakTime = 3f;

		[Tooltip("Seconds to fade out.")]
		public float DecloakTime = 2f;

		[Tooltip("Scales how easily this cloaked ship is detected.")]
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
