using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoLeakage
{
	public class CargoLeakageSettings : MonoBehaviour
	{
		public float ProbabilityFromDamageFactor = 0.1f;

		public float DamageProbabilityReferenceValue = 100f;

		public float ProbabilityFromCargoBayUsageFactor = 0.1f;

		public int MinCargoTypesLeaked = 1;

		public int MaxCargoTypesLeaked = 3;

		public float CargoVolumePower = 2f;

		public float NumCargoTypesLeakedPower = 4f;

		public float MaxPercentageLeaked = 0.4f;
	}
}
