using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class RandomShipDamageSeederSettings : MonoBehaviour
	{
		public float SpawnedShipProbabilityOfDamage = 0.125f;

		public float SpawnedShipDamageMinDamage = 0.05f;

		public float SpawnedShipDamageMaxDamage = 0.3f;

		public float SpawnedShipDamagePower = 8f;
	}
}
