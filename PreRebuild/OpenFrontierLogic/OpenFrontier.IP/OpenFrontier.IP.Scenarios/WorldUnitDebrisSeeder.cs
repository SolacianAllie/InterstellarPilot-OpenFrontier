using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios
{
	public class WorldUnitDebrisSeeder : MonoBehaviour
	{
		public float ChanceOfNewGameShipDebris = 0.1f;

		private WorldBase world;

		public UnitClass[] NewGameShipDebrisClasses;

		private void Awake()
		{
			world = this.FindInParents<WorldBase>();
			if (world == null)
			{
				Debug.LogError("WorldUnitDebrisSeeder expects parent world object", this);
			}
			else
			{
				world.NewGame += world_NewGame;
			}
		}

		private void world_NewGame(WorldBase sender)
		{
			sender.NewGame -= world_NewGame;
			foreach (Sector sector in world.Engine.Sectors)
			{
				if (Mathf.Pow(Random.value, 1f - sector.SecurityLevel) < ChanceOfNewGameShipDebris)
				{
					CreateShipDebris(sector);
				}
			}
		}

		private void CreateShipDebris(Sector sector)
		{
			Vector3 randomSafeDeploymentSectorPosition = sector.GetRandomSafeDeploymentSectorPosition(1.2f, 1.5f, 20f, GameController.Instance.NonOVerlappingUnitsMask);
			CreateShipDebris(sector, randomSafeDeploymentSectorPosition);
		}

		private void CreateShipDebris(Sector sector, Vector3 sectorPosition)
		{
			UnitClass random = NewGameShipDebrisClasses.GetRandom();
			if (random != null)
			{
				CargoClass random2 = world.Engine.CargoClasses.Where((CargoClass e) => !e.IsReserved && e.IsTraded).GetRandom();
				if (random2 != null)
				{
					UnitDebris unitDebris = random.CreateDebris(world.Engine, sector, sectorPosition);
					unitDebris.Expires = false;
					unitDebris.Unit.CreateCargoItem(random2, Random.Range(1, 10), sectorPosition + Maths.RandomXZDirection() * 30f).Expires = false;
				}
			}
		}
	}
}
