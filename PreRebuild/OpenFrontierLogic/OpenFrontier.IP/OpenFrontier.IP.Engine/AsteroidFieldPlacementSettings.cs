using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class AsteroidFieldPlacementSettings : MonoBehaviour
	{
		public int Count = 20;

		public float CountReferenceRadius = 1000f;

		public float MaxSize = 200f;

		public float MinSize = 20f;

		public float SizePower = 0.8f;

		public float MaxSizeReferenceAsteroidFieldRadius = 4000f;

		public float MinSpacing = 400f;

		public float MinYPosition = 75f;

		public float MaxPlacementRadiusMultiplier = 0.9f;
	}
}
