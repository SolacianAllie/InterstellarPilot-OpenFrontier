using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class CreateShipsSeederSettings : MonoBehaviour
	{
		public float BanditsRpUsedOnNewGamePower = 2f;

		public float FactionsMinRpUsedOnNewGame = 0.2f;

		public float FactionsMaxRpUsedOnNewGame = 0.6f;

		public float FactionsRpUsedOnNewGamePower = 2f;

		public bool SeedOnlyAvailableShips;
	}
}
