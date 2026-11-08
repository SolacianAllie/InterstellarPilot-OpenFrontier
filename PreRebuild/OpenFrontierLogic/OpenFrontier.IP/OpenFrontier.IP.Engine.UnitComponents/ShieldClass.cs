using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ShieldClass : ComponentClass
	{
		private const float costPower = 1.8f;

		private const int baseCost = 1750;

		private const float baseCharge = 25f;

		[Tooltip("Shield strength at each rating tier, weakest first.")]
		public float[] Capacities = new float[6];

		[Tooltip("Energy that one point of regenerated shield costs.")]
		public float RegenEnergyCost = 1.5f;

		[Tooltip("Scales how quickly this shield comes back after damage.")]
		public float RegenRateMultiplier = 10f;

		[Tooltip("Scales how much incoming damage the shield puts back as regeneration.")]
		public float ShieldRestoreMultiplier = 1f;

		[Tooltip("Total shield strength. Worked out from Capacities.")]
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
