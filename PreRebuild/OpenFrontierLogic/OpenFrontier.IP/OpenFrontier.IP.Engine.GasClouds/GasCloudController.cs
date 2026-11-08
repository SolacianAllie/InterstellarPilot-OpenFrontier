using System.Collections.Generic;
using OpenFrontier.IP.Engine.ActiveUnitFx;
using UnityEngine;

namespace OpenFrontier.IP.Engine.GasClouds
{
	public class GasCloudController : MonoBehaviour
	{
		public bool HasDefaultFogColor;

		public Color DefaultFogColor = Color.white;

		public float DefaultFogStart = 1000f;

		public float DefaultFogEnd = 4000f;

		public bool DefaultFogOn;

		public float TintBlendRate = 1f;

		public float FogBlendRate = 1f;

		private ActiveUnitGasCloud currentActiveUnitGasCloud;

		private float currentTintBlend;

		private float currentFogBlend;

		private ActiveGasCloud activeGasCloud;

		private Color skyPrimaryTint = Color.white;

		private float skyPrimaryExposure = 1f;

		private Color? skyOverlayColor;

		private float skySecondaryExposure = 1f;

		private bool fogDesiredOn;

		private float fogDesiredStart;

		private float fogDesiredEnd;

		private Color fogDesiredColor;

		public bool UseRealTime = true;

		public ActiveUnitGasCloud CurrentGasCloud
		{
			get
			{
				return currentActiveUnitGasCloud;
			}
			set
			{
				currentActiveUnitGasCloud = value;
			}
		}

		public Color? SkyOverlayColor
		{
			get
			{
				return skyOverlayColor;
			}
			set
			{
				if (skyOverlayColor != value)
				{
					skyOverlayColor = value;
				}
			}
		}

		public bool FogDesiredOn
		{
			get
			{
				return fogDesiredOn;
			}
			set
			{
				fogDesiredOn = value;
			}
		}

		public Color FogDesiredColor
		{
			get
			{
				return fogDesiredColor;
			}
			set
			{
				fogDesiredColor = value;
			}
		}

		public float FogDesiredStart
		{
			get
			{
				return fogDesiredStart;
			}
			set
			{
				fogDesiredEnd = value;
			}
		}

		public float FogDesiredEnd
		{
			get
			{
				return fogDesiredEnd;
			}
			set
			{
				fogDesiredEnd = value;
			}
		}

		public float SkySecondaryExposure
		{
			get
			{
				return skySecondaryExposure;
			}
			set
			{
				skySecondaryExposure = value;
			}
		}

		internal void SetBlend(float blend)
		{
			currentTintBlend = blend;
			currentFogBlend = blend;
			ApplyBlend();
		}

		public float GetDeltaTime()
		{
			if (UseRealTime)
			{
				return RealTime.deltaTime;
			}
			return Time.deltaTime;
		}

		public void MoveToBlend(float desired)
		{
			currentTintBlend = Mathf.MoveTowards(currentTintBlend, desired, GetDeltaTime() * TintBlendRate);
			currentFogBlend = Mathf.MoveTowards(currentFogBlend, desired, GetDeltaTime() * FogBlendRate);
			ApplyBlend();
		}

		private void ApplyBlend()
		{
			GameController.Instance.SkyboxCamera.SkyOverlayRenderer.gameObject.SetActive(skyOverlayColor.HasValue);
			if (skyOverlayColor.HasValue)
			{
				Color value = skyOverlayColor.Value;
				GameController.Instance.SkyboxCamera.SetSkyOverlayColor(Color.Lerp(new Color(value.r, value.g, value.b, 0f), value, currentTintBlend));
			}
			float b = skyPrimaryExposure * skySecondaryExposure;
			GameController.Instance.SkyboxCamera.SetExposure(Mathf.Lerp(skyPrimaryExposure, b, currentTintBlend));
			GameController.Instance.SkyboxCamera.SetTint(skyPrimaryTint);
			ApplyFogBlend();
		}

		private void ApplyFogBlend()
		{
			RenderSettings.fogStartDistance = Mathf.Lerp(DefaultFogStart, fogDesiredStart, currentFogBlend);
			RenderSettings.fogEndDistance = Mathf.Lerp(DefaultFogEnd, fogDesiredEnd, currentFogBlend);
			if (currentFogBlend > 0f)
			{
				RenderSettings.fog = fogDesiredOn;
				RenderSettings.fogColor = fogDesiredColor;
			}
			else
			{
				RenderSettings.fog = DefaultFogOn;
			}
		}

		public void Init()
		{
			EngineASX.Instance.CameraMoved += engine_CameraMoved;
			RenderSettings.fogMode = FogMode.Linear;
		}

		private void engine_CameraMoved(EngineASX sender)
		{
			UpdateImmediate();
		}

		public void UpdateImmediate()
		{
			ActiveUnitGasCloud cameraGasCloud = GetCameraGasCloud();
			SetNewGasCloudImmediate(cameraGasCloud);
			ApplyEnvironmentalSettingsImmediate();
		}

		private void ApplyEnvironmentalSettingsImmediate()
		{
			Color? desiredAmbientLight = GetDesiredAmbientLight();
			if (desiredAmbientLight.HasValue)
			{
				RenderSettings.ambientLight = desiredAmbientLight.Value;
			}
			if (EngineASX.Instance.ActiveSectorData != null)
			{
				Color? desiredDirectionLightColor = GetDesiredDirectionLightColor();
				if (desiredDirectionLightColor.HasValue)
				{
					EngineASX.Instance.DirectionalLight.color = desiredDirectionLightColor.Value;
					EngineASX.Instance.DirectionalLight.intensity = GetDesiredDirectionLightIntensity();
				}
			}
		}

		// Open Frontier: a giant gas cloud blocks starlight - inside one the
		// directional light drops to this fraction of its base intensity.
		private const float CloudDirectionalLightBlock = 0.2f;

		// Open Frontier: the light's CURRENT intensity as a fraction of its
		// undimmed base (lerped 0.2 inside clouds). SunBillboard dims its
		// quads by the same factor so the visible sun breathes with the
		// starlight the cloud blocks.
		public float CurrentDirectionalLightFactor
		{
			get
			{
				if (baseDirectionalLightIntensity <= 0f || EngineASX.Instance == null || EngineASX.Instance.DirectionalLight == null)
				{
					return 1f;
				}
				return EngineASX.Instance.DirectionalLight.intensity / baseDirectionalLightIntensity;
			}
		}

		private float baseDirectionalLightIntensity = -1f;

		private float GetDesiredDirectionLightIntensity()
		{
			if (baseDirectionalLightIntensity < 0f)
			{
				baseDirectionalLightIntensity = EngineASX.Instance.DirectionalLight.intensity;
			}
			return baseDirectionalLightIntensity * ((activeGasCloud != null) ? CloudDirectionalLightBlock : 1f);
		}

		public void Update()
		{
			if (!EngineASX.LoadedAndReady || !(EngineASX.Instance.ActiveSectorData != null))
			{
				return;
			}
			if (EngineASX.Instance.ActiveSector != null)
			{
				skyPrimaryExposure = EngineASX.Instance.ActiveSector.SkyExposure;
				skyPrimaryTint = EngineASX.Instance.ActiveSector.SkyTintColor;
			}
			if (EngineASX.Instance.ActiveSectorData.SpaceFog != null)
			{
				EngineASX.Instance.ActiveSectorData.SpaceFog.transform.position = GameController.Instance.MainCamera.transform.position;
			}
			ActiveUnitGasCloud cameraGasCloud = GetCameraGasCloud();
			// Open Frontier: inside ANY cloud, hide every cloud billboard.
			GasCloudBilboardController.BillboardsHidden = cameraGasCloud != null;
			if (cameraGasCloud != currentActiveUnitGasCloud)
			{
				SetNewGasCloud(cameraGasCloud);
			}
			if (activeGasCloud != null)
			{
				skySecondaryExposure = activeGasCloud.GasCloudData.SkyExposure;
				SkyOverlayColor = activeGasCloud.GasCloudData.FogColor; // Open Frontier: sky matches fog color
				fogDesiredStart = activeGasCloud.GasCloudData.FogStartDistance;
				fogDesiredEnd = activeGasCloud.GasCloudData.FogEndDistance;
				fogDesiredColor = activeGasCloud.GasCloudData.FogColor;
				if (EngineASX.Instance.ActiveSectorData.SpaceFog != null)
				{
					EngineASX.Instance.ActiveSectorData.StopParticleSystems();
				}
				activeGasCloud.PositionParticleSystems(GameController.Instance.MainCamera.transform.position);
			}
			float desired = ((currentActiveUnitGasCloud != null) ? 1f : 0f);
			MoveToBlend(desired);
			Color? desiredAmbientLight = GetDesiredAmbientLight();
			if (desiredAmbientLight.HasValue)
			{
				RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, desiredAmbientLight.Value, GetDeltaTime());
			}
			if (EngineASX.Instance.DirectionalLight != null && EngineASX.Instance.ActiveSector != null)
			{
				// Open Frontier: the day-cycle time term is removed - the light
				// keeps each sector's static LightDirectionFudge angle but no
				// longer rotates over time. With a visible sun tracking the
				// light (and shadows cast from it), the rotation read as the
				// sun sliding across the sky and made shadows shimmer.
				double num2 = EngineASX.Instance.ActiveSector.LightDirectionFudge;
				float x = EngineASX.Instance.DirectionalLight.transform.localRotation.eulerAngles.x;
				EngineASX.Instance.DirectionalLight.transform.localRotation = Quaternion.Euler(x, (float)num2 * 360f, 0f);
				Color? desiredDirectionLightColor = GetDesiredDirectionLightColor();
				if (desiredDirectionLightColor.HasValue)
				{
					// Open Frontier: color snaps (matches the sun, which
					// changes instantly); intensity still fades smoothly
					// for the gas cloud light-blocking.
					EngineASX.Instance.DirectionalLight.color = desiredDirectionLightColor.Value;
					EngineASX.Instance.DirectionalLight.intensity = Mathf.Lerp(EngineASX.Instance.DirectionalLight.intensity, GetDesiredDirectionLightIntensity(), GetDeltaTime());
				}
			}
		}

		private Color? GetDesiredDirectionLightColor()
		{
			// (Open Frontier note: no star-color multiplication here anymore -
			// DirectionLightColor IS the star color now, seeded into the
			// sector at universe creation by SectorCreator.)
			if (activeGasCloud != null)
			{
				return activeGasCloud.GasCloudData.DirectionLightColor;
			}
			if (EngineASX.Instance.ActiveSector != null)
			{
				return EngineASX.Instance.ActiveSector.DirectionLightColor;
			}
			return null;
		}

		private bool GetDesiredFogOn()
		{
			if (activeGasCloud != null)
			{
				return activeGasCloud.GasCloudData.FogOn;
			}
			return DefaultFogOn;
		}

		private float GetDesiredFogStart()
		{
			if (activeGasCloud != null)
			{
				return activeGasCloud.GasCloudData.FogStartDistance;
			}
			return DefaultFogStart;
		}

		private float GetDesiredFogEnd()
		{
			if (activeGasCloud != null)
			{
				return activeGasCloud.GasCloudData.FogEndDistance;
			}
			return DefaultFogEnd;
		}

		private Color? GetDesiredFogColor()
		{
			if (activeGasCloud != null)
			{
				return activeGasCloud.GasCloudData.FogColor;
			}
			if (HasDefaultFogColor)
			{
				return DefaultFogColor;
			}
			return null;
		}

		private Color? GetDesiredAmbientLight()
		{
			if (activeGasCloud != null)
			{
				return activeGasCloud.GasCloudData.AmbientLightColor;
			}
			if (EngineASX.Instance.ActiveSector != null)
			{
				return EngineASX.Instance.ActiveSector.AmbientLightColor;
			}
			return null;
		}

		private void SetNewGasCloudImmediate(ActiveUnitGasCloud newGasCloud)
		{
			GasCloudData gasCloudData = null;
			if (activeGasCloud != null)
			{
				gasCloudData = activeGasCloud.GasCloudData;
			}
			currentActiveUnitGasCloud = newGasCloud;
			GasCloudData gasCloudData2 = null;
			if (currentActiveUnitGasCloud != null)
			{
				gasCloudData2 = currentActiveUnitGasCloud.ActiveGasCloudPrefab.GasCloudData;
			}
			if (gasCloudData != gasCloudData2)
			{
				if (activeGasCloud != null)
				{
					Object.Destroy(activeGasCloud.gameObject);
					activeGasCloud = null;
				}
				if (currentActiveUnitGasCloud != null)
				{
					if (EngineASX.Instance.ActiveSectorData != null)
					{
						EngineASX.Instance.ActiveSectorData.ClearParticleSystems();
					}
					activeGasCloud = UnityObjectHelper.InstantiateAndGetComponent(currentActiveUnitGasCloud.ActiveGasCloudPrefab, currentActiveUnitGasCloud.gameObject.transform);
				}
			}
			currentTintBlend = ((activeGasCloud != null) ? 1f : 0f);
			currentFogBlend = currentTintBlend;
			ApplyBlend();
			if (activeGasCloud != null)
			{
				activeGasCloud.PhaseInCompletely();
			}
			else if (EngineASX.Instance.ActiveSector != null && EngineASX.Instance.ActiveSectorData != null)
			{
				EngineASX.Instance.ActiveSectorData.PlayParticleSystems(prewarm: true);
			}
		}

		private void SetNewGasCloud(ActiveUnitGasCloud newGasCloud)
		{
			GasCloudData gasCloudData = null;
			if (activeGasCloud != null)
			{
				gasCloudData = activeGasCloud.GasCloudData;
			}
			currentActiveUnitGasCloud = newGasCloud;
			GasCloudData gasCloudData2 = null;
			if (currentActiveUnitGasCloud != null)
			{
				gasCloudData2 = currentActiveUnitGasCloud.ActiveGasCloudPrefab.GasCloudData;
			}
			if (!(gasCloudData != gasCloudData2))
			{
				return;
			}
			if (activeGasCloud != null)
			{
				activeGasCloud.StartPhaseOut();
				activeGasCloud = null;
			}
			if (currentActiveUnitGasCloud != null)
			{
				if (EngineASX.Instance.ActiveSectorData != null)
				{
					EngineASX.Instance.ActiveSectorData.StopParticleSystems();
				}
				activeGasCloud = UnityObjectHelper.InstantiateAndGetComponent(currentActiveUnitGasCloud.ActiveGasCloudPrefab, currentActiveUnitGasCloud.gameObject.transform);
				activeGasCloud.PhaseIn();
			}
			else
			{
				EngineASX.Instance.ActiveSectorData.PlayParticleSystems(prewarm: false);
			}
		}

		public void ClearGasCloud()
		{
			CurrentGasCloud = null;
			if (activeGasCloud != null)
			{
				Object.Destroy(activeGasCloud.gameObject);
			}
			ApplyBlend();
			ClearSecondaryEnvironmentParameters();
		}

		private void ClearSecondaryEnvironmentParameters()
		{
			skySecondaryExposure = skyPrimaryExposure;
			SkyOverlayColor = null;
		}

		private ActiveUnitGasCloud GetCameraGasCloud()
		{
			Sector activeSector = EngineASX.Instance.ActiveSector;
			if (activeSector != null && EngineASX.Instance.ActiveSectorData != null)
			{
				Vector3 position = GameController.Instance.MainCamera.transform.position;
				Unit unit = null;
				if (currentActiveUnitGasCloud != null)
				{
					unit = currentActiveUnitGasCloud.Unit;
				}
				List<Unit> unitsByType = activeSector.GetUnitsByType(UnitType.GasCloud);
				if (unitsByType != null)
				{
					float num = 0f;
					Unit unit2 = null;
					foreach (Unit item in unitsByType)
					{
						float num2 = item.Radius;
						if (item == unit)
						{
							num2 += 50f;
						}
						float num3 = Vector3.Distance(item.transform.position, position);
						if (num3 < num2 && (unit2 == null || num3 < num))
						{
							unit2 = item;
							num = num3;
						}
					}
					if (unit2 != null)
					{
						if (unit2.ActiveUnit == null)
						{
							Debug.LogError("Expecting gas cloud to have an active unit", unit2);
							return null;
						}
						return unit2.ActiveUnit.GetComponent<ActiveUnitGasCloud>();
					}
				}
			}
			return null;
		}
	}
}
