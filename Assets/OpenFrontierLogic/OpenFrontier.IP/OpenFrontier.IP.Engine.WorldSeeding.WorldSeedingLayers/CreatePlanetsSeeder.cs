using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers
{
	// ---- planets scaled to themselves ----
	//
	// What generation used to do: every planet class renders an active unit
	// whose sphere child is scaled 900 over a mesh with extents (10,10,10),
	// so every planet measured the same radius - about 9000 world units,
	// 9100 with its atmosphere shell - and that body was parked at a random
	// annulus 19800..20000 from the sector centre. Since neighbouring
	// sector anchors sit only 8000 apart, the disc already outran its own
	// slot, and moons orbited at a flat 40000 - further out than the
	// neighbouring planet is far. Nothing in the pipeline had ever read a
	// planet's size, because every prefab carried the same one.
	//
	// What this layer now does: read the size, then place by it. The size
	// is authored ON THE PREFAB - the active unit hierarchy's own
	// transform scale - and nothing in the pipeline rescales a body. The
	// same prefab renders the same silhouette in every world on every
	// seed, and every measurement below reads that same prefab, so
	// placement, orbit radius and silhouette agree by construction.
	//
	//	planet distance   = max( radius * 2.17..2.2 ,
	//	                         play bound + radius + clearance )
	//	moon orbit        = max( planet radius * (11.5 + ring*2.5 + 0..1) ,
	//	                         planet radius + moon radius*2 + clearance )
	//
	// The ring index is the moon's ordinal around ITS OWN host (spawn
	// order), so a multi-moon system staggers into unique shells the way
	// real ones do, instead of stacking every body on one band.
	//
	// Both bands are CALIBRATED against the numbers they replace. The
	// planet band lands a stock body almost exactly where the old annulus
	// rolled (radius 9100 -> 19747..20020 against the old 19800..20000).
	// The moon band was 4.3..4.5 - a stock orbit around the settings-era
	// flat 40000, which was authored for the sector map, not a camera.
	// It doubled to 8.3..8.7 when the cameras that capture the planets
	// grew well past 40000, and went again to 11.5..12.5 when play said
	// even that sky crowded the host: a stock orbit now reads as about
	// 109200 and the shell it lives in is still empty (the play space
	// ends at 7000, neighbouring sectors carry their own space, and the
	// sector map does not draw planets at all). The bands hold for any
	// radius a prefab authors: a gas giant (~30100 measured) places
	// itself out past 65k and the floor never binds; a small body sorts
	// closer in, and its near edge (dist - radius) still clears the
	// sector's play shell (MaxUnitDistanceFromOriginUpperBound, 7000) by
	// PlanetPlacementClearance - close AND out of the shipping lanes.
	//
	// The radius is MEASURED from the class's active unit prefab
	// (MeasureVisualRadius), not authored twice anywhere - the same
	// discipline ActiveUnitPlanet.ResolvePlanetRadius applies to the ring
	// shadow, because a typed number here would be a second copy of the
	// scale waiting to disagree.
	//
	// Dice discipline: all generation dice ride the UnityEngine Random
	// stream, exactly like the stock seeder. Sizes are not dice at all -
	// a body is the size its prefab authors, every world, every seed.
	[RequireComponent(typeof(WorldSeederLayer))]
	public class CreatePlanetsSeeder : MonoBehaviour
	{
		// How far a planet's NEAR EDGE must clear the sector's play shell.
		// Measured in the same units as MaxUnitDistanceFromOriginUpperBound
		// (7000), accepted as a constant rather than a settings field until
		// a second cluster of worlds wants it different.
		public const float PlanetPlacementClearance = 1500f;

		// Slack under a moon (radius*2) inside its own orbit when the
		// proportional band would try to park a moon inside its planet.
		public const float MoonOrbitClearance = 1200f;

		// Staggered rings: every moon of a host owns its own shell, spawn
		// order inner-to-outer, so no two moons of a system ever share a
		// distance. The 0..1 per-ring roll is jitter; adjacent rings stay
		// >= 1.5 radii apart, so uniqueness survives it.
		public const float MoonRingInnerFactor = 11.5f;

		public const float MoonRingSpacing = 2.5f;

		public CreatePlanetsSeederSettings CreatePlanetsSeederSettings;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Creating planets", this, 1);
			}
			CreatePlanetsSeederSettings = world.Seeder.Settings.CreatePlanetsSeederSettings;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if ((sector.SectorType & SectorType.Planet) != 0)
				{
					CreateScenePlanets(sector);
				}
			}
		}

		private int GetCountOfPlanetClass(PlanetClass planetClass)
		{
			int num = 0;
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Planet);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.UnitClass.PlanetClass == planetClass)
					{
						num++;
					}
				}
			}
			return num;
		}

		private Unit GetBestPlanetPrefabToSpawn()
		{
			return CreatePlanetsSeederSettings.PlanetPrefabs.OrderBy((Unit e) => (float)GetCountOfPlanetClass(e.UnitClass.PlanetClass) + Random.value * 0.2f).FirstOrDefault();
		}

		public void CreateScenePlanets(Sector sector)
		{
			int num = Random.Range(CreatePlanetsSeederSettings.MinPlanets, CreatePlanetsSeederSettings.MaxPlanets + 1);
			for (int i = 0; i < num; i++)
			{
				Unit bestPlanetPrefabToSpawn = GetBestPlanetPrefabToSpawn();
				if (!bestPlanetPrefabToSpawn)
				{
					continue;
				}
				Unit unit = UnityObjectHelper.InstantiateAndGetComponent(bestPlanetPrefabToSpawn);
				unit.transform.SetParent(sector.transform, worldPositionStays: true);
				// Init before the position rolls: Seed lands ahead of the
				// dice that use it, and the placement math below has to
				// agree with the body it is placing. The size is already
				// final - ReadVisualMeasure reads the prefab hierarchy
				// and nothing rescales the instance.
				unit.GetComponent<Unit>().Init();
				float visualRadius = MeasureVisualRadius(unit);
				unit.transform.localPosition = GetRandomPlanetLocalPosition(sector, CreatePlanetsSeederSettings, visualRadius);
				float x = Random.Range(CreatePlanetsSeederSettings.MinPlanetRotationX, CreatePlanetsSeederSettings.MaxPlanetRotationX) * UnityRandomHelper.RandomSign();
				unit.GetComponent<UnitPlanet>().Rotation = new Vector3(x, Random.Range(0f, 360f), 0f);
				if (Random.value < CreatePlanetsSeederSettings.ProbabilityOfPlanetMoon)
				{
					Unit random = CreatePlanetsSeederSettings.MoonPrefabs.GetRandom();
					if (random != null)
					{
						Unit unit2 = UnityObjectHelper.InstantiateAndGetComponent(random);
						Moon component = unit2.GetComponent<Moon>();
						if (component != null)
						{
							component.OrbitingAroundUnit = unit;
							// Seed before offset: the orbit phase in
							// ActiveUnitMoon reads it, so Init lands
							// first. The two radii are read straight
							// off the prefabs, already final.
							unit2.GetComponent<Unit>().Init(autoFindParents: false);
							RandomizeMoon(component, CreatePlanetsSeederSettings, visualRadius, MeasureVisualRadius(unit2), 0);
							unit2.Sector = sector;
						}
						else
						{
							Debug.LogError("Expecting moon to have moon component", unit2.gameObject);
						}
					}
				}
			}
		}

		// Orbit radius rides the PLANET's radius, not a global constant: a
		// gas giant's moons are pushed proportionally out, a small planet's
		// moons hug it, and proportionality keeps the silhouette the same
		// whatever the bodies do.
		//
		// Rings are staggered per host: ringIndex - the moon's ordinal
		// around its own planet, in spawn order - picks the shell at
		// 11.5 + ring * 2.5 radii, plus a 0..1 radius jitter. Every moon
		// of a system keeps a unique distance; neighbours never stack on
		// one band, and ring 0 keeps the familiar 11.5..12.5 the
		// single-moon case tuned. The floor guard keeps an outsized moon
		// out of its own planet: the moon needs its diameter plus slack
		// inside the orbit rim. A planet that failed to measure (no
		// active unit prefab loaded) falls back to the settings pair,
		// which is exactly the stock flat behaviour.
		public static void RandomizeMoon(Moon moonComponent, CreatePlanetsSeederSettings settings, float planetVisualRadius, float moonVisualRadius, int ringIndex)
		{
			float num = Random.value * 360f;
			float num2 = settings.MaxMoonAngleXFromPlanet;
			float num3 = Random.Range(0f - num2, num2);
			float num4;
			if (planetVisualRadius > 0f)
			{
				float num5 = planetVisualRadius * (MoonRingInnerFactor + (float)ringIndex * MoonRingSpacing + Random.Range(0f, 1f));
				float num6 = planetVisualRadius + moonVisualRadius * 2f + MoonOrbitClearance;
				num4 = Mathf.Max(num5, num6);
			}
			else
			{
				num4 = Random.Range(settings.MinMoonDistanceFromPlanet, settings.MaxMoonDistanceFromPlanet);
			}
			moonComponent.OffsetFromPlanet = Quaternion.Euler(new Vector3(num3, num, 0f)) * Vector3.forward * num4;
		}

		// Distance from the sector centre, now a function of the planet's
		// size. The proportional band (2.17..2.2 x radius) is the stock
		// annulus expressed against the body instead of against the sky;
		// the floor keeps the near edge out of the play shell no matter
		// how small the body is. Y keeps the settings band - the whole
		// disc sits in its shallow deck and that has never been the
		// problem.
		public static Vector3 GetRandomPlanetLocalPosition(Sector sector, CreatePlanetsSeederSettings settings, float visualRadius)
		{
			float y = Random.Range(settings.MinPlanetPositionY, settings.MaxPlanetPositionY);
			Vector3 vector = Geometry.RandomXZUnitVector();
			float num = visualRadius * Random.Range(2.17f, 2.2f);
			float num2 = GameController.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginUpperBound + visualRadius + PlanetPlacementClearance;
			num = Mathf.Max(num, num2);
			Vector3 result = vector * num;
			result.y = y;
			return result;
		}

		// The radius this unit will RENDER, read from the class's active
		// unit prefab - the identical vocabulary to
		// ActiveUnitPlanet.ResolvePlanetRadius (max axis of the mesh
		// extents - SphereMediumPoly is (10,10,10), and the magnitude of
		// that vector is 17.3, which is not the radius - times the largest
		// axis of the filter's cumulative scale), taking the LARGEST of
		// every mesh in the prefab so the atmosphere shell (910, just over
		// the planet's 900) counts as the silhouette it is. The unit's own
		// localScale multiplies in last, so a logic unit whose root was
		// authored at a purposeful scale still measures true. Returns 0 when
		// nothing can be measured; callers are written to fall back to the
		// stock settings pair.
		public static float MeasureVisualRadius(Unit unit)
		{
			if (!(unit != null) || !unit.IsValidAndNotDestroyed)
			{
				return 0f;
			}
			string activeUnitResourceName = unit.GetActiveUnitResourceName();
			if (string.IsNullOrEmpty(activeUnitResourceName))
			{
				return 0f;
			}
			ActiveUnit activeUnit = GameController.Instance.ActiveUnitPrefabCache.GetActiveUnit(EngineASX.Instance, activeUnitResourceName);
			if (activeUnit == null)
			{
				return 0f;
			}
			float num = 0f;
			MeshFilter[] componentsInChildren = activeUnit.GetComponentsInChildren<MeshFilter>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Mesh sharedMesh = componentsInChildren[i].sharedMesh;
				if (sharedMesh == null)
				{
					continue;
				}
				num = Mathf.Max(num, LargestAxis(sharedMesh.bounds.extents) * LargestAxis(componentsInChildren[i].transform.lossyScale));
			}
			return num * LargestAxis(unit.transform.localScale);
		}

		private static float LargestAxis(Vector3 vector)
		{
			return Mathf.Max(vector.x, Mathf.Max(vector.y, vector.z));
		}
	}
}
