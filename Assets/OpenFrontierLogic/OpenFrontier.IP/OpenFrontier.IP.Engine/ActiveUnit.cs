using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.ActiveUnitFx;
using OpenFrontier.IP.Engine.AmbientSounds;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Settings;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnit : MonoBehaviour
	{
		public delegate void ActiveUnitDrawDistanceChanged(ActiveUnit sender);

		public Color CloakDrawColor = Color.white;

		private bool shouldEffectsBeDrawn;

		private ActiveUnitRenderMode activeUnitRenderMode;

		private const float cameraDistanceCheckFrequency = 2f;

		private static List<UnitDamageParticlesInfo> inactiveParticleCache = new List<UnitDamageParticlesInfo>(8);

		public ActiveUnitClass ActiveUnitClass;

		private List<AmbientSound> ambientSounds = new List<AmbientSound>();

		public GameObject CommsDialogPositioner;

		private List<UnitDamageParticlesInfo> damageParticlesInfo = new List<UnitDamageParticlesInfo>();

		[NonSerialized]
		public AnimatedUnitExplosion DestructionAnimGameObject;

		public List<GameObject> DrawRangeObjects;

		private bool inAudioRange;

		private bool inDrawRange;

		private float lastDistanceFromCamera = float.MaxValue;

		private float nextCameraDistanceChk;

		private float nextDamageParticlesGen = 0.1f;

		private Unit unit;

		[FormerlySerializedAs("CloakedRenderer")]
		public MeshRenderer CloakOutlineRenderer;

		public List<MeshRenderer> CloakOutlineRenderers;

		public MeshRenderer UnitRenderer;

		public List<MeshRenderer> UnitRenderers;

		public MeshRenderer CloakRenderer;

		public List<MeshRenderer> CloakRenderers;

		private MaterialPropertyBlock cloakMaterialPropertyBlock;

		public bool ApplyTeamColours;

		public bool LevelOfDetail;

		[SerializeField]
		private List<ActiveUnitThrusterAudio> activeUnitThrusterAudios = new List<ActiveUnitThrusterAudio>();

		private Color lastAppliedOpaqueColor = Color.white;

		private float lastFadeAlpha = -1f;

		public bool IsInDrawRange => inDrawRange;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				if (unit != value)
				{
					unit = value;
					if (unit != null)
					{
						unit.ActiveUnit = this;
					}
				}
			}
		}

		public EngineASX Engine => unit.Engine;

		public Rigidbody UnitRigidBody
		{
			get
			{
				if (unit != null)
				{
					return unit.RBody;
				}
				return null;
			}
		}

		public List<UnitDamageParticlesInfo> DamageParticlesInfo => damageParticlesInfo;

		public float LastDistanceFromCamera => lastDistanceFromCamera;

		public ActiveUnitShip ActiveUnitShip { get; set; }

		public bool ShouldEffectsBeDrawn => shouldEffectsBeDrawn;

		public ActiveUnitRenderMode ActiveUnitRenderMode
		{
			get
			{
				return activeUnitRenderMode;
			}
			set
			{
				if (activeUnitRenderMode != value)
				{
					activeUnitRenderMode = value;
					ApplyRenderMode();
				}
			}
		}

		public event ActiveUnitDrawDistanceChanged DrawDistanceChangedImmediate;

		public void Awake()
		{
			if (LevelOfDetail)
			{
				if (CloakRenderers.Count > 0)
				{
					cloakMaterialPropertyBlock = new MaterialPropertyBlock();
				}
			}
			else if (CloakRenderer != null)
			{
				cloakMaterialPropertyBlock = new MaterialPropertyBlock();
			}
			SetCloakOutlineRendererEnabled(enabled: false);
			ApplyRenderMode();
		}

		private void SetCloakOutlineRendererEnabled(bool enabled)
		{
			if (CloakOutlineRenderer != null)
			{
				CloakOutlineRenderer.enabled = enabled;
			}
			else
			{
				if (CloakOutlineRenderers.Count <= 0)
				{
					return;
				}
				foreach (MeshRenderer cloakOutlineRenderer in CloakOutlineRenderers)
				{
					cloakOutlineRenderer.enabled = enabled;
				}
			}
		}

		public void Init()
		{
			if (unit == null)
			{
				Debug.LogError("Must set unit before initialising ActiveUnit", this);
			}
			SetDrawObjectsActive();
			if (TryGetComponent<ActiveUnitShip>(out var component))
			{
				ActiveUnitShip = component;
				ActiveUnitShip.Init();
			}
			foreach (ActiveUnitThrusterAudio activeUnitThrusterAudio in activeUnitThrusterAudios)
			{
				if (activeUnitThrusterAudio != null)
				{
					activeUnitThrusterAudio.Init(this);
				}
			}
			ApplyRenderMode();
		}

		private void Update()
		{
			if (!(unit != null))
			{
				return;
			}
			if (Time.realtimeSinceStartup > nextCameraDistanceChk && GameController.Instance.MainCamera != null)
			{
				UpdateVisibility(immediate: false);
			}
			if (inDrawRange)
			{
				UpdateRenderMode();
				switch (activeUnitRenderMode)
				{
				case ActiveUnitRenderMode.Opaque:
					UpdateOpaqueMaterial(unit.Faction);
					break;
				case ActiveUnitRenderMode.Fade:
					UpdateFadeMaterial();
					break;
				}
			}
		}

		internal Vector3 GetRandomHullTargetLocalPosition()
		{
			if (unit.Components != null)
			{
				return unit.Components.Bays.GetRandom().transform.localPosition;
			}
			return UnityEngine.Random.rotation * Vector3.forward * UnityEngine.Random.Range(0f, unit.UnitClass.DisplayData.Radius * 0.5f);
		}

		public void UpdateOpaqueMaterial(Faction owner)
		{
		}

		public void UpdateFadeForConstructionProgressValue(float constructionProgress, Faction owner)
		{
			VideoSettings videoSettings = GameController.Instance.GameSettings.VideoSettings;
			float alpha = Mathf.Lerp(videoSettings.StationUnderConstructionMinAlpha, videoSettings.StationUnderConstructionMaxAlpha, (Mathf.Sin(constructionProgress * videoSettings.StationUnderConstructionAlphaFadeRate) + 1f) / 2f);
			SetFadeAlpha(alpha, owner);
		}

		public void SetFadeAlpha(float alpha, Faction owner)
		{
			if (LevelOfDetail)
			{
				if (lastFadeAlpha == alpha)
				{
					return;
				}
				Color color = CalculateCloakColor(alpha, owner);
				foreach (MeshRenderer cloakRenderer in CloakRenderers)
				{
					ApplyCloakColorMaterialPropertyBlock(cloakRenderer, ref color);
				}
				lastFadeAlpha = alpha;
			}
			else if (CloakRenderer != null)
			{
				Color color2 = CalculateCloakColor(alpha, owner);
				ApplyCloakColorMaterialPropertyBlock(CloakRenderer, ref color2);
			}
		}

		private Color CalculateCloakColor(float alpha, Faction owner)
		{
			Color cloakDrawColor = CloakDrawColor;
			cloakDrawColor.a = alpha;
			return cloakDrawColor;
		}

		private void ApplyCloakColorMaterialPropertyBlock(MeshRenderer meshRenderer, ref Color color)
		{
			meshRenderer.GetPropertyBlock(cloakMaterialPropertyBlock);
			cloakMaterialPropertyBlock.SetColor("_BaseColor", color); // URP port: _Color -> _BaseColor
			meshRenderer.SetPropertyBlock(cloakMaterialPropertyBlock);
		}

		public void UpdateRenderMode()
		{
			ActiveUnitRenderMode = ((!unit.IsFullyDecloaked || unit.IsUnderConstructionOrDismantling) ? ActiveUnitRenderMode.Fade : ActiveUnitRenderMode.Opaque);
		}

		private void UpdateFadeMaterial()
		{
			if (CloakOutlineRenderer != null || CloakOutlineRenderers.Count > 0)
			{
				SetCloakOutlineRendererEnabled(GameController.Instance.RenderCloakedShipOutlinesEnabled && unit.CloakState == CloakState.Cloaked && unit.IsPlayerOrAllied() && (unit.IsPlayerCurrentUnit || lastDistanceFromCamera < 1500f));
			}
			if (unit.IsUnderConstructionOrDismantling)
			{
				UpdateFadeForConstructionProgressValue(unit.Components.ConstructionProgress * unit.UnitClass.maxHealth, Unit.Faction);
			}
			else if (!unit.IsFullyDecloaked)
			{
				CloakComponent cloakComponent = unit.Components.CloakComponent;
				float alpha = Mathf.Lerp(GameController.Instance.GameSettings.VideoSettings.CloakMaxAlpha, cloakComponent.GetCloakMinAlpha(), Mathf.Pow(cloakComponent.CloakProgress, GameController.Instance.GameSettings.VideoSettings.CloakAlphaPower));
				SetFadeAlpha(alpha, Unit.Faction);
			}
		}

		public void UpdateVisibility(bool immediate)
		{
			shouldEffectsBeDrawn = GetShouldEffectsBeDrawn();
			nextCameraDistanceChk = Time.realtimeSinceStartup + 2f;
			UpdateCameraDistance();
			UpdateInCameraRangeChanged();
			UpdateAdaptiveCollisionColliders(lastDistanceFromCamera);
			if (immediate && DrawDistanceChangedImmediate != null)
			{
				DrawDistanceChangedImmediate(this);
			}
		}

		private void UpdateAdaptiveCollisionColliders(float cameraDistance)
		{
			if (unit.UnitClass.DisplayData.NormalCollisionDisabled)
			{
				return;
			}
			PerformanceSettings performanceSettings = GameController.Instance.GameSettings.PerformanceSettings;
			if (performanceSettings.DisableCollidersInActiveSector && unit.CollisionCollider != null)
			{
				bool flag = GameController.Instance.GameSettings.DebugSettings.CollisionEnabled && Time.timeScale <= 1f;
				bool collisionColliderEnabled = unit.GetCollisionColliderEnabled();
				if (!flag || (collisionColliderEnabled && cameraDistance > performanceSettings.DisableCollidersInActiveSectorUpperDistance))
				{
					unit.SetCollisionColliderEnabled(enabled: false);
				}
				else if (flag && !collisionColliderEnabled && cameraDistance < performanceSettings.DisableCollidersInActiveSectorLowerDistance)
				{
					unit.SetCollisionColliderEnabled(enabled: true);
				}
			}
		}

		public AnimatedUnitExplosion StartDeathAnimation()
		{
			AnimatedUnitExplosion result = null;
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: StartDeathAnimation", this, 3);
			}
			if (ActiveUnitClass.DestructionPrefab != null && unit.Sector != null && lastDistanceFromCamera < ActiveUnitClass.DestructionAnimMaxPlayDistance)
			{
				result = Engine.PlayPooledAnimatedUnitExplosion(ActiveUnitClass.DestructionPrefab, unit);
			}
			if (ActiveUnitShip != null)
			{
				ActiveUnitShip.DesiredTurn = 0f;
			}
			return result;
		}

		public bool GetShouldEffectsBeDrawn()
		{
			if (unit != null && !unit.IsDocked && !unit.IsDestroyed)
			{
				return !unit.IsFullyCloaked;
			}
			return false;
		}

		public void CreateDamageParticles(float damage)
		{
			GameSettings gameSettings = Engine.GameSettings;
			if (!(damage > 0f) || !(unit.Destructable.HealthNormalized < gameSettings.DamageParticlesThreshold) || GetActiveDamageParticles() >= damageParticlesInfo.Count)
			{
				return;
			}
			damage /= 0.8f;
			nextDamageParticlesGen -= damage / unit.UnitClass.maxHealth;
			if (!(nextDamageParticlesGen <= 0f))
			{
				return;
			}
			if (CountActiveDamageParticles() < gameSettings.DamageParticlesMaxCount)
			{
				UnitDamageParticlesInfo inactiveDamageParticles = GetInactiveDamageParticles();
				if (inactiveDamageParticles != null)
				{
					inactiveDamageParticles.Play();
				}
			}
			nextDamageParticlesGen = Mathf.Lerp(gameSettings.DamageParticlesMinHealth, gameSettings.DamageParticlesMaxHealth, Mathf.Pow(UnityEngine.Random.value, 1f + (1f - unit.Destructable.HealthNormalized) * 25f));
		}

		public void UpdateFxVisibility()
		{
			UpdateCameraDistance();
			UpdateInCameraRangeChanged();
		}

		public int GetActiveDamageParticles()
		{
			int num = 0;
			for (int i = 0; i < damageParticlesInfo.Count; i++)
			{
				if (damageParticlesInfo[i].IsActive)
				{
					num++;
				}
			}
			return num;
		}

		public void OnUnitDestroyed()
		{
			UpdateFxVisibility();
			if (ActiveUnitShip != null)
			{
				ActiveUnitShip.DesiredTurn = 0f;
			}
		}

		public AnimatedUnitExplosion RequestAnimatedExplosion()
		{
			if (unit.IsActiveInEngine)
			{
				return StartDeathAnimation();
			}
			return null;
		}

		public void PropelFromWormhole()
		{
			Rigidbody unitRigidBody = UnitRigidBody;
			if (unitRigidBody != null)
			{
				unitRigidBody.linearVelocity = transform.forward * 40f;
			}
		}

		private void PlayAmbientSounds()
		{
			if (!Engine.UnitAmbientSoundsEnabled)
			{
				return;
			}
			RecycleAmbientSounds();
			bool isPlayerCurrentUnit = unit.IsPlayerCurrentUnit;
			foreach (AudioSource ambientSound in ActiveUnitClass.AmbientSounds)
			{
				AmbientSound item = Engine.AmbientSoundManager.PlayAmbientSound(ambientSound, transform, isPlayerCurrentUnit ? Engine.GameSettings.PlayerAmbientSoundVolumeMultiplier : 1f);
				ambientSounds.Add(item);
			}
		}

		private void RecycleAmbientSounds()
		{
			foreach (AmbientSound ambientSound in ambientSounds)
			{
				if (ambientSound != null)
				{
					ambientSound.Parent = null;
				}
			}
			ambientSounds.Clear();
		}

		public void SafeDestroy()
		{
			RecycleAmbientSounds();
			if (unit != null && unit.ActiveUnit == this)
			{
				unit.ActiveUnit = null;
			}
			UnityEngine.Object.Destroy(gameObject);
		}

		private void OnJointBreak(float breakForce)
		{
			ActiveTractorTurret componentInChildren = GetComponentInChildren<ActiveTractorTurret>();
			if (componentInChildren != null)
			{
				componentInChildren.NotifyJointBroken(breakForce);
			}
		}

		private UnitDamageParticlesInfo GetInactiveDamageParticles()
		{
			inactiveParticleCache.Clear();
			for (int i = 0; i < damageParticlesInfo.Count; i++)
			{
				if (!damageParticlesInfo[i].IsActive)
				{
					inactiveParticleCache.Add(damageParticlesInfo[i]);
				}
			}
			return inactiveParticleCache.GetRandom();
		}

		private int CountActiveDamageParticles()
		{
			int num = 0;
			for (int i = 0; i < damageParticlesInfo.Count; i++)
			{
				if (damageParticlesInfo[i].IsActive)
				{
					num++;
				}
			}
			return num;
		}

		public void UpdateCameraDistance()
		{
			Camera mainCamera = GameController.Instance.MainCamera;
			if (mainCamera != null)
			{
				Vector3 p = unit.transform.position;
				Vector3 p2 = mainCamera.transform.position;
				lastDistanceFromCamera = Maths.GetDistanceIgnoreY(ref p, ref p2);
			}
		}

		private void UpdateInCameraRangeChanged()
		{
			if (inAudioRange)
			{
				if (LastDistanceFromCamera > ActiveUnitClass.AmbientSoundPlayFar)
				{
					inAudioRange = false;
					RecycleAmbientSounds();
				}
			}
			else if (LastDistanceFromCamera < ActiveUnitClass.AmbientSoundPlayNear)
			{
				inAudioRange = true;
				PlayAmbientSounds();
			}
			// The hard cull. At the top quality level the prefab LODGroups govern
			// instead - see LetLodGroupsGovernDrawDistance.
			if (LetLodGroupsGovernDrawDistance)
			{
				// Left in draw range permanently, so SetActive(false) is never
				// applied and a far ship keeps drawing its LOWEST LOD mesh
				// rather than disappearing.
				//
				// This also repairs units already culled before the quality
				// level was raised: UpdateVisibility runs every two seconds
				// and calls in here, so a unit culled at High is switched back
				// on within two seconds of moving up. Without that, raising
				// the setting in the options would leave everything that had
				// been culled still invisible for the rest of the session.
				if (!inDrawRange)
				{
					inDrawRange = true;
					SetDrawObjectsActive();
				}
			}
			else if (inDrawRange)
			{
				if (LastDistanceFromCamera > ActiveUnitClass.DrawDistFar)
				{
					inDrawRange = false;
					SetDrawObjectsActive();
				}
			}
			else if (LastDistanceFromCamera < ActiveUnitClass.DrawDistNear)
			{
				inDrawRange = true;
				SetDrawObjectsActive();
			}
		}

		/// <summary>
		/// Whether the per-prefab LODGroups should decide what is drawn,
		/// rather than a fixed distance that switches the whole ship off.
		/// </summary>
		/// <remarks>
		/// True only at the topmost quality level, where there is budget for
		/// the extra ships this keeps on screen. Below that the hard cull stays,
		/// which is also what keeps a phone fast: a phone will not be at the
		/// top level, so it keeps the cheap behaviour by default rather than
		/// needing a separate code path.
		///
		/// WHAT THIS ACTUALLY CHANGES, since it is easy to assume otherwise:
		/// it does not make the LODGroups any smarter, and it does not make
		/// them do work they were not doing. The LODGroup on each ship prefab
		/// has always been choosing the mesh inside the draw window. This
		/// widens that window from DrawDistFar (1000 by default) out to
		/// wherever the camera's far clip plane stops it - 8500 on the game
		/// camera - so a distant fleet stays visible as clusters of very low
		/// poly silhouettes instead of blinking out.
		///
		/// The cost is real and is the other side of that trade: the lowest
		/// LOD is still a real mesh with real triangles, so this draws more of
		/// them, over a much larger volume. That is why it is confined to the
		/// level where the budget for it exists.
		///
		/// Keyed off the TOPMOST index rather than a hardcoded 4, so
		/// reordering or adding quality levels does not silently move which
		/// one gets this.
		/// </remarks>
		private static bool LetLodGroupsGovernDrawDistance
		{
			get
			{
				if (GameController.Instance == null)
				{
					return false;
				}

				string[] names = QualitySettings.names;

				if (names == null || names.Length == 0)
				{
					return false;
				}

				return GameController.Instance.CurrentQualityLevel >= names.Length - 1;
			}
		}

		private void SetDrawObjectsActive()
		{
			for (int i = 0; i < DrawRangeObjects.Count; i++)
			{
				if (DrawRangeObjects[i] != null)
				{
					DrawRangeObjects[i].gameObject.SetActive(inDrawRange);
				}
			}
		}

		public void ApplyRenderMode()
		{
			if (LevelOfDetail)
			{
				foreach (MeshRenderer cloakRenderer in CloakRenderers)
				{
					cloakRenderer.enabled = activeUnitRenderMode == ActiveUnitRenderMode.Fade;
				}
				foreach (MeshRenderer unitRenderer in UnitRenderers)
				{
					unitRenderer.enabled = activeUnitRenderMode == ActiveUnitRenderMode.Opaque;
				}
			}
			else
			{
				if (CloakRenderer != null)
				{
					CloakRenderer.enabled = activeUnitRenderMode == ActiveUnitRenderMode.Fade;
				}
				if (UnitRenderer != null)
				{
					UnitRenderer.enabled = activeUnitRenderMode == ActiveUnitRenderMode.Opaque;
				}
			}
			if (activeUnitRenderMode != ActiveUnitRenderMode.Fade)
			{
				SetCloakOutlineRendererEnabled(enabled: false);
			}
		}

		public float GetEffectiveThrottle()
		{
			if (unit != null && unit.Components != null)
			{
				UnitEngineComponent engineComponent = unit.Components.EngineComponent;
				if (engineComponent != null && engineComponent.IsPoweredAndEnergySupplied)
				{
					return unit.Components.EngineThrottle;
				}
			}
			return 0f;
		}

		public float GetCurrentTurn()
		{
			if (ActiveUnitShip != null)
			{
				return ActiveUnitShip.CurrentTurn;
			}
			return 0f;
		}
	}
}
