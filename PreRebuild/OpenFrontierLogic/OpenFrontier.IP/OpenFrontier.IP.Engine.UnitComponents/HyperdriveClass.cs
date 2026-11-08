using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class HyperdriveClass : TurretClass
	{
		[Tooltip("Seconds the drive spends charging before it can be used.")]
		public float ChargeTime = 8f;

		protected override ComponentBase createComponent(GameObject target)
		{
			HyperdriveComponent hyperdriveComponent = target.AddComponent<HyperdriveComponent>();
			hyperdriveComponent.HyperdriveClass = this;
			hyperdriveComponent.TurretClass = this;
			return hyperdriveComponent;
		}
	}
}
