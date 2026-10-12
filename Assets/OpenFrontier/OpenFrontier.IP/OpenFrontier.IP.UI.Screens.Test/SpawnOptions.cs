using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Testing.Spawning;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class SpawnOptions : MonoBehaviour
	{
		public Button SpawnEveryShipButton;

		public Button SpawnBanditHorde;

		public Button SpawnEveryStationButton;

		public Button SpawnPlayerFleet8SmallShipsButton;

		public Button SpawnPlayerFleet8LargeShipsButton;

		public Button SpawnPlayerMiningFleetButton;

		public Button SpawnPlayerPropertyLowButton;

		public Button SpawnPlayerPropertyMediumButton;

		public Button SpawnPlayerPropertyHighButton;

		public Button SpawnBanditHordeMultiSectorButton;

		public Button SpawnAsteroidTypeA;

		public Button SpawnAsteroidTypeH;

		private void Awake()
		{
			SpawnEveryShipButton.onClick.AddListener(SpawnEveryShipButtonClick);
			SpawnBanditHorde.onClick.AddListener(SpawnBanditHordeClick);
			SpawnBanditHordeMultiSectorButton.onClick.AddListener(SpawnBanditHordeMultiSectorButtonClick);
			SpawnEveryStationButton.onClick.AddListener(SpawnEveryStationButtonClick);
			SpawnPlayerFleet8SmallShipsButton.onClick.AddListener(SpawnPlayerFleet8SmallShipsButtonClick);
			SpawnPlayerFleet8LargeShipsButton.onClick.AddListener(SpawnPlayerFleet8LargeShipsButtonClick);
			SpawnPlayerMiningFleetButton.onClick.AddListener(SpawnPlayerMiningFleetButtonClick);
			SpawnPlayerPropertyLowButton.onClick.AddListener(SpawnPlayerPropertyLowButtonClick);
			SpawnPlayerPropertyMediumButton.onClick.AddListener(SpawnPlayerPropertyMediumButtonClick);
			SpawnPlayerPropertyHighButton.onClick.AddListener(SpawnPlayerPropertyHighButtonClick);
			SpawnAsteroidTypeA.onClick.AddListener(SpawnAsteroidTypeAClick);
			SpawnAsteroidTypeH.onClick.AddListener(SpawnAsteroidTypeHClick);
		}

		private void SpawnAsteroidTypeAClick()
		{
			SpawnAsteroid(GameController.Instance.UnitClasses.AsteroidTypeA.UnitPrefab);
		}

		private void SpawnAsteroidTypeHClick()
		{
			SpawnAsteroid(GameController.Instance.UnitClasses.AsteroidTypeH.UnitPrefab);
		}

		private void SpawnAsteroid(Unit prefab)
		{
			Vector3 spawnSectorPositionFromLocalUnit = SpawnUtils.GetSpawnSectorPositionFromLocalUnit(EngineASX.Instance.LocalUnit, 100f);
			Vector3? vector = PhysicsNonOverlappingPositionFinder.FindSectorPositionOrNull(EngineASX.Instance.ActiveSector, spawnSectorPositionFromLocalUnit, GameController.Instance.GameSettings.DefaultWorldSeedSettings.CreateAsteroidsSeederSettings.MinDistanceBetweenAsteroids, GameController.Instance.StaticNonOverlappingMask);
			if (!vector.HasValue)
			{
				UIController.Instance.ShowMessageBox("Failed to find position to spawn asteroid", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
			else if (SpawnUtils.TrySpawnUnit(prefab, EngineASX.Instance.ActiveSector, vector.Value, null) != null)
			{
				UIController.Instance.ShowMessageBox("Asteroid has been spawned in current sector");
			}
			else
			{
				UIController.Instance.ShowMessageBox("Failed to spawn asteroid", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
			}
		}

		private void SpawnPlayerPropertyLowButtonClick()
		{
			List<Unit> units = SpawnUtils.SpawnPropertyEverywhere(EngineASX.Instance.LocalFaction, EngineASX.Instance.LocalPlayerSector, 5, 0.08f, 0.16f, 4, 20, 8f, 5f);
			ReportOnSpawnPlayerUnits(units);
		}

		private void SpawnPlayerPropertyMediumButtonClick()
		{
			List<Unit> units = SpawnUtils.SpawnPropertyEverywhere(EngineASX.Instance.LocalFaction, EngineASX.Instance.LocalPlayerSector, 5, 0.15f, 0.2f, 4, 20, 6f, 3f);
			ReportOnSpawnPlayerUnits(units);
		}

		private void ReportOnSpawnPlayerUnits(List<Unit> units)
		{
			if (units.Count == 0)
			{
				UIController.Instance.ShowMessageBox("No units were spawned. It's possible there are too many units already", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			UIController.Instance.ShowMessageBox($"Spawned {units.Count((Unit e) => e.UnitType == UnitType.Station)} stations and {units.Count((Unit e) => e.UnitType == UnitType.Ship)} ships", MessageBoxButtons.Ok);
		}

		private void SpawnPlayerPropertyHighButtonClick()
		{
			List<Unit> units = SpawnUtils.SpawnPropertyEverywhere(EngineASX.Instance.LocalFaction, EngineASX.Instance.LocalPlayerSector);
			ReportOnSpawnPlayerUnits(units);
		}

		private void SpawnEveryShipButtonClick()
		{
			int num = SpawnUtils.SpawnEveryShip();
			UIController.Instance.ShowMessageBox($"{num} ships were spawned in current sector");
		}

		private void SpawnBanditHordeClick()
		{
			int num = SpawnUtils.SpawnBanditHordesAtRandomSectorPositions(EngineASX.Instance.LocalPlayerSector);
			UIController.Instance.ShowMessageBox($"{num} ships were spawned in current sector");
		}

		private void SpawnBanditHordeMultiSectorButtonClick()
		{
			int num = 0;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if (Random.value < 0.3f || num == 0)
				{
					num += SpawnUtils.SpawnBanditHordesAtRandomSectorPositions(sector);
				}
			}
			UIController.Instance.ShowMessageBox($"{num} ships were spawned in different sectors");
		}

		private void SpawnEveryStationButtonClick()
		{
			int num = SpawnUtils.SpawnEveryStation();
			UIController.Instance.ShowMessageBox($"{num} stations were spawned in current sector");
		}

		private void SpawnPlayerFleet8SmallShipsButtonClick()
		{
			SpawnUtils.SpawnFleetWithSmallUnits(EngineASX.Instance.LocalFaction, EngineASX.Instance.ActiveSector, EngineASX.Instance.LocalUnit.SectorPosition + EngineASX.Instance.LocalUnit.transform.forward * 200f, 8);
			UIController.Instance.ShowMessageBox($"{8} ships were spawned in current sector");
		}

		private void SpawnPlayerFleet8LargeShipsButtonClick()
		{
			SpawnUtils.SpawnFleetWithLargerUnits(EngineASX.Instance.LocalFaction, EngineASX.Instance.ActiveSector, EngineASX.Instance.LocalUnit.SectorPosition + EngineASX.Instance.LocalUnit.transform.forward * 200f, 8);
			UIController.Instance.ShowMessageBox($"{8} ships were spawned in current sector");
		}

		private void SpawnPlayerMiningFleetButtonClick()
		{
			SpawnUtils.SpawnFleetWithLargerMiningUnits(EngineASX.Instance.LocalFaction, EngineASX.Instance.ActiveSector, EngineASX.Instance.LocalUnit.SectorPosition + EngineASX.Instance.LocalUnit.transform.forward * 200f, 8);
			UIController.Instance.ShowMessageBox($"{8} ships were spawned in current sector");
		}
	}
}
