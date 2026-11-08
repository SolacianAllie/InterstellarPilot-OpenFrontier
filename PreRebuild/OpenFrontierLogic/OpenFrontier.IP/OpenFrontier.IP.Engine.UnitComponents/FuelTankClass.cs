using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Holds jump fuel. No power draw in the Elite table, so this does not
	/// derive from PowerConsumerClass.
	/// </summary>
	public class FuelTankClass : ComponentClass
	{
		[Tooltip("Tonnes of jump fuel this tank holds.")]
		public float FuelCapacity = 0f;

		protected override ComponentBase createComponent(GameObject target)
		{
			CoreInternalComponent coreInternalComponent = target.AddComponent<CoreInternalComponent>();
			coreInternalComponent.ModuleClass = this;
			return coreInternalComponent;
		}
	}
}
