using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Sets how far this ship can be detected from.
	/// </summary>
	public class SensorPackageClass : PowerConsumerClass
	{
		[Tooltip("How far this ship can be picked up, in kilometres. Cloaking shortens it; a sensor package sets the baseline.")]
		public float EmissionRange = 0f;

		protected override ComponentBase createComponent(GameObject target)
		{
			CoreInternalComponent coreInternalComponent = target.AddComponent<CoreInternalComponent>();
			coreInternalComponent.ModuleClass = this;
			return coreInternalComponent;
		}
	}
}
