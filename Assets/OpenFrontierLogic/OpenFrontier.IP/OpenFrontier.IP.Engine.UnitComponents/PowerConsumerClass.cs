using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Base for the new-system internals that draw power from the ship's
	/// distributor. Three of the four Elite tables that list a draw are
	/// this: thrusters, life support, distributors and sensors.
	/// </summary>
	public class PowerConsumerClass : ComponentClass
	{
		[Tooltip("Energy drawn per second while this module is online.")]
		public float PowerDraw = 0f;

		protected override ComponentBase createComponent(GameObject target)
		{
			CoreInternalComponent coreInternalComponent = target.AddComponent<CoreInternalComponent>();
			coreInternalComponent.ModuleClass = this;
			return coreInternalComponent;
		}
	}
}
