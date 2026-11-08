using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Tooling
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class NpcsAutoPilotShips : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Npcs taking control of ships...", this, 1);
			}
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.Components.PilotPerson == null && item.UnitClass.IsPilottable && item.Components.Crew.Count > 0)
					{
						item.Components.PilotPerson = item.Components.Crew[0];
					}
				}
			}
		}
	}
}
