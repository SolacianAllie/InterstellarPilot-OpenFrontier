using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// A hull armour package. The Elite table gives these five alloys rather
	/// than a class and rating ladder, so this class has no ladder of its own.
	/// </summary>
	public class BulkheadClass : ComponentClass
	{
		[Tooltip("Extra hull points this package adds, as a percentage over the bare hull. +80% is the baseline alloy.")]
		public float HullPointBoost = 0f;

		[Tooltip("Resistance to kinetic damage. Negative means the hull takes more than a bare hull would.")]
		public float KineticResistance = 0f;

		[Tooltip("Resistance to thermal damage. Negative means the hull takes more than a bare hull would.")]
		public float ThermalResistance = 0f;

		[Tooltip("Resistance to explosive damage. Negative means the hull takes more than a bare hull would.")]
		public float ExplosiveResistance = 0f;

		protected override ComponentBase createComponent(GameObject target)
		{
			CoreInternalComponent coreInternalComponent = target.AddComponent<CoreInternalComponent>();
			coreInternalComponent.ModuleClass = this;
			return coreInternalComponent;
		}
	}
}
