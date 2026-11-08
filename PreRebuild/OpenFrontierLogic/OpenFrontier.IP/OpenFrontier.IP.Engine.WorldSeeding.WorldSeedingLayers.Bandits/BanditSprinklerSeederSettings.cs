using OpenFrontier.IP.Scenarios;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Bandits
{
	public class BanditSprinklerSeederSettings : MonoBehaviour
	{
		public float MinSectorDistanceMultiplier = 0.7f;

		public float MaxSectorDistanceMultiplier = 0.9f;

		public FactionSpawnerSpawnType BanditsSpawnType;

		public float MinShipCombatRating = 0.5f;

		public float MaxShipCombatRating = 20f;

		public float ShipCombatRatingPower = 2f;

		public int MinShipCount = 3;

		public int MaxShipCount = 8;

		public float ShipCountPower = 1.2f;

		public float ProbabilityOfSpawnMultiplierMin = 0.1f;

		public float ProbabilityOfSpawnMultiplierMax = 0.9f;

		public float ProbabilityOfBounty = 0.75f;

		public float ProbabilityOfBountyKnownPosition = 0.5f;

		public bool ManualHostileWithAll;
	}
}
