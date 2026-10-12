using System.Linq;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.ActiveObjectives;
using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Mining
{
	public class MineOrderSeeder : MonoBehaviour
	{
		public MineOrderSeederSettings MineOrderSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Randomizing miners...", this, 1);
			}
			MineOrderSeederSettings = world.Seeder.Settings.MineOrderSeederSettings;
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (!faction.IsAIFactionType)
				{
					continue;
				}
				foreach (Fleet fleet in faction.Fleets)
				{
					if (!fleet.ExcludeFromFactionAI)
					{
						TrySpawnFleetWithRandomCargoOnNewGame(fleet, EngineASX.Instance.EconomySettings.TraderInitWithCargoProbability);
					}
				}
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Completed spawning fleet cargo", this, 1);
			}
		}

		public void TrySpawnFleetWithRandomCargoOnNewGame(Fleet fleet, float probabilityOfCargo)
		{
			if (!(fleet.ActiveOrder is ActiveMineOrder activeMineOrder) || CollectCargoHelper.CheckFullCargoBay(fleet, activeMineOrder.MineObjective.FullCargoThreshold))
			{
				return;
			}
			MineSearchOperation mineSearchOperation = new MineSearchOperation();
			Sector startSector = fleet.Sector;
			Vector3 startPosition = fleet.SectorPosition;
			if (fleet.IsHomeBaseValid)
			{
				startSector = fleet.HomeSector;
				startPosition = fleet.HomeSectorPosition;
			}
			mineSearchOperation.InitialiseAndStartSearch(startSector, startPosition, fleet.Faction, activeMineOrder.GetActualMaxJumpDist());
			mineSearchOperation.ProcessUntilCompletion();
			if (mineSearchOperation.Result != null)
			{
				if (Random.value < MineOrderSeederSettings.ProbabilityOfSellingCargo)
				{
					SpawnFleetWithMinedCargo(activeMineOrder, fleet, mineSearchOperation.Result);
				}
				else
				{
					SpawnFleetTargettingAsteroid(activeMineOrder, fleet, mineSearchOperation.Result);
				}
			}
		}

		private void SpawnFleetTargettingAsteroid(ActiveMineOrder activeMineOrder, Fleet fleet, Unit asteroid)
		{
			activeMineOrder.ChangeMineTarget(asteroid);
		}

		private void SpawnFleetWithMinedCargo(ActiveMineOrder activeMineOrder, Fleet fleet, Unit asteroid)
		{
			if (asteroid.Asteroid.AsteroidClass.AsteroidYieldItems.Count == 0)
			{
				return;
			}
			float num = asteroid.Asteroid.AsteroidClass.AsteroidYieldItems.Sum((AsteroidYieldItem e) => e.Weight);
			foreach (UnitComponentHolder ship in fleet.Ships)
			{
				if (ship.CargoBayComponent == null)
				{
					continue;
				}
				float num2 = Random.Range(MineOrderSeederSettings.MinFreeCargoSpaceUsage, MineOrderSeederSettings.MaxFreeCargoSpaceUsage);
				float num3 = ship.CargoBayComponent.FreeSpace * num2;
				if (num3 < 2f)
				{
					continue;
				}
				foreach (AsteroidYieldItem asteroidYieldItem in asteroid.Asteroid.AsteroidClass.AsteroidYieldItems)
				{
					int delta = (int)(asteroidYieldItem.Weight / num * num3 / asteroidYieldItem.CargoClass.Volume);
					ship.CargoBayComponent.AddToCargo(asteroidYieldItem.CargoClass, delta, ignoreCapacity: true);
				}
			}
			if (fleet.OrderQueue.Count <= 0 || !(fleet.OrderQueue[0] is SellCargoOrder))
			{
				return;
			}
			foreach (UnitComponentHolder ship2 in fleet.Ships)
			{
				ship2.Unit.UndockIfDocked();
				ship2.Unit.Sector = asteroid.Sector;
				ship2.Unit.transform.localPosition = asteroid.SectorPosition + Geometry.RandomXZUnitVector() * Random.Range(100, 300) + fleet.GetLocalFormationPositionForPilot(ship2.PilotNpc);
			}
			activeMineOrder.OnComplete(silent: true);
			fleet.AssignNextOrderIfNoCurrentOrder();
		}
	}
}
