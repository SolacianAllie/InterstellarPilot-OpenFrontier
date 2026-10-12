using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class PwrGeneratorClass : ComponentClass
	{
		private const float costPower = 2.82f;

		private const float costMinCharge = -10f;

		private const int costMin = 500;

		public float PowerGenRate = 100f;

		public override string GetFriendlyName()
		{
			return $"Generator {PowerGenRate:N2}";
		}

		protected override ComponentBase createComponent(GameObject target)
		{
			PwrGeneratorComponent pwrGeneratorComponent = target.AddComponent<PwrGeneratorComponent>();
			pwrGeneratorComponent.PwrGeneratorClass = this;
			return pwrGeneratorComponent;
		}

		protected override int CalculateCost()
		{
			float num = PowerGenRate / 2f;
			int num2 = Mathf.CeilToInt((500f + Mathf.Pow(num - -10f, 2.82f)) / 25f) * 25;
			if (num2 > 100000000)
			{
				return 100000000;
			}
			return num2;
		}
	}
}
