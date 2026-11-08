using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Mining
{
	public class MineOrderSeederSettings : MonoBehaviour
	{
		public float ProbabilityOfSellingCargo = 0.25f;

		public float MinFreeCargoSpaceUsage = 0.5f;

		public float MaxFreeCargoSpaceUsage = 1f;
	}
}
