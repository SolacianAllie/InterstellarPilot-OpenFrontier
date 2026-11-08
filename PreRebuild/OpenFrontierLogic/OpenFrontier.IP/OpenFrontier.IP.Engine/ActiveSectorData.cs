using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.Engine.Core.Units.ActiveUnits;
using OpenFrontier.IP.Misc;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine
{
	public class ActiveSectorData : MonoBehaviour
	{
		public GameObject BackgroundObjectsRoot;

		public string BackgroundMusicResourceName;

		private EngineASX engine;

		public GameObject SpaceFog;

		// Open Frontier: materials for the runtime-built visible sun (crisp
		// core + soft glow). See SunBillboard.
		public Material SunCoreMaterial;

		public Material SunGlowMaterial;

		private List<ParticleSystem> spaceFogParticleSystems = new List<ParticleSystem>(3);

		public void Init(EngineASX engine)
		{
			this.engine = engine;
			if (SpaceFog != null)
			{
				SpaceFog.gameObject.SetActive(value: false);
			}
			spaceFogParticleSystems = SpaceFog.GetComponentsInChildren<ParticleSystem>().ToList();
			if (BackgroundObjectsRoot == null)
			{
				GameObject gameObject = new GameObject();
				gameObject.transform.SetParent(transform, worldPositionStays: false);
				gameObject.transform.localPosition = Vector3.zero;
				BackgroundObjectsRoot = gameObject;
			}
			EnsureSunBillboard();
			EnsureSpaceReflectionProbe();
		}

		public void OnSectorChanged()
		{
			UnityObjectHelper.DestroyChildren(BackgroundObjectsRoot.transform, destroyImmediate: true);
			ClearParticleSystems();
			if (EngineASX.Instance.ActiveSector != null)
			{
				EngineASX.Instance.ActiveSector.GenerateSpaceBackground();
				TryCreateBackgroundObjects();
			}
			EnsureSunBillboard();
			EnsureSpaceReflectionProbe();
			// Capture the new sector's backdrop immediately rather than
			// waiting out the 1s render interval.
			if (SpaceReflectionProbe.Instance != null)
			{
				SpaceReflectionProbe.Instance.RequestRender();
			}
		}

		// Open Frontier: spawn the space reflection probe (once) under the
		// EngineASX root - ships reflect the starfield/sun/distant planets.
		private void EnsureSpaceReflectionProbe()
		{
			EngineASX engineASX = (engine != null) ? engine : EngineASX.Instance;
			if (engineASX == null || engineASX.GetComponentInChildren<SpaceReflectionProbe>() != null)
			{
				return;
			}
			GameObject gameObject = new GameObject("SpaceReflectionProbe");
			gameObject.transform.SetParent(engineASX.transform, worldPositionStays: false);
			gameObject.AddComponent<SpaceReflectionProbe>();
		}

		// Open Frontier: give the sector's directional light a visible sun.
		// (The light is a sibling of this object under EngineASX, not a
		// child, so FindFirstChildDirectionalLight can't find it - look at
		// scene lights instead. Scene light COLOR is star-tinted inside
		// GasCloudController.GetDesiredDirectionLightColor, which drives the
		// light color every frame - tinting it here would get stomped.)
		private void EnsureSunBillboard()
		{
			EngineASX engineASX = (engine != null) ? engine : EngineASX.Instance;
			if (engineASX == null)
			{
				return;
			}
			Light[] componentsInChildren = engineASX.GetComponentsInChildren<Light>();
			foreach (Light light in componentsInChildren)
			{
				if (light.type == LightType.Directional)
				{
					SunBillboard sunBillboard = light.GetComponent<SunBillboard>();
					if (sunBillboard == null)
					{
						sunBillboard = light.gameObject.AddComponent<SunBillboard>();
					}
					sunBillboard.CoreMaterial = SunCoreMaterial;
					sunBillboard.GlowMaterial = SunGlowMaterial;
					// ResolveCurrent: a StarColorMarker under the sector's
					// object wins, then a scene-wide marker, then the sector's
					// DirectionLightColor (author-painted or seeded at
					// creation), otherwise the deterministic per-sector color.
					sunBillboard.StarTint = StarColorGenerator.ResolveCurrent((engineASX.ActiveSector != null) ? engineASX.ActiveSector.gameObject : null, (engineASX.ActiveSector != null) ? engineASX.ActiveSector.DirectionLightColor : Color.white, (engineASX.ActiveSector != null) ? engineASX.ActiveSector.UniqueId : 0);
					break;
				}
			}
		}

		private void TryCreateBackgroundObjects()
		{
			if (engine.ActiveSector != null)
			{
				BackgroundObjectsRoot.transform.position = engine.ActiveSector.transform.position;
				CreateAsteroidClusterBackgroundObjects();
				if (GameController.Instance.GameSettings.BackgroundObjectsSettings.Enabled)
				{
					CreateDistantBackgroundObjects();
				}
			}
			else
			{
				Debug.LogError("ActiveSceneData: expecting engine to have ActiveScene", this);
			}
		}

		private void CreateDistantBackgroundObjects()
		{
			HashSet<int> hashSet = new HashSet<int>();
			Queue<(Sector, int)> queue = new Queue<(Sector, int)>();
			queue.Enqueue((EngineASX.Instance.ActiveSector, 0));
			hashSet.Add(engine.ActiveSector.UniqueId);
			int num = Mathf.Max(GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundObjectPlanetMaxJumpDistance, GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundObjectAsteroidClusterMaxJumpDistance);
			while (queue.Count > 0)
			{
				var (sector, num2) = queue.Dequeue();
				if (sector != EngineASX.Instance.ActiveSector)
				{
					CreateDistantBackgroundObjectsForSector(sector, num2);
				}
				foreach (SectorNeighbour neighbour in sector.Neighbours)
				{
					if (!neighbour.ConnectingGate.IsUnstable && num2 <= num && !hashSet.Contains(neighbour.Sector.UniqueId))
					{
						hashSet.Add(neighbour.Sector.UniqueId);
						queue.Enqueue((neighbour.Sector, num2 + 1));
					}
				}
			}
		}

		private void CreateDistantBackgroundObjectsForSector(Sector neighbourSector, int jumpDistance)
		{
			if (jumpDistance <= GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundObjectPlanetMaxJumpDistance)
			{
				List<Unit> unitsByType = neighbourSector.GetUnitsByType(UnitType.Planet);
				if (unitsByType != null)
				{
					foreach (Unit item in unitsByType)
					{
						if (item.GetComponent<UnitPlanet>().IsMoon && !GameController.Instance.GameSettings.BackgroundObjectsSettings.CreateDistantMoons)
						{
							continue;
						}
						Vector3 distantCelestialBodyRenderPosition = GetDistantCelestialBodyRenderPosition(neighbourSector, item.SectorPosition);
						if (!ShouldDrawDistantCelestialBody(distantCelestialBodyRenderPosition))
						{
							continue;
						}
						ActiveUnit activeUnit = EngineASX.Instance.InstantiateActiveUnit(item.UnitClass);
						if (!(activeUnit != null))
						{
							continue;
						}
						float scaleMultiplier = GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundPlanetScale * GameController.Instance.GameSettings.BackgroundObjectsSettings.GeneralBackgroundObjectScale;
						DrawDistantCelestialBody(distantCelestialBodyRenderPosition, activeUnit, scaleMultiplier);
						if (!GameController.Instance.GameSettings.VideoSettings.RenderAtmospheresOnDistantPlanets)
						{
							ActiveUnitPlanet component = activeUnit.GetComponent<ActiveUnitPlanet>();
							if (component != null && component.AtmoshphereRenderer != null)
							{
								component.AtmoshphereRenderer.enabled = false;
							}
						}
					}
				}
			}
			if (!GameController.Instance.GameSettings.BackgroundObjectsSettings.CreateDistanceAsteroidFields || jumpDistance > GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundObjectAsteroidClusterMaxJumpDistance)
			{
				return;
			}
			List<Unit> unitsByType2 = neighbourSector.GetUnitsByType(UnitType.AsteroidCluster);
			if (unitsByType2 == null)
			{
				return;
			}
			foreach (Unit item2 in unitsByType2)
			{
				if (!(item2.UnitClass.DisplayData != null) || string.IsNullOrEmpty(item2.UnitClass.DisplayData.ActiveUnitClassNameDistant))
				{
					continue;
				}
				Vector3 distantCelestialBodyRenderPosition2 = GetDistantCelestialBodyRenderPosition(neighbourSector, item2.SectorPosition);
				if (!ShouldDrawDistantCelestialBody(distantCelestialBodyRenderPosition2))
				{
					continue;
				}
				ActiveUnit activeUnit2 = EngineASX.LoadAndInstantiateActiveUnit(item2.UnitClass.DisplayData.ActiveUnitClassNameDistant);
				if (activeUnit2 != null)
				{
					float num = item2.Radius * GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundAsteroidClusterScaleMultiplier * GameController.Instance.GameSettings.BackgroundObjectsSettings.GeneralBackgroundObjectScale;
					DrawDistantCelestialBody(distantCelestialBodyRenderPosition2, activeUnit2, num);
					Vector3 eulerAngles = FaceCamera.GetRotation(activeUnit2.transform.position, Vector3.zero).eulerAngles;
					eulerAngles.z = (float)item2.Seed % 360f;
					activeUnit2.transform.rotation = Quaternion.Euler(eulerAngles);
					float num2 = num * GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundAsteroidClusterRandomYPositionMultiplier;
					if (num2 > 0f)
					{
						float num3 = (float)item2.Seed % num2;
						activeUnit2.transform.localPosition += Vector3.up * ((0f - num2) / 2f + num3);
					}
				}
			}
		}

		private void DrawDistantCelestialBody(Vector3 position, ActiveUnit activeUnit, float scaleMultiplier = 1f)
		{
			activeUnit.transform.SetParent(BackgroundObjectsRoot.transform);
			activeUnit.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
			activeUnit.transform.localPosition = position;
		}

		private bool ShouldDrawDistantCelestialBody(Vector3 position)
		{
			return position.magnitude < GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundObjectMaxRenderDistance;
		}

		private Vector3 GetDistantCelestialBodyRenderPosition(Sector sector, Vector3 sectorPosition)
		{
			Vector3 value = sector.MapPosition - EngineASX.Instance.ActiveSector.MapPosition;
			return Vector3.Normalize(value) * value.magnitude * GameController.Instance.GameSettings.BackgroundObjectsSettings.BackgroundPlanetMapDistanceToActualDistance + sectorPosition * GameController.Instance.GameSettings.BackgroundObjectsSettings.BackPlanetUnitPositionScaleFactor;
		}

		private Light FindFirstChildDirectionalLight()
		{
			Light[] componentsInChildren = GetComponentsInChildren<Light>();
			foreach (Light light in componentsInChildren)
			{
				if (light.type == LightType.Directional)
				{
					return light;
				}
			}
			return null;
		}

		private void CreateAsteroidClusterBackgroundObjects()
		{
			List<Unit> unitsByType = engine.ActiveSector.GetUnitsByType(UnitType.AsteroidCluster);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item in unitsByType)
			{
				AsteroidCluster component = item.GetComponent<AsteroidCluster>();
				System.Random random = new System.Random(item.Seed + 1646423);
				AsteroidFieldInstantiator.Instantiate(component, component.transform.position, BackgroundObjectsRoot.transform, component.AsteroidType.AsteroidFieldPrefabSettings, GameController.Instance.GameSettings.DefaultAsteroidPlacementSettings, random);
			}
		}

		public void StopParticleSystems()
		{
			if (!(SpaceFog != null))
			{
				return;
			}
			foreach (ParticleSystem spaceFogParticleSystem in spaceFogParticleSystems)
			{
				spaceFogParticleSystem.Stop();
			}
		}

		public void ClearParticleSystems()
		{
			if (!(SpaceFog != null))
			{
				return;
			}
			foreach (ParticleSystem spaceFogParticleSystem in spaceFogParticleSystems)
			{
				spaceFogParticleSystem.Stop();
				spaceFogParticleSystem.Clear();
			}
			SpaceFog.gameObject.SetActive(value: false);
		}

		public void PlayParticleSystems(bool prewarm)
		{
			if (!(SpaceFog != null))
			{
				return;
			}
			SpaceFog.gameObject.transform.position = GameController.Instance.MainCamera.transform.position;
			SpaceFog.gameObject.SetActive(value: true);
			foreach (ParticleSystem spaceFogParticleSystem in spaceFogParticleSystems)
			{
				if (prewarm)
				{
					spaceFogParticleSystem.Clear();
					spaceFogParticleSystem.Simulate(5f, withChildren: true, restart: false);
				}
				spaceFogParticleSystem.Play();
			}
		}
	}
}
