using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.UnstableWormholes
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class SetAllSecurityLevelsSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Set all security levels...", this, 1);
			}
			world.CalculateSectorSecurityLevels();
		}
	}
}
