using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class CapacitorClass : ComponentClass
	{
		private const float costPower = 1.32f;

		private const float costMinCharge = 300f;

		private const int costMin = 100;

		public float Capacity = 100f;

		public override string GetFriendlyName()
		{
			return $"Capacitor {TextFormattingHelper.FormatNumberThousands(Capacity)}";
		}

		protected override ComponentBase createComponent(GameObject target)
		{
			CapacitorComponent capacitorComponent = target.AddComponent<CapacitorComponent>();
			capacitorComponent.CapacitorClass = this;
			return capacitorComponent;
		}

		protected override int CalculateCost()
		{
			return Mathf.CeilToInt((100f + Mathf.Pow(Capacity - 300f, 1.32f)) / 25f) * 25;
		}
	}
}
