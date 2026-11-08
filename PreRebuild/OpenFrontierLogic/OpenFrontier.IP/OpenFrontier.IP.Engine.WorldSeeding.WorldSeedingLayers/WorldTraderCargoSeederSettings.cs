using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class WorldTraderCargoSeederSettings : MonoBehaviour
	{
		public float ProbabilityOfSurplusCargo = 0.12f;

		public float SurplusCargoMultiplier = 3f;

		public float InitialTraderCargoMaxStockMultiplier = 1f;

		public float InitialTraderCargoMinStockMultiplier = 0.1f;

		public float InitialTraderCargoPowerMin = 1f;

		public float InitialTraderCargoPowerMax = 1f;

		public float InitialTraderCargoPower01 = 1f;

		public float CargoRarityMultiplier = 1f;

		[FormerlySerializedAs("DefaultCargoPower")]
		public float DefaultCargoPowerMultiplier = 1.25f;

		[FormerlySerializedAs("ConsumerCargoPower")]
		public float ConsumerCargoPowerMultiplier = 4f;

		[FormerlySerializedAs("ProducerCargoPower")]
		public float ProducerCargoPowerMultiplier = 0.8f;

		public float GeneralCargoPowerMultiplier = 2f;
	}
}
