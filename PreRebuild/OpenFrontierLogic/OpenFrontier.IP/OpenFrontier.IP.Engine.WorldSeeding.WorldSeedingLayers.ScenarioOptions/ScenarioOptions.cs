using OpenFrontier.IP.Common;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.ScenarioOptions
{
	public class ScenarioOptions : MonoBehaviour
	{
		public bool FactionSpawningEnabled = true;

		public bool AsteroidRespawningEnabled = true;

		public float AsteroidRespawnTime = 0.5f;

		public RespawnOnDeathPreference RespawnOnDeath;

		public bool Permadeath;

		public bool AllowTeleporting = true;

		public bool PlayerPropertyAttackNotifications = true;

		public bool AllowStationCapture = true;

		public bool AllowAbandonShip = true;

		public void ApplyOptions(WorldBase world)
		{
			EngineASX.Instance.SetFactionSpawningEnabled(FactionSpawningEnabled);
		}
	}
}
