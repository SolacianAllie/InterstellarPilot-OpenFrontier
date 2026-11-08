using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.MissionSpecs
{
	public class DestroyGroupMissionSpecSettings : MonoBehaviour
	{
		public ModdedUnitSeederSettings ModdedUnitSettings;

		public float EnemyShipTypeMultiplier = 3f;

		public float MaxEnemyNetWorthMultiplier = 1.2f;

		public float MinEnemyNetWorthMultiplier = 0.15f;

		public float MinPlayerNetWorthBasis = 12000f;

		public float MinReward = 500f;

		public float RewardCombatRatingMultiplier = 1900f;
	}
}
