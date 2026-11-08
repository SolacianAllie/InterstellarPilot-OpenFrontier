using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class DeployableUnitCargoClass : MonoBehaviour
	{
		public Unit GetUnitPrefab()
		{
			CargoClass component = GetComponent<CargoClass>();
			if (component != null)
			{
				return component.RelatedPrefab.GetComponent<Unit>();
			}
			return null;
		}

		public void DeployUnit(Sector sector, Vector3 sectorPosition, Unit deployingUnit)
		{
			CargoClass component = GetComponent<CargoClass>();
			Unit unitPrefab = GetUnitPrefab();
			bool isUnderConstruction = unitPrefab.UnitType == UnitType.Station;
			Unit unit = WorldHelper.SpawnUnitAndInstallComponentsAtSafePosition(unitPrefab, sector, sectorPosition, 10f, addCargoLoadout: true, isUnderConstruction);
			if (deployingUnit != null)
			{
				unit.Faction = deployingUnit.Faction;
				if (deployingUnit.CargoBayComponent != null)
				{
					deployingUnit.CargoBayComponent.AddToCargoIfFits(component, -1);
				}
			}
		}

		public bool CanDeploy(Sector deploymentSector, Vector3 deploymentPosition, out Unit blockingUnit)
		{
			Unit unitPrefab = GetUnitPrefab();
			float radius = unitPrefab.UnitClass.DisplayData.Radius;
			blockingUnit = null;
			if (unitPrefab != null && unitPrefab.UnitClass.UnitType == UnitType.Station)
			{
				blockingUnit = null;
				if (unitPrefab.UnitClass.StationPurpose != StationPurpose.Defence)
				{
					List<Unit> unitsByType = deploymentSector.GetUnitsByType(UnitType.Station);
					if (unitsByType != null)
					{
						foreach (Unit item in unitsByType)
						{
							float num = radius;
							if (unitPrefab.UnitClass.StationPurpose != StationPurpose.Defence && item.UnitClass.StationPurpose != StationPurpose.Defence)
							{
								num += GameController.Instance.GameSettings.CargoDeployMinDistanceFromDockableStations;
							}
							if (IsUnitBlockingDeployment(deploymentSector, deploymentPosition, item, num))
							{
								blockingUnit = item;
								return false;
							}
						}
					}
				}
				float requiredDistance = radius + GameController.Instance.GameSettings.CargoDeployMinDistanceFromWormhole;
				List<Unit> unitsByType2 = deploymentSector.GetUnitsByType(UnitType.Wormhole);
				if (unitsByType2 != null)
				{
					foreach (Unit item2 in unitsByType2)
					{
						if (IsUnitBlockingDeployment(deploymentSector, deploymentPosition, item2, requiredDistance))
						{
							blockingUnit = item2;
							return false;
						}
					}
				}
			}
			return true;
		}

		private bool IsUnitBlockingDeployment(Sector deploymentSector, Vector3 deploymentPosition, Unit unit, float requiredDistance)
		{
			if (Vector3.Distance(unit.transform.position, deploymentPosition) - unit.UnitClass.DisplayData.Radius < requiredDistance)
			{
				return true;
			}
			return false;
		}
	}
}
