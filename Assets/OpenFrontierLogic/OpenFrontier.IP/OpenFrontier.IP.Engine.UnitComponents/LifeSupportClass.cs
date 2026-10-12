using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Keeps the crew breathing. Breach it and the air goes, which is the hook
	/// a future canopy system would sit on.
	/// </summary>
	public class LifeSupportClass : PowerConsumerClass
	{
		[Tooltip("Seconds of air at full, with nobody aboard consuming it. The Elite table gives this as a clock.")]
		public float OxygenTimeCapacity = 300f;

		protected override ComponentBase createComponent(GameObject target)
		{
			CoreInternalComponent coreInternalComponent = target.AddComponent<CoreInternalComponent>();
			coreInternalComponent.ModuleClass = this;
			return coreInternalComponent;
		}
	}
}
