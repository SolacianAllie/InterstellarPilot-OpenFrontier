using UnityEngine;
using UnityEngine.Serialization;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class ModdedUnitSeederSettings : MonoBehaviour
	{
		public bool ModStations = true;

		public bool ModShips = true;

		[FormerlySerializedAs("ProbabilityOfModdedUnit")]
		public float ProbabilityOfModdedShip = 0.15f;

		public float ProbabilityOfModdedStation = 0.15f;

		public float ProbabilityOfModdedTurret = 0.75f;

		public float ProbabilityOfModdedMisc = 0.4f;

		public float MinCargoLoadoutMultiplier = 0.6f;

		public float MaxCargoLoadoutMultiplier = 2.5f;

		public bool ModEquipmentBay;

		public bool AllowDowngrade;
	}
}
