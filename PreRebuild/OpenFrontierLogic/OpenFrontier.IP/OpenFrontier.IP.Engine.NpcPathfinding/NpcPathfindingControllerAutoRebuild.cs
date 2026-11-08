using UnityEngine;

namespace OpenFrontier.IP.Engine.NpcPathfinding
{
	public class NpcPathfindingControllerAutoRebuild : MonoBehaviour
	{
		private float lastRebuild;

		public float RebuildFrequency = 0.25f;

		private void Update()
		{
			if (EngineASX.LoadedAndReady && Time.time > lastRebuild + RebuildFrequency)
			{
				lastRebuild = Time.time;
				EngineASX.Instance.NpcPathfindingController.Rebuild();
			}
		}
	}
}
