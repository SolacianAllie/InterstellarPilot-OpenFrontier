using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.StationBuilding;
using OpenFrontier.IP.Scenarios;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateEmpireFactionsSeeder : MonoBehaviour
	{
		private class ExpansionOperation
		{
			public Faction Faction { get; set; }

			public int RemainingSectorCount { get; set; }

			public FactionEmpireExpansionSearch ExpansionSearch { get; set; }
		}

		public CreateEmpireFactionsSeederSettings CreateEmpireFactionsSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (world.Seeder.Settings.FactionSeederSettings.SeedNonBanditFactions)
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log("Seeding empire factions...", this, 1);
				}
				CreateEmpireFactionsSeederSettings = world.Seeder.Settings.CreateEmpireFactionsSeederSettings;
				float num = Maths.RandomFloatWithPower(CreateEmpireFactionsSeederSettings.MinNumFactionsPerSector, CreateEmpireFactionsSeederSettings.MaxNumFactionsPerSector, CreateEmpireFactionsSeederSettings.NumFactionsPerSectorPower);
				int minFactions = CreateEmpireFactionsSeederSettings.MinFactions;
				int num2 = Mathf.Max(0, Mathf.Min(CreateEmpireFactionsSeederSettings.MaxFactions, Mathf.RoundToInt(num * (float)EngineASX.Instance.Sectors.Count)));
				if (num2 > 0)
				{
					List<Faction> specialEmpireFactions = new List<Faction>(CreateEmpireFactionsSeederSettings.SpecialFactionPrefabs);
					int numEmpireFactionsToSpawn = Random.Range(minFactions, num2);
					IEnumerable<Faction> enumerable = CreateEmpireFactions(numEmpireFactionsToSpawn, specialEmpireFactions, world.Seeder.Settings.FactionIntelSeederSettings);
					int maxControlledSectors = Mathf.Max(1, Mathf.RoundToInt(Maths.RandomFloatWithPower(CreateEmpireFactionsSeederSettings.MaxTotalEmpireControlledSectorsLower, CreateEmpireFactionsSeederSettings.MaxTotalEmpireControlledSectorsUpper, CreateEmpireFactionsSeederSettings.MaxTotalEmpireControlledSectorsPower) * (float)EngineASX.Instance.Sectors.Count));
					ExpandEmpireFactions(enumerable.ToList(), maxControlledSectors);
					BuildStationsInControlledSectors(enumerable);
				}
			}
		}

		private void BuildStationsInControlledSectors(IEnumerable<Faction> empireFactions)
		{
			foreach (Faction empireFaction in empireFactions)
			{
				BuildStationsInControlledSectors(empireFaction);
			}
		}

		private void BuildStationsInControlledSectors(Faction faction)
		{
			foreach (Sector controlledSector in faction.ControlledSectors)
			{
				Vector3 randomSafeDeploymentSectorPosition = controlledSector.GetRandomSafeDeploymentSectorPosition(0f, 0.2f, GameController.Instance.GameSettings.MinDistanceBetweenStations * 1.5f, GameController.Instance.StaticNonOverlappingMask);
				FactionStationBuilder.BuildStation(GameController.Instance.UnitClasses.SectorHQ, controlledSector, randomSafeDeploymentSectorPosition, faction, cargoLoadout: true, underConstruction: false);
			}
		}

		private void ExpandEmpireFactions(IList<Faction> factions, int maxControlledSectors)
		{
			List<ExpansionOperation> list = new List<ExpansionOperation>();
			foreach (Faction faction in factions)
			{
				int num = Mathf.RoundToInt(Mathf.Lerp(CreateEmpireFactionsSeederSettings.MinExpansion, CreateEmpireFactionsSeederSettings.MaxExpansion, Mathf.Pow(Random.value, CreateEmpireFactionsSeederSettings.ExpansionPower)));
				if (num > 0)
				{
					ExpansionOperation expansionOperation = new ExpansionOperation
					{
						Faction = faction,
						ExpansionSearch = new FactionEmpireExpansionSearch
						{
							ScoreControlledSectors = false
						},
						RemainingSectorCount = num
					};
					list.Add(expansionOperation);
					expansionOperation.ExpansionSearch.Initialise((FactionAIEmpire)faction.FactionAI);
				}
			}
			int num2 = 0;
			while (list.Count > 0 && EngineASX.Instance.ControlledSectorCount < maxControlledSectors)
			{
				num2++;
				if (num2 >= 100000)
				{
					Debug.LogError("Max empire expansion iterations reached", this);
					break;
				}
				for (int i = 0; i < list.Count; i++)
				{
					if (EngineASX.Instance.ControlledSectorCount >= maxControlledSectors)
					{
						return;
					}
					ExpansionOperation expansionOperation2 = list[i];
					FactionEmpireExpansionSearch expansionSearch = expansionOperation2.ExpansionSearch;
					if (!expansionSearch.HasFinished)
					{
						expansionSearch.ProcessUntilCompletion();
					}
					if (!expansionSearch.HasFinished)
					{
						continue;
					}
					if (expansionSearch.BestSector != null)
					{
						expansionSearch.BestSector.ChangeControllingFaction(expansionSearch.FactionAI.Faction, setTimeOfChange: false);
						expansionOperation2.RemainingSectorCount--;
						if (expansionOperation2.RemainingSectorCount > 0)
						{
							expansionSearch.Initialise(expansionSearch.FactionAI);
							continue;
						}
						list.RemoveAt(i);
						i--;
					}
					else
					{
						list.RemoveAt(i);
						i--;
					}
				}
			}
		}

		private IEnumerable<Faction> CreateEmpireFactions(int numEmpireFactionsToSpawn, List<Faction> specialEmpireFactions, FactionIntelSeederSettings factionIntelSeederSettings)
		{
			List<Faction> list = new List<Faction>();
			for (int i = 0; i < numEmpireFactionsToSpawn; i++)
			{
				Faction faction = CreateEmpireFactionAndSetup(specialEmpireFactions, factionIntelSeederSettings);
				faction.WasSeeded = true;
				if (faction != null)
				{
					list.Add(faction);
				}
			}
			return list;
		}

		private Faction CreateEmpireFactionAndSetup(List<Faction> specialEmpireFactions, FactionIntelSeederSettings factionIntelSeederSettings)
		{
			Sector sector = FindHomeSectorForEmpireFaction();
			if (sector != null)
			{
				Faction faction = FactionSpawner.CreateFactionAndAI(CreateEmpireFactionsSeederSettings.EmpireSpawnType);
				FactionSeeder.SetSeededFactionCredits(CreateEmpireFactionsSeederSettings.EmpireSpawnType, faction, EngineASX.Instance.World.Seeder.Settings.FactionSeederSettings);
				faction.SpawnType = CreateEmpireFactionsSeederSettings.EmpireSpawnType;
				if (specialEmpireFactions.Count > 0 && (CreateEmpireFactionsSeederSettings.PrioritizeSpecialFactions || Random.value < 0.5f))
				{
					int index = Random.Range(0, specialEmpireFactions.Count);
					Faction faction2 = specialEmpireFactions[index];
					specialEmpireFactions.RemoveAt(index);
					faction.ShortName = faction2.ShortName;
					faction.Name = faction2.Name;
					faction.Description = faction2.Description;
					faction2.CopyPersonalityTo(faction);
				}
				else
				{
					FactionSpawner.AssignNewFactionName(CreateEmpireFactionsSeederSettings.EmpireSpawnType, faction);
				}
				faction.ChangeHomeSector(sector);
				faction.HomeSector.ChangeControllingFaction(faction, setTimeOfChange: false);
				WorldSeederUnitDiscovery.SeedFactionStaticIntel(faction, faction.HomeSector, factionIntelSeederSettings);
				return faction;
			}
			Debug.LogWarning("Could not find a home planet for seed empire faction", this);
			return null;
		}

		private Sector FindHomeSectorForEmpireFaction()
		{
			return (from sector in EngineASX.Instance.Sectors
				where CanBeHomeSectorForEmpire(sector)
				orderby GetSectorScoreAsHomeSectorForEmpire(sector)
				select sector).LastOrDefault();
		}

		private float GetSectorScoreAsHomeSectorForEmpire(Sector sector)
		{
			float num = 0f;
			if (sector.HasPlanets)
			{
				num += 10f;
			}
			if (CreateEmpireFactionsSeederSettings.GroupEmpireFactions)
			{
				num -= sector.FringeSectorRating * 10f;
				int num2 = sector.CalculateJumpDistanceToNearestControlledSector();
				if (num2 > -1 && num2 > 3)
				{
					num -= (float)(num2 - 3) * 10f;
				}
				num -= sector.FringeSectorRating * 10f;
			}
			return num + Random.value * 4f;
		}

		private bool CanBeHomeSectorForEmpire(Sector sector)
		{
			if (sector.ControllingFaction != null)
			{
				return false;
			}
			return MinDistanceFromSectorToControlledSector(sector) >= CreateEmpireFactionsSeederSettings.MinSectorDistanceBetweenFactions;
		}

		private int MinDistanceFromSectorToControlledSector(Sector sector)
		{
			int num = int.MaxValue;
			foreach (Sector sector2 in EngineASX.Instance.Sectors)
			{
				if (sector2.ControllingFaction != null && sector2 != sector)
				{
					int jumpDistanceTo = sector2.GetJumpDistanceTo(sector);
					if (jumpDistanceTo >= 0 && jumpDistanceTo < num)
					{
						num = jumpDistanceTo;
					}
				}
			}
			return num;
		}
	}
}
