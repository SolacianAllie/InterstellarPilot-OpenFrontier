using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class RefreshSectorContentTypeSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Refreshing sector content...", this, 1);
			}
			EngineASX.Instance.RefreshSectorContentType();
		}
	}
}
