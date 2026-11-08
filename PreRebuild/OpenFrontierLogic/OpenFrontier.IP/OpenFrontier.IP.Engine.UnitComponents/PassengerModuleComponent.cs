namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class PassengerModuleComponent : ComponentBase
	{
		public PassengerModuleClass PassengerModuleClass;

		public override ComponentClass ComponentClass => PassengerModuleClass;

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.PassengerModule = this;
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			unit.PassengerModule = null;
		}
	}
}
