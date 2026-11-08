using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class CapacitorComponent : ComponentBase
	{
		public CapacitorClass CapacitorClass;

		[SerializeField]
		private float energyUnitsStored;

		public float Charge
		{
			get
			{
				return energyUnitsStored;
			}
			set
			{
				float capacity = CapacitorClass.Capacity;
				energyUnitsStored = Mathf.Clamp(value, 0f, capacity);
				if (energyUnitsStored < capacity)
				{
					UnitComponents.anyComponentRequiresRecharge = true;
				}
			}
		}

		public override ComponentClass ComponentClass => CapacitorClass;

		public float ChargeNormalized
		{
			get
			{
				return energyUnitsStored / CapacitorClass.Capacity;
			}
			set
			{
				energyUnitsStored = value * CapacitorClass.Capacity;
			}
		}

		public bool IsFullyCharged => energyUnitsStored >= CapacitorClass.Capacity;

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.Capacitor = this;
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			unit.Capacitor = null;
		}

		protected override void rechargeFull()
		{
			base.rechargeFull();
			Charge = CapacitorClass.Capacity;
		}

		protected override void removeCharge()
		{
			Charge = 0f;
		}
	}
}
