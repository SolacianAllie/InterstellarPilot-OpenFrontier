using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Splits the reactor's output between weapons and everything else. Two
	/// separate pools, which is why CapacitorClass is not enough - it has a
	/// single Capacity.
	/// </summary>
	public class PowerDistributorClass : PowerConsumerClass
	{
		[Tooltip("Energy the weapons pool can hold before it must be drawn down.")]
		public float WeaponsCapacity = 0f;

		[Tooltip("Energy per second flowing back into the weapons pool.")]
		public float WeaponsRechargeRate = 0f;

		[Tooltip("Energy the engines-and-systems pool can hold.")]
		public float SystemsCapacity = 0f;

		[Tooltip("Energy per second flowing back into the engines-and-systems pool.")]
		public float SystemsRechargeRate = 0f;

		protected override ComponentBase createComponent(GameObject target)
		{
			CoreInternalComponent coreInternalComponent = target.AddComponent<CoreInternalComponent>();
			coreInternalComponent.ModuleClass = this;
			return coreInternalComponent;
		}
	}
}
