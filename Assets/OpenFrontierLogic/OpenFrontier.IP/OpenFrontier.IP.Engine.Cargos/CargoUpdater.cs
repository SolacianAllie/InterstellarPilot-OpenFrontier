using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Cargos
{
	public class CargoUpdater
	{
		private int cargoIndex = -1;

		private int sectorIndex = -1;

		public void Update()
		{
			if (EngineASX.Instance.Sectors.Count == 0)
			{
				return;
			}
			if (sectorIndex > -1)
			{
				if (sectorIndex < EngineASX.Instance.Sectors.Count)
				{
					UpdateCargoInCurrentSector();
					return;
				}
				sectorIndex = 0;
				cargoIndex = -1;
			}
			else
			{
				sectorIndex++;
				cargoIndex = -1;
			}
		}

		private void UpdateCargoInCurrentSector()
		{
			cargoIndex++;
			List<Unit> unitsByType = EngineASX.Instance.Sectors[sectorIndex].GetUnitsByType(UnitType.Cargo);
			if (unitsByType != null && cargoIndex < unitsByType.Count)
			{
				Unit unit = unitsByType[cargoIndex];
				if (unit != null)
				{
					unit.CargoComponent.UpdateExpiry();
				}
			}
			else
			{
				sectorIndex++;
				cargoIndex = -1;
			}
		}
	}
}
