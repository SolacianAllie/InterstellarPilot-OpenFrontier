using UnityEngine;

namespace OpenFrontier.IP.Engine.SecurityLevels
{
	public class SectorSecurityLevelCalculator : MonoBehaviour
	{
		private int currentSectorIndex;

		private float nextUpdateTime;

		private const float updateInterval = 1f;

		private void Update()
		{
			if (EngineASX.LoadedAndReady && Time.time > nextUpdateTime)
			{
				currentSectorIndex++;
				if (currentSectorIndex >= EngineASX.Instance.Sectors.Count)
				{
					currentSectorIndex = 0;
				}
				if (currentSectorIndex < EngineASX.Instance.Sectors.Count)
				{
					EngineASX.Instance.Sectors[currentSectorIndex].RefreshSecurityLevel();
				}
				nextUpdateTime = Time.time + 1f;
			}
		}
	}
}
