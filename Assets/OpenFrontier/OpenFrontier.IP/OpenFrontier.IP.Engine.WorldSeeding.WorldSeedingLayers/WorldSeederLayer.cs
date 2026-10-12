using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	public class WorldSeederLayer : MonoBehaviour
	{
		public void Seed(WorldBase world)
		{
			SendMessage("SeedLayer", world, SendMessageOptions.RequireReceiver);
		}
	}
}
