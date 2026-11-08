using UnityEngine;

namespace OpenFrontier.IP
{
	public class RotateObjectRandomly : RotateObject
	{
		public Vector3 AxisMultipliers = Vector3.one;

		public float MaxRotationRate = 10f;

		public float MinRotationRate;

		private void Start()
		{
			RotationRate = new Vector3(Random.Range(MinRotationRate, MaxRotationRate) * AxisMultipliers.x, Random.Range(MinRotationRate, MaxRotationRate) * AxisMultipliers.y, Random.Range(MinRotationRate, MaxRotationRate) * AxisMultipliers.z);
		}
	}
}
