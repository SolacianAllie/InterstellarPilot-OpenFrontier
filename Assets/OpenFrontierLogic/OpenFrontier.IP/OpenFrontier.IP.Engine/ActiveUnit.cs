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

		// Open Frontier: cloak-shader state. A hull whose material carries
		// the cloak uniforms (_CloakAmount exists) is driven directly by
		// this unit: the legacy cloak-shell and outline renderers stay
		// dark and the hull trades itself for bent light inside the
		// shader (OpenFrontier/Ship Cloak). Everything else keeps the old
		// shell behaviour untouched.
		private bool cloakCapableHullChecked;
		private bool cloakCapableHull;
		// v3 shell-driven routing: some ship families (the Magnus pirate,
		// to name the one that fooled a whole session) were AUTHORED with
		// the cloak look as separate legacy shell meshes -- the hull
		// renderers carry the plain Ship Surface material while the
		// "Cloak" meshes carry the fold material. The grey the player saw
		// "when cloaked" was those shells, drawn the legacy way, because
		// the hull probe below failed and everything defaulted to the old
		// render mode -- the fold never ran and the backdrop counter never
		// engaged. When a unit is not hull-capable but its shells carry
		// the cloak uniforms, the SHELLS become the cloak hull instead:
		// they render through the fold shader (authored hull clones, the
		// same swap the old game performed between hull and shell) while
		// the hull stands in exactly decloaked, and no grey shell ever
		// exists again.
		private bool cloakDrivenByShells;
		private float lastCloakAmount = -1f;
		private float lastCloakFloor = -1f;
		private int lastCloakFriendly = -1;
		private bool wantsCloakSceneColor;

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
					// A reassigned unit starts a fresh cloak history: the cached
					// pushes must not carry the previous hull's values.
					ResetCloakMaterialState();
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

		private void OnEnable()
		{
			// A re-enabled unit restarts its cloak history: the first
			// ApplyCloakToUnitRenderers must push for real, not skip on a
			// stale "unchanged" verdict from a previous draw cycle.
			ResetCloakMaterialState();
		}

		private void OnDisable()
		{
			// Scene teardown or despawn: a dead unit must not keep the
			// opaque-texture copy running for the fleet forever.
			ReleaseCloakSceneColor();
		}

		private void ResetCloakMaterialState()
		{
			lastCloakAmount = -1f;
			lastCloakFloor = -1f;
			lastCloakFriendly = -1;
			ReleaseCloakSceneColor();
		}

		private void ReleaseCloakSceneColor()
		{
			if (wantsCloakSceneColor)
			{
				wantsCloakSceneColor = false;
				CloakBackdropRequests.Request(wants: false);
			}
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
				if (IsCloakCapableHull())
				{
					// The hull's own shader owns cloaking: no shell swaps, no
					// render mode. Amount and floor are pushed every frame while
					// anything is actually changing.
					UpdateCloakMaterial();
					return;
				}
				UpdateRenderMode();
				switch (activeUnitRenderMode)
				{
				case ActiveUnitRenderMode.Opaque:
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

		public void UpdateFadeForConstructionProgressValue(float constructionProgress, Faction owner)
		{
			VideoSettings videoSettings = GameController.Instance.GameSettings.VideoSettings;
			float alpha = Mathf.Lerp(videoSettings.StationUnderConstructionMinAlpha, videoSettings.StationUnderConstructionMaxAlpha, (Mathf.Sin(constructionProgress * videoSettings.StationUnderConstructionAlphaFadeRate) + 1f) / 2f);
			SetFadeAlpha(alpha, owner);
		}

		public void SetFadeAlpha(float alpha, Faction owner)
		{
			if (IsCloakCapableHull())
			{
				// External fade control (BuildModePlacementScreen preview and
				// friends): alpha is visibility. The cloak shader gets the
				// complement as amount with no persistent floor, so the wanted
				// alpha is exactly the alpha shown.
				ApplyCloakToUnitRenderers(1f - Mathf.Clamp01(alpha), 0f);
				return;
			}
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

		private bool IsCloakCapableHull()
		{
			if (!cloakCapableHullChecked)
			{
				cloakCapableHullChecked = true;
				// One probe at first use: the hulls' materials are fixed per
				// prefab (UpdateOpaqueMaterial was an empty stub, nothing swaps
				// them at runtime), so this verdict never goes stale.
				MeshRenderer meshRenderer = (LevelOfDetail && UnitRenderers.Count > 0) ? UnitRenderers[0] : UnitRenderer;
				Material hullMaterial = ((meshRenderer != null) ? meshRenderer.sharedMaterial : null);
				cloakCapableHull = ((hullMaterial != null) && hullMaterial.HasProperty("_CloakAmount"));
				MeshRenderer shellRepresentative = null;
				Material shellMaterial = null;
				if (!cloakCapableHull)
				{
					// No fold material on the hull: fall back to the authored
					// legacy shells when THEY carry it (see cloakDrivenByShells).
					// Shells that are still legacy grey stay legacy -- this only
					// routes when the material state genuinely exists.
					shellRepresentative = (LevelOfDetail && CloakRenderers.Count > 0) ? CloakRenderers[0] : CloakRenderer;
					shellMaterial = ((shellRepresentative != null) ? shellRepresentative.sharedMaterial : null);
					if (shellMaterial != null && shellMaterial.HasProperty("_CloakAmount"))
					{
						cloakCapableHull = true;
						cloakDrivenByShells = true;
					}
				}
				ReportCloakProbe(meshRenderer, hullMaterial, shellRepresentative, shellMaterial);
			}
			return cloakCapableHull;
		}

		// One print per unit: the verdict that routes this unit's whole
		// cloak flow would otherwise be invisible -- it took a session of
		// frame-debugger archaeology to learn that the "cloak draws" on
		// screen were the legacy shells. Editor-only.
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private void ReportCloakProbe(MeshRenderer meshRenderer, Material hullMaterial, MeshRenderer shellRepresentative, Material shellMaterial)
		{
			string hullName = meshRenderer != null ? meshRenderer.name : "<none>";
			string hullMaterialName = hullMaterial != null ? hullMaterial.name : "<none>";
			string shellName = shellRepresentative != null ? shellRepresentative.name : "<none>";
			string shellMaterialName = shellMaterial != null ? shellMaterial.name : "<none>";
			Debug.Log($"[CloakProbe] '{name}': hull '{hullName}'/{hullMaterialName}, shell '{shellName}'/{shellMaterialName} -> capable {cloakCapableHull}, shell-driven {cloakDrivenByShells}");
		}

		private void UpdateCloakMaterial()
		{
			if (unit.IsUnderConstructionOrDismantling)
			{
				// The same strobe settings the legacy shell used, expressed as
				// cloak amount: visibility inverted, no persistent floor.
				VideoSettings videoSettings = GameController.Instance.GameSettings.VideoSettings;
				float alpha = Mathf.Lerp(videoSettings.StationUnderConstructionMinAlpha, videoSettings.StationUnderConstructionMaxAlpha, (Mathf.Sin(unit.Components.ConstructionProgress * unit.UnitClass.maxHealth * videoSettings.StationUnderConstructionAlphaFadeRate) + 1f) / 2f);
				ApplyCloakToUnitRenderers(1f - alpha, 0f);
			}
			else if (!unit.IsFullyDecloaked)
			{
				// Cloak progress under the same power curve the shell alpha
				// used, with the settings' visibility floor for full cloak
				// (0.3 owned/allied, 0.07 enemies).
				CloakComponent cloakComponent = unit.Components.CloakComponent;
				VideoSettings videoSettings2 = GameController.Instance.GameSettings.VideoSettings;
				float cloakAmount = Mathf.Pow(cloakComponent.CloakProgress, videoSettings2.CloakAlphaPower);
				ApplyCloakToUnitRenderers(cloakAmount, cloakComponent.GetCloakMinAlpha());
			}
			else
			{
				ApplyCloakToUnitRenderers(0f, 0f);
			}
		}

		private void ApplyCloakToUnitRenderers(float cloakAmount, float visibleFloor)
		{
			// vis = how much of the hull's own material survives; the shader
			// spends 1 - vis on bent background. Same surface the old alpha
			// floors described, read through the hull instead of a shell.
			float visible = Mathf.Lerp(1f, Mathf.Clamp01(visibleFloor), Mathf.Clamp01(cloakAmount));
			// The friendly tell gate is the old outline gate: owned or allied,
			// and either the current unit or close enough to read.
			int friendly = (!(unit.IsPlayerOrAllied() && (unit.IsPlayerCurrentUnit || lastDistanceFromCamera < 1500f))) ? 0 : 1;
			bool draw = visible > 0.003f;
			if (cloakAmount != lastCloakAmount || visibleFloor != lastCloakFloor || friendly != lastCloakFriendly)
			{
				lastCloakAmount = cloakAmount;
				lastCloakFloor = visibleFloor;
				lastCloakFriendly = friendly;
				if (cloakMaterialPropertyBlock == null)
				{
					cloakMaterialPropertyBlock = new MaterialPropertyBlock();
				}
				if (cloakDrivenByShells)
				{
					// The fold runs on the authored shell meshes (see
					// cloakDrivenByShells); the hull stands in exactly
					// decloaked and parks the instant cloaking starts, so
					// the player never sees a grey shell -- the shell was
					// where the fold material already lived.
					bool shellDraw = draw && cloakAmount > 0.001f;
					if (LevelOfDetail)
					{
						foreach (MeshRenderer unitRenderer in UnitRenderers)
						{
							unitRenderer.enabled = cloakAmount <= 0.001f;
						}
						foreach (MeshRenderer shellRenderer in CloakRenderers)
						{
							shellRenderer.enabled = shellDraw;
							ApplyCloakMaterialPropertyBlock(shellRenderer, cloakAmount, visibleFloor, friendly);
						}
					}
					else
					{
						if (UnitRenderer != null)
						{
							UnitRenderer.enabled = cloakAmount <= 0.001f;
						}
						if (CloakRenderer != null)
						{
							CloakRenderer.enabled = shellDraw;
							ApplyCloakMaterialPropertyBlock(CloakRenderer, cloakAmount, visibleFloor, friendly);
						}
					}
				}
				else if (LevelOfDetail)
				{
					foreach (MeshRenderer unitRenderer in UnitRenderers)
					{
						unitRenderer.enabled = draw;
						ApplyCloakMaterialPropertyBlock(unitRenderer, cloakAmount, visibleFloor, friendly);
					}
				}
				else
				{
					if (UnitRenderer != null)
					{
						UnitRenderer.enabled = draw;
						ApplyCloakMaterialPropertyBlock(UnitRenderer, cloakAmount, visibleFloor, friendly);
					}
				}
			}
			// The bend needs the frame behind the hull. Track transitions once:
			// the backdrop snapshot is built only while some hull actually
			// deflects light, not for every decloaked ship carrying the shader.
			bool wants = cloakAmount > 0.001f;
			if (wantsCloakSceneColor != wants)
			{
				wantsCloakSceneColor = wants;
				OpenFrontier.CloakBackdropRequests.Request(wants);
			}
		}

		private void ApplyCloakMaterialPropertyBlock(MeshRenderer meshRenderer, float cloakAmount, float visibleFloor, int friendly)
		{
			meshRenderer.GetPropertyBlock(cloakMaterialPropertyBlock);
			cloakMaterialPropertyBlock.SetFloat("_CloakAmount", cloakAmount);
			cloakMaterialPropertyBlock.SetFloat("_CloakFloor", visibleFloor);
			cloakMaterialPropertyBlock.SetFloat("_CloakFriendly", friendly);
			meshRenderer.SetPropertyBlock(cloakMaterialPropertyBlock);
		}

		public void UpdateRenderMode()
		{
			ActiveUnitRenderMode = ((!unit.IsFullyDecloaked || unit.IsUnderConstructionOrDismantling) ? ActiveUnitRenderMode.Fade : ActiveUnitRenderMode.Opaque);
		}

		private void UpdateFadeMaterial()
		{
			// The cloak outline is gone: owned-ship visibility while
			// construction/cloak fading belongs to the materials now (the
			// cloak shader pushes the tell from within; non-cloak hulls keep
			// their shell, which was always the visible thing here).
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
			// inDrawRange is a PROXIMITY flag, not the model cull, and it must stay
			// one. ActiveUnitThrusterAudio computes volume from engine throttle
			// rather than distance, and Asteroid gates its yield explosion on it,
			// so a unit held permanently in range would be audible across the
			// sector and every asteroid impact anywhere in the sector would pop
			// a particle effect. That is the opposite of what the flag is for,
			// on a project whose lowest target is a phone.
			//
			// An earlier attempt lifted the flag itself at the top quality level,
			// on the theory that the LODGroups had taken over. That was the
			// wrong half of the wrong fix: it kept the audio and effects cost,
			// and it still left the models broken - see SetDrawObjectsActive,
			// where the actual cull now is, and where the whole regression is
			// written out.
			bool wasInDrawRange = inDrawRange;

			if (inDrawRange)
			{
				if (LastDistanceFromCamera > ActiveUnitClass.DrawDistFar)
				{
					inDrawRange = false;
				}
			}
			else if (LastDistanceFromCamera < ActiveUnitClass.DrawDistNear)
			{
				inDrawRange = true;
			}

			// Only on an actual transition. SetDrawObjectsActive is idempotent
			// and now unconditional, so this is purely to avoid re-walking a
			// two-entry list on every visibility poll - which is every two
			// seconds per unit.
			if (inDrawRange != wasInDrawRange)
			{
				SetDrawObjectsActive();
			}
		}

		private void SetDrawObjectsActive()
		{
			// Always ON. Never follows the flag, at any distance and at any
			// quality level.
			//
			// This is the actual fix for structures vanishing at the top
			// quality level. History, because it is the whole shape of the
			// bug:
			//
			// SetActive(inDrawRange) was guarded by
			// `&& !LetLodGroupsGovernDrawDistance` at the top level, on the
			// theory that the prefab LODGroups took over there. They did not
			// "take over" - suppressing the call just meant these objects were
			// never switched back ON. A unit switched off at a lower level then
			// stayed off, because the flag only clears again below DrawDistNear,
			// so anything still far away never recovered for the rest of the
			// session. Loading straight into the top level hit it too, because
			// CurrentQualityLevel is -1 until GameController resolves it, so
			// early units were culled before the gate went active.
			//
			// bd3ae90b added a re-enable. 8f5aae89 dropped it while separating
			// the flag from the cull, and that is the regression.
			//
			// What draws these objects now is the real mechanism, and for these
			// prefabs that mechanism is thinner than it looks. Be honest about
			// what is actually underneath:
			//
			//   GameCamera far clip plane is 8500. That is the real bound - a
			//   station past it is not drawn at all, so this is 8.5x further out
			//   than the old 1000 cull, but it is not unbounded.
			//
			//   NO LODGroup on any structure prefab - Factory, TradeStation,
			//   Bar2, Refinery all have zero. There is no LOD ladder to climb,
			//   so QualitySettings lodBias (0.5 -> 2 across the range) does
			//   NOTHING for them specifically. A station inside the frustum at
			//   8000 units draws at the same detail as one at 100.
			//
			//   Occlusion culling is switched on for six cameras but has zero
			//   occluders baked, so it is not doing any work either.
			//
			// So inside the frustum and inside 8500 units, these render at full
			// detail and NO graphics setting changes that. That is the cost of
			// lifting the cull, and it is a deliberate trade for never having a
			// structure silently vanish - but it is not free, and if station
			// cost ever shows up on a phone the fix is authored LODGroups on the
			// structure prefabs, not putting a distance back here.
			//
			// inDrawRange itself is untouched and still gates thruster audio,
			// asteroid yield explosions and material updates. See
			// ActiveUnitClass.DrawDistFar for why that has to stay real.
			for (int i = 0; i < DrawRangeObjects.Count; i++)
			{
				if (DrawRangeObjects[i] != null)
				{
					DrawRangeObjects[i].gameObject.SetActive(true);
				}
			}
		}

		public void ApplyRenderMode()
		{
			if (IsCloakCapableHull())
			{
				// The cloak shader owns visibility through _CloakAmount: shells
				// and outlines stay dark for good, the hull draws until nothing
				// is left to draw (ApplyCloakToUnitRenderers toggles it per
				// frame).
				if (cloakDrivenByShells)
				{
					// Shell-driven routing: the fold owns the shells; the hull
					// stands in as the decloak surface. Per-frame
					// ApplyCloakToUnitRenderers owns the swap from here on --
					// this only sets the quiet state so spawn frames flash
					// neither the standard hull mid-cloak nor a grey shell.
					if (LevelOfDetail)
					{
						foreach (MeshRenderer cloakRenderer in CloakRenderers)
						{
							cloakRenderer.enabled = false;
						}
						foreach (MeshRenderer unitRenderer in UnitRenderers)
						{
							unitRenderer.enabled = true;
						}
					}
					else
					{
						if (CloakRenderer != null)
						{
							CloakRenderer.enabled = false;
						}
						if (UnitRenderer != null)
						{
							UnitRenderer.enabled = true;
						}
					}
					SetCloakOutlineRendererEnabled(enabled: false);
					return;
				}
				if (LevelOfDetail)
				{
					foreach (MeshRenderer cloakRenderer in CloakRenderers)
					{
						cloakRenderer.enabled = false;
					}
					foreach (MeshRenderer unitRenderer in UnitRenderers)
					{
						unitRenderer.enabled = true;
					}
				}
				else
				{
					if (CloakRenderer != null)
					{
						CloakRenderer.enabled = false;
					}
					if (UnitRenderer != null)
					{
						UnitRenderer.enabled = true;
					}
				}
				SetCloakOutlineRendererEnabled(enabled: false);
				return;
			}
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
