using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.StationBuilding;
using OpenFrontier.IP.Engine.WorldPopulation;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.CreateBars
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreateBarsSeeder : MonoBehaviour
	{
		public CreateBarsSeederSettings CreateBarsSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (!world.Seeder.Settings.FactionSeederSettings.SeedNonBanditFactions)
			{
				return;
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Seeding bars...", this, 1);
			}
			CreateBarsSeederSettings = world.Seeder.Settings.CreateBarsSeederSettings;
			int requiredFactionCountOfType = FactionSpawner.GetRequiredFactionCountOfType(CreateBarsSeederSettings.FactionSpawnType);
			requiredFactionCountOfType = Mathf.CeilToInt((float)requiredFactionCountOfType * CreateBarsSeederSettings.FactionSpawnType.SeedCountMultiplier);
			List<CreateBarCustomName> specialNames = CreateBarsSeederSettings.CustomNames.ToList();
			for (int i = 0; i < requiredFactionCountOfType; i++)
			{
				Sector bestHomeSectorForNewFactionType = FactionSpawner.GetBestHomeSectorForNewFactionType(CreateBarsSeederSettings.FactionSpawnType.TypeInfo, !CreateBarsSeederSettings.FactionSpawnType.IsFreelancer);
				if (bestHomeSectorForNewFactionType != null)
				{
					SeedFaction(CreateBarsSeederSettings.FactionSpawnType, bestHomeSectorForNewFactionType, world.Seeder.Settings.FactionSeederSettings.GetNonBanditCreditsPower(1f), specialNames);
				}
				else
				{
					Debug.LogError("Couldn't find home sector for new bar owner faction", this);
				}
			}
		}

		private Faction SeedFaction(FactionSpawnerSpawnType spawnType, Sector homeSector, float wealthPower, List<CreateBarCustomName> specialNames)
		{
			Faction faction = FactionSpawner.CreateFactionAndAI(spawnType);
			CreateBarCustomName createBarCustomName = null;
			if (faction != null)
			{
				if (specialNames.Count > 0 && Random.value < 0.5f)
				{
					int index = Random.Range(0, specialNames.Count);
					createBarCustomName = specialNames[index];
					faction.ShortName = (faction.Name = createBarCustomName.FactionName);
					specialNames.RemoveAt(index);
				}
				else
				{
					FactionSpawner.AssignNewFactionName(spawnType, faction);
				}
				faction.WasSeeded = true;
				faction.ChangeHomeSector(homeSector);
				faction.SpawnType = spawnType;
				int minSeedCredits = spawnType.MinSeedCredits;
				int maxSeedCredits = spawnType.MaxSeedCredits;
				FactionSeeder.SetFactionCredits(faction, spawnType, wealthPower, minSeedCredits, maxSeedCredits);
				WorldStationSeeder.StationBuild nextStationBuild = faction.FactionAI.StationBuilder.GetNextStationBuild(faction.Credits, considerShipRatio: false);
				if (nextStationBuild != null)
				{
					faction.FactionAI.SetLastBuiltUnitTimeToCurrent();
					Unit unit = FactionStationBuilder.BuildStation(nextStationBuild, cargoLoadout: true, underConstruction: false);
					unit.Faction = faction;
					Person person = faction.FactionAI.SpawnAndInitialisePerson(faction.FactionAI.GetPilotPrefab());
					person.CurrentUnit = unit;
					if (createBarCustomName != null)
					{
						UnitClass random = EngineASX.Instance.UnitClasses.Where((UnitClass e) => e.IsUsable && !e.IsBarebonesShip && e.HasStrategy(FactionStrategy.Trade)).GetRandom();
						if (random != null)
						{
							Vector3 sectorPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(faction.HomeSector, unit.SectorPosition + Geometry.RandomXZUnitVector() * Random.Range(200f, 400f), 50f, GameController.Instance.StaticNonOverlappingMask);
							Unit unit2 = WorldHelper.SpawnUnitAndInstallComponents(random.UnitPrefab, unit.Sector, sectorPosition);
							unit2.Faction = faction;
							NpcPilot npcPilot = faction.FactionAI.SpawnAndInitialiseNpcPilot(faction.FactionAI.GetPilotPrefab(), unit2);
							npcPilot.Person.IsMale = createBarCustomName.IsMale;
							npcPilot.Person.CustomName = createBarCustomName.PersonName;
							npcPilot.Person.RefreshName();
							faction.LeaderPerson = npcPilot.Person;
						}
						else
						{
							Debug.LogError("Couldn't create custom unit for new bar faction", faction);
						}
					}
					else
					{
						faction.LeaderPerson = person;
					}
				}
				else
				{
					Debug.LogError("Couldn't create bar for new faction", faction);
				}
			}
			return faction;
		}
	}
}
