using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class PassengerModuleClass : ComponentClass
	{
		public int PassengerCapacity = 1;

		protected override ComponentBase createComponent(GameObject target)
		{
			PassengerModuleComponent passengerModuleComponent = target.AddComponent<PassengerModuleComponent>();
			passengerModuleComponent.PassengerModuleClass = this;
			return passengerModuleComponent;
		}
	}
}
