using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ShieldClass : ComponentClass
	{
		private const float costPower = 1.8f;

		private const int baseCost = 1750;

		private const float baseCharge = 25f;

		public float[] Capacities = new float[6];

		public float RegenEnergyCost = 1.5f;

		public float RegenRateMultiplier = 10f;

		public float ShieldRestoreMultiplier = 1f;

		public float TotalCapacity = -1f;

		public override string GetFriendlyName()
		{
			return $"Shield {Capacities[0]:N0}";
		}

		public float CalculateTotalCapacity()
		{
			float num = 0f;
			for (int i = 0; i < 6; i++)
			{
				num += Capacities[i];
			}
			return num;
		}

		protected override ComponentBase createComponent(GameObject target)
		{
			ShieldComponent shieldComponent = target.AddComponent<ShieldComponent>();
			shieldComponent.ShieldClass = this;
			return shieldComponent;
		}

		protected override int CalculateCost()
		{
			return Mathf.CeilToInt((1750f + Mathf.Pow(Capacities[0] - 25f, 1.8f)) / 25f) * 25;
		}
	}
}
