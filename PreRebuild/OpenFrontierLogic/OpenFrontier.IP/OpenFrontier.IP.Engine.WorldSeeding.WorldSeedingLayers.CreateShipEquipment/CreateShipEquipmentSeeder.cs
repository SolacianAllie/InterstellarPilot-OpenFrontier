using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.CreateShipEquipment
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateShipEquipmentSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding ship equipment...", this, 1);
			}
			ModdedUnitSeederSettings moddedUnitSettings = GameController.Instance.GameSettings.ModdedUnitSettings;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Ship);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					ModdedUnitSeeder.AddUnitEquipment(item, moddedUnitSettings);
				}
			}
		}
	}
}
