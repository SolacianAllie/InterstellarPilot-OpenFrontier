namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class PwrGeneratorComponent : ComponentBase
	{
		public PwrGeneratorClass PwrGeneratorClass;

		public override ComponentClass ComponentClass => PwrGeneratorClass;

		public override bool RequiresRecharge()
		{
			CapacitorComponent capacitor = UnitComponents.Capacitor;
			if (capacitor != null)
			{
				return capacitor.Charge < capacitor.CapacitorClass.Capacity;
			}
			return false;
		}

		protected override void rechargeTick(float elapsedTime)
		{
			CapacitorComponent capacitor = UnitComponents.Capacitor;
			float num = PwrGeneratorClass.PowerGenRate * GameController.Instance.GameSettings.GameplaySettings.PowerGeneratorRateMultiplier * elapsedTime;
			if (IsDamaged)
			{
				num -= NormalizedDamage * Engine.GameSettings.ComponentDamageEffectSettings.DamagedPowerGeneratorMaxAffect * num;
			}
			if (capacitor.IsDamaged)
			{
				num -= capacitor.NormalizedDamage * Engine.GameSettings.ComponentDamageEffectSettings.DamagedCapacitorChargeMaxAffect * num;
			}
			capacitor.Charge += num;
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.PowerGenerator = this;
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			unit.PowerGenerator = null;
		}
	}
}
