using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Wormholes
{
	public class WormholeUpdater
	{
		private int currentSectorIndex = -1;

		public void Update()
		{
			if (EngineASX.Instance.Sectors.Count == 0)
			{
				return;
			}
			currentSectorIndex++;
			if (currentSectorIndex >= EngineASX.Instance.Sectors.Count)
			{
				currentSectorIndex = 0;
			}
			List<Unit> unitsByType = EngineASX.Instance.Sectors[currentSectorIndex].GetUnitsByType(UnitType.Wormhole);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				if (item.WormholeComponent.IsUnstable)
				{
					item.WormholeComponent.UpdateUnstableWormhole();
				}
			}
		}
	}
}
