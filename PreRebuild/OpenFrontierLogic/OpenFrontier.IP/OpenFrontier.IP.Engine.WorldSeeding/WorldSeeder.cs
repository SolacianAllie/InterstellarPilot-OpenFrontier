using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding
{
	public class WorldSeeder : MonoBehaviour
	{
		[SerializeField]
		private WorldSeedSettings settings;

		public WorldSeedSettings Settings
		{
			get
			{
				return settings;
			}
			set
			{
				settings = value;
			}
		}

		public void SeedWorld(WorldBase world)
		{
			SeedSpecialLayers(world);
			world.CacheAllFactionValidTraders();
			SetupFactions(world);
			PopulateFactionValidTraderTargets();
			SeperateOverlappingUnitsInAllSectors(world);
		}

		private static void SetupFactions(WorldBase world)
		{
			foreach (Faction item in world.Engine.Factions.Where((Faction e) => e.FactionAI != null))
			{
				item.FactionAI.CreatePilotsAndHandleSurplusShips(createPilots: true);
				item.FactionAI.CreateFleets();
				item.CalculateNetWorth();
				item.UpdateHighestEverNetWorth();
				item.RecordStartingStats();
			}
		}

		public static void PopulateFactionValidTraderTargets()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				faction.RecacheValidTraderTargets();
			}
		}

		private void SeedSpecialLayers(WorldBase world)
		{
			WorldSeederLayer[] componentsInChildren = GetComponentsInChildren<WorldSeederLayer>();
			foreach (WorldSeederLayer worldSeederLayer in componentsInChildren)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log("Seeding layer " + worldSeederLayer.gameObject.name, worldSeederLayer, 1);
				}
				worldSeederLayer.Seed(world);
			}
		}

		private static void SeperateOverlappingUnitsInAllSectors(WorldBase world)
		{
			foreach (Sector sector in world.Engine.Sectors)
			{
				sector.SeparateOverlappingShips();
			}
		}

		private static void AutoNameAllEngineUnits(WorldBase world)
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				unit.AutoNameGameObject();
			});
		}

		private void FactionSetSpawnTime(WorldBase world)
		{
			foreach (Faction faction in world.Engine.Factions)
			{
				faction.SetSpawnTimeToCurrent();
			}
		}
	}
}
