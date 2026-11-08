using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.ScenarioOptions
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class ScenarioOptionsSeeder : MonoBehaviour
	{
		public ScenarioOptions ScenarioOptions;

		public void SeedLayer(WorldBase world)
		{
			Debug.LogError("This component is obsolete");
		}
	}
}
