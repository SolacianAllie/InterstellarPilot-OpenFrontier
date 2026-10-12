using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Fleets.FleetOrders;
using OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers;
using OpenFrontier.IP.Scenarios;
using OpenFrontier.IP.Testing.Spawning;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Open Frontier: re-rolls the main menu's star system on EVERY menu
	/// visit so the backdrop is a fresh randomly generated system each
	/// time, custom-universe style. Detects the menu scenario the same way
	/// EngineMusicPlayerController does (World.ScenarioInfo matches
	/// GameController.MainMenuScenario), then:
	///   1. re-seeds the sector and regenerates its backdrop (nebulae +
	///      starfield params re-rolled via the stock pipeline)
	///   2. rolls a new star color via the seeded HSV generator and writes
	///      it into the sector's DirectionLightColor (drives light, sun
	///      billboard, tint and the reflection probe's anchor sun)
	///   3. jitters the sky exposure
	///   4. POPULATES the system like a light custom universe: civilian
	///      stations + trader traffic, miner fleets working the clusters,
	///      a security patrol, and a chance of a hostile bandit horde -
	///      faction AI assigns strategies and hostilities play out
	///      organically, so the spectating camera always has life to watch.
	/// ActiveSectorData.OnSectorChanged applies the visuals (backdrop
	/// regen + sun tint refresh + probe recapture).
	/// Lives on the persistent GameController.
	/// </summary>
	public class MainMenuWorldController : MonoBehaviour
	{
		private ScenarioInfo appliedScenario;

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || EngineASX.Instance.World == null || GameController.Instance == null)
			{
				return;
			}
			ScenarioInfo scenarioInfo = EngineASX.Instance.World.ScenarioInfo;
			if (scenarioInfo != appliedScenario)
			{
				appliedScenario = scenarioInfo;
				if (IsMainMenuScenario(scenarioInfo))
				{
					RollMenuSystem();
				}
			}
		}

		private static bool IsMainMenuScenario(ScenarioInfo scenarioInfo)
		{
			ScenarioInfo mainMenuScenario = GameController.Instance.MainMenuScenario;
			return scenarioInfo != null && mainMenuScenario != null && (scenarioInfo == mainMenuScenario || scenarioInfo.UniqueId == mainMenuScenario.UniqueId);
		}

		private void RollMenuSystem()
		{
			Sector sector = EngineASX.Instance.ActiveSector;
			if (sector == null || EngineASX.Instance.ActiveSectorData == null)
			{
				return;
			}
			// WorldBase re-seeds Unity's RNG with the scenario's
			// CustomSeed on load, and the menu's is FIXED - so every
			// UnityEngine.Random call below (and Sector.Init's
			// AssignRandomSeed before us) replayed the same sequence
			// every visit: same sector seed, same sun, same nebulae.
			// Re-seed from entropy or nothing actually re-rolls.
			UnityEngine.Random.InitState(System.Environment.TickCount ^ (int)System.DateTime.Now.Ticks);
			// The sector self-assigns a fresh RandomSeed at load (prefab
			// seed -1 = stock "random per visit" behavior) and the
			// backdrop recipe lives in the prefab's SpaceConstructorParams.
			// We only derive the star color from that seed (keeps
			// sky/sun/star coherent) and jitter the exposure. The guard
			// covers init ordering - if Init hasn't run yet, roll it here.
			// NOTE: Init may have run BEFORE this re-seed (fixed
			// sequence) - force a re-roll regardless so the seed is
			// post-entropy.
			sector.AssignRandomSeed();
			sector.DirectionLightColor = StarColorGenerator.ForSector(sector.RandomSeed);
			sector.SkyExposure = Mathf.Lerp(0.7f, 1.3f, UnityEngine.Random.value);
			// The backdrop recipe (Hellemus clone) pins NebulaColors to
			// flag value 1 = BLUE only, NebulaCount 39, StarsIntensity
			// ~0.3. Re-roll all three per visit. Colors use a rarity
			// ladder (the more colors, the rarer the system, 10% full
			// rainbow); density and star intensity use the godmode
			// menu's own min/max (0-64 nebulae, 0-2 star intensity).
			CustomSectorAppearance component = sector.GetComponent<CustomSectorAppearance>();
			if (component != null && component.SpaceConstructorParams != null)
			{
				Common.NebulaColour[] array = new Common.NebulaColour[8]
				{
					Common.NebulaColour.BLUE,
					Common.NebulaColour.PINK,
					Common.NebulaColour.PURPLE,
					Common.NebulaColour.GREEN,
					Common.NebulaColour.YELLOW,
					Common.NebulaColour.ORANGE,
					Common.NebulaColour.RED,
					Common.NebulaColour.CYAN
				};
				for (int i = array.Length - 1; i > 0; i--)
				{
					int num3 = UnityEngine.Random.Range(0, i + 1);
					Common.NebulaColour nebulaColour = array[i];
					array[i] = array[num3];
					array[num3] = nebulaColour;
				}
				// Even rarity ladder: cumulative P(>=N) drops linearly
				// from 100% (N=1) to 10% (N=8) - 12.86% per step, so
				// every exact count 1-7 lands at ~12.9% and the rainbow
				// stays the 10% jackpot.
				float value = UnityEngine.Random.value;
				int num4;
				if (value < 0.1f)
				{
					num4 = 8;
				}
				else if (value < 0.2286f)
				{
					num4 = 7;
				}
				else if (value < 0.3571f)
				{
					num4 = 6;
				}
				else if (value < 0.4857f)
				{
					num4 = 5;
				}
				else if (value < 0.6143f)
				{
					num4 = 4;
				}
				else if (value < 0.7429f)
				{
					num4 = 3;
				}
				else if (value < 0.8714f)
				{
					num4 = 2;
				}
				else
				{
					num4 = 1;
				}
				Common.NebulaColour nebulaColors = (Common.NebulaColour)0;
				for (int j = 0; j < num4; j++)
				{
					nebulaColors |= array[j];
				}
				component.SpaceConstructorParams.NebulaColors = nebulaColors;
				component.SpaceConstructorParams.NebulaCount = UnityEngine.Random.Range(0, 65);
				component.SpaceConstructorParams.StarsIntensity = UnityEngine.Random.Range(0f, 2f);
			}
			// Replace the preset dressing before regenerating: stock
			// stations/ships/planet/shuttle/clusters go, everything comes
			// back randomly below.
			ClearPresetMenuContent();
			// Belts are a chance, not a guarantee: a belted system gets
			// real mineable asteroids, a refinery within reach and miner
			// fleets working the rocks; a beltless one is stations and
			// traders only - so some menus have miners, some don't.
			List<AsteroidCluster> clusters = ((UnityEngine.Random.value < 0.7f) ? SpawnAsteroidClusters(sector) : new List<AsteroidCluster>());
			// Applies everything: regenerates the backdrop, refreshes the
			// sun tint, recaptures the reflection probe.
			EngineASX.Instance.ActiveSectorData.OnSectorChanged();
			SpawnGuaranteedPlanetWithMoon(sector);
			PopulateMenuSystem(sector, clusters);
			SpawnHeroShipAndSpectate(sector);
			MakeMenuAIAggressive();
			Debug.Log("[MainMenuWorldController] rolled menu system, seed " + sector.RandomSeed);
		}

		// The trader faction that owns the stations and traffic; the hero
		// ship joins it so the faction AI hands it trade runs.
		private static Faction civilianFaction;

		// The MenuScreenScene's hand-placed world content - replaced by the
		// random composition below on every visit. EVERY unit goes: the
		// old name/prefix list missed preset fleet SHIPS, because the
		// fleet roots (GenericGroupNoCloak*) carry Fleet+DestructableUnit
		// but no Unit component, and their ships reparent to the sector
		// at Init - destroying the root orphaned live ships, which then
		// sat around doing nothing (the idle Hornet). No name filtering.
		private static readonly string[] PresetRootPrefixes = new string[5] { "Bay_", "Dock", "AIGenericPilotController", "GenericNpc", "GenericGroupNoCloak" };

		private static void ClearPresetMenuContent()
		{
			List<Unit> list = new List<Unit>();
			EngineASX.Instance.EnumerateUnitsWithPredicate(delegate(Unit unit)
			{
				Unit rootUnit = unit.GetRootUnit();
				if (rootUnit != null && !list.Contains(rootUnit))
				{
					list.Add(rootUnit);
				}
			}, (Unit e) => e.IsValidAndNotDestroyed);
			foreach (Unit item in list)
			{
				// Notify first (fleets and AI processors drop their
				// references), then clean up - raw Destroy leaves the
				// faction AI ticking over corpses and spams the log.
				EngineASX.Instance.RegisterDestroyedUnit(item);
				item.SafeDestroy();
			}
			// Non-unit preset shells: fleet roots (no Unit component -
			// their ships died above), pilot controllers and npc
			// spawners.
			foreach (GameObject gameObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
			{
				string name = gameObject.name;
				if (PresetRootPrefixes.Any((string prefix) => name.StartsWith(prefix)))
				{
					Unit component = gameObject.GetComponent<Unit>();
					if (component != null)
					{
						EngineASX.Instance.RegisterDestroyedUnit(component);
						component.SafeDestroy();
					}
					else
					{
						Object.Destroy(gameObject);
					}
				}
			}
			// The easter egg bandit-horde trigger lives UNDER the NewGame
			// flow object, so the root pass above never sees it - and its
			// condition is Trigger_Always evaluated during the intro
			// state: a GUARANTEED horde on every menu visit. Destroy it;
			// bandits now come only from the rare roll in
			// PopulateMenuSystem.
			GameObject gameObject2 = GameObject.Find("Trigger_EasterEgg_SpawnBanditHorde");
			if (gameObject2 != null)
			{
				Object.Destroy(gameObject2);
			}
		}

		// Fresh belts (1-2), each filled with REAL mineable asteroid units
		// so miner fleets have something to work - the decorative cluster
		// rocks alone aren't harvestable. Belts may come with a gas cloud
		// (variance): gassy types roll the shared probability, the NoGas
		// type variants stay clean by construction.
		private List<AsteroidCluster> SpawnAsteroidClusters(Sector sector)
		{
			List<AsteroidCluster> list = new List<AsteroidCluster>();
			AsteroidType[] array = Resources.LoadAll<AsteroidType>("Prefabs/WorldData/AsteroidTypes");
			if (array.Length == 0)
			{
				return list;
			}
			int num = UnityEngine.Random.Range(1, 3);
			// Mirror the sandbox generator's spacing rule (same settings
			// source, CreateAsteroidClustersSeeder): a new belt's edge must
			// clear every placed belt's edge by MinDistanceBetweenAsteroidClusters.
			// The menu used to place blind, so two 4000-radius belts could
			// land on top of each other.
			float num2 = 400f;
			float num3 = 0.5f;
			CreateAsteroidClustersSeederSettings createAsteroidClustersSeederSettings = GameController.Instance.GameSettings.DefaultWorldSeedSettings.CreateAsteroidClustersSeederSettings;
			if (createAsteroidClustersSeederSettings != null)
			{
				num2 = createAsteroidClustersSeederSettings.MinDistanceBetweenAsteroidClusters;
				num3 = createAsteroidClustersSeederSettings.ProbabilityOfGeneratingGasCloud;
			}
			for (int i = 0; i < num; i++)
			{
				AsteroidType asteroidType = array[UnityEngine.Random.Range(0, array.Length)];
				AsteroidCluster asteroidClusterPrefab = asteroidType.AsteroidClusterPrefabs[UnityEngine.Random.Range(0, asteroidType.AsteroidClusterPrefabs.Count)];
				AsteroidCluster asteroidCluster = UnityObjectHelper.InstantiateAndGetComponent(asteroidClusterPrefab);
				asteroidCluster.Unit.Radius = UnityEngine.Random.Range(2500f, 4000f);
				asteroidCluster.transform.SetParent(sector.transform, worldPositionStays: true);
				Vector3 localPosition = sector.GetRandomSectorPositionWithinGateDistance(0.75f);
				int j = 0;
				for (; j < 6; j++)
				{
					if (!list.Any((AsteroidCluster e) => Vector3.Distance(e.Unit.SectorPosition, localPosition) < num2 + e.Unit.Radius + asteroidCluster.Unit.Radius))
					{
						break;
					}
					localPosition = sector.GetRandomSectorPositionWithinGateDistance(0.75f);
				}
				if (j == 6)
				{
					// No clear spot in 6 tries (sandbox seeder behaviour):
					// drop this belt rather than overlap a placed one.
					Object.Destroy(asteroidCluster.gameObject);
					continue;
				}
				asteroidCluster.transform.localPosition = localPosition;
				asteroidCluster.GetComponent<Unit>().Init();
				SeedRealAsteroids(asteroidCluster);
				// Roll gas from the PICKED type, not the cluster prefab's
				// back-referenced type (always the gassy original) - that's
				// what makes the NoGas variants actually stay clean.
				Unit random = asteroidType.GasCloudPrefabs.GetRandom();
				if (random != null && UnityEngine.Random.value < num3)
				{
					CreateAsteroidClustersSeeder.CreateGasCloud(sector, asteroidCluster.Unit.Radius, localPosition, random);
				}
				list.Add(asteroidCluster);
			}
			return list;
		}

		// The stock CreateAsteroidsSeeder world layer, run per-cluster:
		// scatters real asteroid units through the cluster volume.
		public CreateAsteroidsSeederSettings AsteroidsSettings;

		private void SeedRealAsteroids(AsteroidCluster cluster)
		{
			if (AsteroidsSettings == null)
			{
				Debug.LogWarning("[MainMenuWorldController] AsteroidsSettings not wired - belt has decorative rocks only");
				return;
			}
			GameObject gameObject = new GameObject("MenuAsteroidSeeder");
			CreateAsteroidsSeeder createAsteroidsSeeder = gameObject.AddComponent<CreateAsteroidsSeeder>();
			createAsteroidsSeeder.CreateAsteroidsSeederSettings = AsteroidsSettings;
			createAsteroidsSeeder.PopulateAsteroidsAroundCluster(cluster);
			Object.Destroy(gameObject);
		}

		// The menu ALWAYS has a planet, and it ALWAYS has a moon. Uses the
		// stock universe/sandbox pipeline (CreatePlanetsSeeder driven by the
		// shared WorldSeederSettings) verbatim, then tops up the moon if
		// the 0.8 probability didn't produce one.
		public CreatePlanetsSeederSettings PlanetsSettings;

		private void SpawnGuaranteedPlanetWithMoon(Sector sector)
		{
			if (PlanetsSettings == null)
			{
				return;
			}
			// The proven sandbox/universe path, verbatim.
			GameObject gameObject = new GameObject("MenuPlanetsSeeder");
			CreatePlanetsSeeder createPlanetsSeeder = gameObject.AddComponent<CreatePlanetsSeeder>();
			createPlanetsSeeder.CreatePlanetsSeederSettings = PlanetsSettings;
			createPlanetsSeeder.CreateScenePlanets(sector);
			Object.Destroy(gameObject);
			// Moon ladder (user-tuned): 1 guaranteed, then keep rolling -
			// 85% for a 2nd, 65% for a 3rd, 45% for a 4th, 25% for a
			// 5th, then 5% for each further one, hard-capped at 8. The
			// stock 0.8 roll may already have produced one, which counts.
			int num = RollMenuMoonTargetCount();
			int num2 = 0;
			List<Unit> list = new List<Unit>();
			EngineASX.Instance.EnumerateUnitsWithPredicate(delegate(Unit unit)
			{
				if (unit.GetComponent<Moon>() != null && unit.Sector == sector)
				{
					num2++;
				}
				else if (unit.Sector == sector)
				{
					list.Add(unit);
				}
			}, (Unit e) => e.IsValidAndNotDestroyed && e.UnitType == UnitType.Planet);
			if (list.Count > 0 && PlanetsSettings.MoonPrefabs.Count > 0)
			{
				Unit unit2 = list[list.Count - 1];
				for (int i = num2; i < num; i++)
				{
					Unit unit3 = UnityObjectHelper.InstantiateAndGetComponent(PlanetsSettings.MoonPrefabs[UnityEngine.Random.Range(0, PlanetsSettings.MoonPrefabs.Count)]);
					Moon component = unit3.GetComponent<Moon>();
					if (component != null)
					{
						component.OrbitingAroundUnit = unit2;
						CreatePlanetsSeeder.RandomizeMoon(component, PlanetsSettings);
						unit3.GetComponent<Unit>().Init(autoFindParents: false);
						unit3.Sector = sector;
					}
				}
			}
			PlaceMoonsBesideTheDisc(sector);
			int num3 = 0;
			int num4 = 0;
			EngineASX.Instance.EnumerateUnitsWithPredicate(delegate(Unit unit)
			{
				if (unit.GetComponent<Moon>() != null)
				{
					num4++;
				}
				else
				{
					num3++;
				}
			}, (Unit e) => e.IsValidAndNotDestroyed && e.Sector == sector && e.UnitType == UnitType.Planet);
		}

		// Menu moon ladder: chance to ADD the Nth moon (conditional
		// rolls). 6+ keeps rolling 5%, hard-capped at 8.
		private static readonly float[] MoonAddChances = new float[7] { 0.85f, 0.65f, 0.45f, 0.25f, 0.05f, 0.05f, 0.05f };

		private static int RollMenuMoonTargetCount()
		{
			int num = 1;
			while (num - 1 < MoonAddChances.Length && UnityEngine.Random.value < MoonAddChances[num - 1])
			{
				num++;
			}
			return num;
		}

		// The stock settings park moons 40000u from their planet -
		// authored for the sector map, not a camera. The planet stays at
		// its stock ~20000u backdrop spot (moving it close let it
		// swallow the spectating camera), so moons are placed hugging
		// the disc: innermost at ~1.5x the planet's TRUE visual radius
		// (renderer bounds - the serialized Unit.Radius is a placeholder
		// 1), each next one further out, spread evenly around the disc
		// starting 90 degrees off the planet->camera line so nothing
		// starts hidden behind the planet. The orbit phase is unit.Seed
		// % 360 in ActiveUnitMoon.Reposition - zeroed so these offsets
		// apply as-authored (the slow stock orbit drifts on from here).
		private static void PlaceMoonsBesideTheDisc(Sector sector)
		{
			Dictionary<Unit, List<Unit>> dictionary = new Dictionary<Unit, List<Unit>>();
			EngineASX.Instance.EnumerateUnitsWithPredicate(delegate(Unit unit)
			{
				Moon component = unit.GetComponent<Moon>();
				if (component != null && component.OrbitingAroundUnit != null)
				{
					if (!dictionary.TryGetValue(component.OrbitingAroundUnit, out var value))
					{
						value = new List<Unit>();
						dictionary[component.OrbitingAroundUnit] = value;
					}
					value.Add(unit);
				}
			}, (Unit e) => e.IsValidAndNotDestroyed && e.Sector == sector && e.UnitType == UnitType.Planet);
			foreach (KeyValuePair<Unit, List<Unit>> item in dictionary)
			{
				float num = 0f;
				Renderer componentInChildren = item.Key.GetComponentInChildren<Renderer>();
				if (componentInChildren != null)
				{
					// extents.magnitude of a sphere = radius * sqrt(3)
					num = componentInChildren.bounds.extents.magnitude / 1.732f;
				}
				if (num <= 0f)
				{
					num = Mathf.Max(1000f, item.Key.Radius);
				}
				Vector3 vector = -item.Key.transform.localPosition;
				vector.y = 0f;
				vector = ((vector.sqrMagnitude < 1f) ? Vector3.forward : vector.normalized);
				Vector3 vector2 = Quaternion.Euler(0f, 90f * ((UnityEngine.Random.value < 0.5f) ? 1f : (-1f)), 0f) * vector;
				for (int i = 0; i < item.Value.Count; i++)
				{
					Unit unit2 = item.Value[i];
					Moon component2 = unit2.GetComponent<Moon>();
					float num2 = num * Mathf.Min(1.5f * Mathf.Pow(1.35f, i), 7f);
					float num3 = (float)i * (360f / (float)item.Value.Count) + UnityEngine.Random.Range(-10f, 10f);
					Vector3 vector3 = Quaternion.Euler(0f, num3, 0f) * vector2;
					component2.OffsetFromPlanet = (vector3 + Vector3.up * UnityEngine.Random.Range(-0.08f, 0.08f)).normalized * num2;
					unit2.Seed = 0;
					// Size variety: 0.5-4x per moon (user-calibrated on
					// screen), so a multi-moon system reads as distinct
					// bodies, not clones. Orbit math uses the PLANET's
					// radius, so scale never hides a moon inside it.
					// The CLOSEST moon (i=0) is clamped to 1x max - it
					// sits near the camera's beat and a giant there
					// swallows the frame.
					float num5 = ((i == 0) ? UnityEngine.Random.Range(0.5f, 1f) : UnityEngine.Random.Range(0.5f, 4f));
					unit2.transform.localScale = Vector3.one * num5;
				}
			}
		}

		// The camera's opening view: a random ship from the whole pool, so
		// over time the menu shows off every ship class in the game. It
		// joins the trader faction as a fleet of one, so the faction AI
		// gives it the old shuttle's life: exploring the system and
		// periodically docking at stations.
		private static void SpawnHeroShipAndSpectate(Sector sector)
		{
			var list = GameController.Instance.LoadedUnitClasses.Where((UnitClass e) => e.IsUsable && e.UnitType == UnitType.Ship).ToList();
			if (list.Count == 0)
			{
				return;
			}
			// Spawn out in the deep black: sample candidates on the
			// sector's outer rim and keep the one farthest from any
			// station, so the hero ship has to FLY IN before it can
			// work. Its faction gets a few million credits of working
			// capital for the trip.
			Vector3 vector = sector.GetRandomSectorPositionWithinGateDistance(0.9f);
			float num = 0f;
			List<Unit> list2 = sector.GetUnitsByType(UnitType.Station);
			for (int i = 0; i < 6; i++)
			{
				Vector3 vector2 = Geometry.RandomXZUnitVector() * (sector.GetActualGateDistance() * UnityEngine.Random.Range(0.85f, 1.15f));
				float num2 = float.MaxValue;
				if (list2 != null)
				{
					foreach (Unit item in list2)
					{
						num2 = Mathf.Min(num2, Vector3.Distance(vector2, item.SectorPosition));
					}
				}
				if (num2 > num)
				{
					num = num2;
					vector = vector2;
				}
			}
			Unit unit = SpawnUtils.SpawnUnit(list[UnityEngine.Random.Range(0, list.Count)].UnitPrefab, sector, vector, civilianFaction);
			if (unit != null)
			{
				if (civilianFaction != null)
				{
					civilianFaction.Credits += UnityEngine.Random.Range(2000000, 5000001);
					SpawnUtils.SpawnGenericFleetWithUnits(civilianFaction, new List<Unit> { unit });
				}
				if (EngineASX.Instance.World != null)
				{
					EngineASX.Instance.World.Engine.CameraStartSpectateUnit(unit, allowFlyby: true, allowViewport: false, allowOrbit: true);
				}
			}
		}

		// Light custom-universe population: ONE OF EVERY station type,
		// spread around the sector, each with its own faction + trade
		// fleet and sometimes a patrol. Refineries park near the belt so
		// miners have somewhere close to haul ore, and every cluster gets
		// at least one miner fleet dropped right into the rocks.
		private static void PopulateMenuSystem(Sector sector, List<AsteroidCluster> clusters)
		{
			if (EngineASX.Instance.FactionSpawner == null || EngineASX.Instance.FactionSpawner.Settings == null)
			{
				return;
			}
			bool flag = clusters.Count > 0;
			float actualGateDistance = sector.GetActualGateDistance();
			var list = GameController.Instance.LoadedUnitClasses.Where((UnitClass e) => e.IsUsable && e.UnitType == UnitType.Station && e.SeedInSandbox && e.StationPurpose != StationPurpose.SectorControl).OrderBy((UnitClass e) => UnityEngine.Random.value).ToList();
			civilianFaction = null;
			float num = 6.2831855f / Mathf.Max(1, list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				Faction faction = CreateAIFaction(FactionType.Trader);
				if (faction == null)
				{
					break;
				}
				if (civilianFaction == null)
				{
					civilianFaction = faction;
				}
				UnitClass unitClass = list[i];
				Vector3 vector;
				if ((unitClass.StationPurpose & StationPurpose.Refinery) != 0 && flag)
				{
					// Refineries try for the belt: just outside a random
					// cluster, a short ore haul for the miners.
					AsteroidCluster asteroidCluster = clusters[UnityEngine.Random.Range(0, clusters.Count)];
					vector = asteroidCluster.Unit.SectorPosition + UnityEngine.Random.onUnitSphere * (asteroidCluster.Unit.Radius + 1500f);
				}
				else
				{
					// Ring spread: even angular slots with a little
					// jitter, radius in a mid-band so nothing clumps.
					float f = (float)i * num + UnityEngine.Random.Range(-0.3f, 0.3f) * num;
					float num2 = actualGateDistance * UnityEngine.Random.Range(0.35f, 0.8f);
					vector = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f)) * num2;
				}
				Unit unit = SpawnUtils.SpawnUnit(unitClass.UnitPrefab, sector, vector, faction);
				Vector3 vector2 = ((unit != null) ? unit.SectorPosition : vector);
				// 2-3 SOLO traders working out of home - each its own
				// one-ship fleet with a standing trade order, so ships
				// run independent routes instead of flying formation.
				int num3 = UnityEngine.Random.Range(2, 4);
				for (int k = 0; k < num3; k++)
				{
					ForceTradeOrders(SpawnSoloShipFleet(faction, sector, vector2 + UnityEngine.Random.onUnitSphere * 900f, TraderPrefabs[UnityEngine.Random.Range(0, TraderPrefabs.Length)]));
				}
				// NOTE: no explicit station patrols - the spawner settings
				// have no non-outlaw Security faction type (the attempt
				// only ever logged a warning), and the strategy layer now
				// generates organic patrol/protect fleets on its own.
			}
			// Every cluster gets 1-2 SOLO miners, dropped in the rocks
			// and force-ordered to mine then sell.
			if (flag)
			{
				Faction faction2 = CreateAIFaction(FactionType.Miner);
				if (faction2 != null)
				{
					foreach (AsteroidCluster cluster in clusters)
					{
						// 1-2 miners PER ASTEROID in the belt, each dropped
						// beside its rock: the belt visibly crawls with
						// workers instead of one lonely hauler.
						List<Unit> list2 = sector.GetUnitsByType(UnitType.Asteroid)?.Where((Unit u) => u != null && Vector3.Distance(u.SectorPosition, cluster.Unit.SectorPosition) <= cluster.Unit.Radius * 1.1f).ToList() ?? new List<Unit>();
						if (list2.Count == 0)
						{
							// Belt somehow rockless - old 1-2 per-cluster
							// behaviour as a fallback.
							int num6 = UnityEngine.Random.Range(1, 3);
							for (int m = 0; m < num6; m++)
							{
								ForceMineOrders(SpawnSoloShipFleet(faction2, sector, cluster.Unit.SectorPosition + UnityEngine.Random.onUnitSphere * (cluster.Unit.Radius * 0.5f), GameController.Instance.UnitClasses.Hauler_M.UnitPrefab));
							}
							continue;
						}
						foreach (Unit item2 in list2)
						{
							int num4 = UnityEngine.Random.Range(1, 3);
							for (int l = 0; l < num4; l++)
							{
								// Spawn at the belt's EDGE (1.0-1.25x
								// radius), not beside the rock: miners
								// fly in over different distances and
								// start work staggered, not all at once.
								Vector3 vector3 = cluster.Unit.SectorPosition + Geometry.RandomXZUnitVector() * (cluster.Unit.Radius * UnityEngine.Random.Range(1f, 1.25f));
								ForceMineOrders(SpawnSoloShipFleet(faction2, sector, vector3, GameController.Instance.UnitClasses.Hauler_M.UnitPrefab));
							}
						}
					}
				}
			}
			// Bandits are a rare event now - the easter egg trigger can
			// also still fire one off on its own.
			if (UnityEngine.Random.value < 0.15f)
			{
				SpawnUtils.SpawnBanditHordesAtSectorPosition(sector, sector.GetRandomSectorPositionWithinGateDistance(0.75f), 1, 2);
			}
			// Stock the shelves: menu stations spawn AFTER world seeding,
			// so they miss WorldTraderCargoSeeder and open with EMPTY
			// cargo bays - no sellable stock = no trade routes = traders
			// "finding trade route" forever. Run the stock pipeline's fill.
			WorldTraderCargoSeederSettings worldTraderCargoSeederSettings = GameController.Instance.GameSettings.DefaultWorldSeedSettings.WorldTraderCargoSeederSettings;
			if (worldTraderCargoSeederSettings != null)
			{
				foreach (CargoTrader trader in EngineASX.Instance.Traders)
				{
					// One bad station must not abort the fill (an NRE here
					// once killed the whole roll tail: no intel, no
					// strategies, no peace - the idle-ship bug).
					try
					{
						WorldTraderCargoSeeder.TrySpawnCargo(trader.UnitComponents, worldTraderCargoSeederSettings);
					}
					catch (System.Exception ex)
					{
						Debug.LogWarning("[MainMenuWorldController] cargo fill failed for " + trader.name + ": " + ex);
					}
				}
			}
			DiscoverMenuSectorForAllFactions(sector);
			AssignStrategiesToIdleFleets();
			MakeMenuFactionsPeaceful();
		}

		// Trade-ship sets mirroring SpawnUtils' hardcoded menu picks.
		private static Unit[] TraderPrefabs => new Unit[3]
		{
			GameController.Instance.UnitClasses.Hauler_A.UnitPrefab,
			GameController.Instance.UnitClasses.Venture_A.UnitPrefab,
			GameController.Instance.UnitClasses.Shuttle_A.UnitPrefab
		};

		// One ship, its own fleet - orders only live on fleets, so a
		// "lone" ship is a fleet of one. Uses the generic fleet prefab
		// (SpawnUtils' fleet helpers build from the PLAYER fleet prefab).
		private static Fleet SpawnSoloShipFleet(Faction faction, Sector sector, Vector3 sectorPosition, Unit prefab)
		{
			Unit unit = SpawnUtils.SpawnUnit(prefab, sector, sectorPosition, faction);
			if (unit == null)
			{
				return null;
			}
			return SpawnUtils.SpawnGenericFleetWithUnits(faction, new List<Unit> { unit });
		}

		// Mirrors FactionAIBase's Trade strategy: a standing autonomous
		// trade order (repeatable - loops forever).
		private static void ForceTradeOrders(Fleet fleet)
		{
			if (fleet == null)
			{
				return;
			}
			AutonomousTradeOrder autonomousTradeOrder = UnityObjectHelper.NewGameObject<AutonomousTradeOrder>();
			autonomousTradeOrder.MaxJumpDistance = 4;
			autonomousTradeOrder.MaxDuration = 0f;
			// Repeat (not Requeue): Requeue silently drops the order when
			// it's the fleet's ONLY one (it re-appends to the back of a
			// non-empty queue); Repeat re-inserts at the front - a solo
			// trader's trade run loops forever.
			autonomousTradeOrder.CompletionMode = FleetOrderCompletionMode.Repeat;
			fleet.EnqueueOrder(autonomousTradeOrder);
		}

		// Mirrors FactionAIBase's Mine strategy verbatim: mine until the
		// hold is full, then sell the haul. One-shot orders; the faction
		// AI's idle pass re-orders when they complete.
		private static void ForceMineOrders(Fleet fleet)
		{
			if (fleet == null)
			{
				return;
			}
			MineOrder mineOrder = UnityObjectHelper.NewGameObject<MineOrder>();
			mineOrder.MaxJumpDistance = 2;
			// Requeue (not Destroy): on completion the order goes to the
			// back of the queue, so [mine, sell] cycles forever - the same
			// pattern the game's own OrdersHelper uses. With Destroy the
			// fleet went permanently idle after its first cycle.
			mineOrder.CompletionMode = FleetOrderCompletionMode.Requeue;
			mineOrder.MaxDuration = mineOrder.AIDefaultMaxDuration;
			fleet.EnqueueOrder(mineOrder);
			SellCargoOrder sellCargoOrder = UnityObjectHelper.NewGameObject<SellCargoOrder>();
			sellCargoOrder.CompletionMode = FleetOrderCompletionMode.Requeue;
			sellCargoOrder.SellEquipment = false;
			sellCargoOrder.MaxJumpDistance = 4;
			sellCargoOrder.MaxDuration = sellCargoOrder.AIDefaultMaxDuration;
			sellCargoOrder.CompleteWhenNoCargoToSell = true;
			sellCargoOrder.CompleteWhenNoBuyerFound = false;
			sellCargoOrder.MinBuyPriceMultiplier = 0.05f;
			fleet.EnqueueOrder(sellCargoOrder);
		}

		// The AI genuinely needs discovery: a faction with zero discovered
		// asteroids gets EXPLORE orders instead of mine orders (stock
		// FactionAIBase behavior). Reveal the whole sector to every menu
		// faction so trade/mine target searches work from frame one.
		private static void DiscoverMenuSectorForAllFactions(Sector sector)
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction != null && faction.Intel != null)
				{
					faction.Intel.DiscoverEverythingInSector(sector);
				}
			}
		}

		// The "ships sitting around doing nothing" fix: generic fleets
		// spawn strategy-less and wait for the AI's periodic idle pass -
		// assign strategies at creation so everyone works immediately.
		private static void AssignStrategiesToIdleFleets()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.FactionAI == null)
				{
					continue;
				}
				foreach (Fleet fleet in faction.Fleets)
				{
					// FleetStrategy is a non-nullable enum FIELD - "== null"
					// compiled to always-false (CS0472), so this whole pass
					// was dead code and strategy-less fleets idled forever.
					if (fleet != null && fleet.FleetStrategy == FactionStrategy.Unspecified)
					{
						fleet.FleetStrategy = FactionAIStrategyModule.GetBestStrategyForFleet(fleet, faction.FactionAI);
					}
				}
			}
		}

		// Menu spectacle: max everyone's aggression so hostiles on sensors
		// get engaged regardless of target score - the whole system piles
		// onto the bandits instead of waiting to be attacked. Aggression
		// 1 zeroes the attack/intercept score thresholds; unarmed ships
		// still can't intercept (no weapons), and the 2km interception
		// distance gate stays stock. Runs after ALL spawning so even the
		// hero ship's one-ship fleet is covered. FleetSettings is a
		// per-fleet component - safe to write, nothing leaks.
		private static void MakeMenuAIAggressive()
		{
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				foreach (Fleet fleet in faction.Fleets)
				{
					if (fleet != null && fleet.Settings != null)
					{
						fleet.Settings.Aggression = 1f;
					}
				}
			}
		}

		private static Faction CreateAIFaction(FactionType factionType)
		{
			// Exact-flag match, outlaws excluded: a flag subset match can
			// pick up outlaw-flavoured variants ("outlaw trader").
			FactionSpawnerSpawnType factionSpawnerSpawnType = EngineASX.Instance.FactionSpawner.Settings.FactionTypes.FirstOrDefault((FactionSpawnerSpawnType e) => e.TypeInfo.FactionType == factionType && (e.TypeInfo.FactionType & FactionType.Outlaw) == 0 && !e.IsFreelancer);
			if (factionSpawnerSpawnType == null)
			{
				Debug.LogWarning("[MainMenuWorldController] no AI faction type available for " + factionType);
				return null;
			}
			return FactionSpawner.CreateFactionAndAIAndAssignName(factionSpawnerSpawnType);
		}

		// Everyone in the menu coexists peacefully - the bandit horde is
		// the only sanctioned hostility. Also stops the ambient faction
		// spawner dropping random (possibly outlaw) factions into the menu.
		private static void MakeMenuFactionsPeaceful()
		{
			if (EngineASX.Instance.FactionSpawner != null)
			{
				EngineASX.Instance.FactionSpawner.enabled = false;
			}
			List<Faction> list = EngineASX.Instance.Factions.Where((Faction e) => e != null && e.FactionType != FactionType.Bandit).ToList();
			foreach (Faction faction in list)
			{
				foreach (Faction faction2 in list)
				{
					if (faction != faction2)
					{
						faction.SetNeutralityWith(faction2, Neutrality.Neutral);
					}
				}
			}
		}
	}
}
