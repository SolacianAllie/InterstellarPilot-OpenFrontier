using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.ActiveUnitFx;
using OpenFrontier.IP.Engine.AmbientSounds;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Settings;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnit : MonoBehaviour
	{
		public delegate void ActiveUnitDrawDistanceChanged(ActiveUnit sender);

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

		public MeshRenderer UnitRenderer;

		public List<MeshRenderer> UnitRenderers;

		private MaterialPropertyBlock cloakMaterialPropertyBlock;

		// Open Frontier: the fold source. After the endgame migration every
		// unit wears its fold on the hull itself; this hand-assigned material
		// is where a hull that was not authored fold-capable gets it from.
		// The shell pools and their renderer lists are deleted - shader
		// property gates verify the assignment so a wrong material can lose
		// the field nothing but the cloak itself. Slot 0 only - the body slot
		// in every authored hull; the deeper slots (glass, engine bits) stay
		// authored as-is.
		public Material CloakMaterial;

		public bool ApplyTeamColours;

		public bool LevelOfDetail;

		[SerializeField]
		private List<ActiveUnitThrusterAudio> activeUnitThrusterAudios = new List<ActiveUnitThrusterAudio>();

		// Open Frontier: cloak-shader state. A hull whose material carries
		// the cloak uniforms (_CloakAmount exists) is driven directly by
		// this unit: the legacy cloak-shell renderers stay dark and the
		// hull trades itself for bent light inside the
		// shader (OpenFrontier/Ship Cloak). Everything else keeps the old
		// shell behaviour untouched.
		private bool cloakCapableHullChecked;
		private bool cloakCapableHull;
		// v3 shell-driven routing, retired: some ship families (the Magnus
		// pirate, to name the one that fooled a whole session) were AUTHORED
		// with the cloak look as separate legacy shell meshes -- the hull
		// renderers carry the plain Ship Surface material while the
		// "Cloak" meshes carry the fold material. For those the old routing
		// parked the hull and drew the shell; that whole route is gone.
		// Today every fold lives on the hull itself: either the hull's
		// authored material carries the cloak uniforms, or the one-shot
		// transpose below writes a fold material into the hull renderer's
		// slots -- sourced from the authored shells still in the prefab, or
		// from the hand-assigned Cloak Material for hulls whose shell meshes
		// are gone. A hull that answers neither simply does not cloak.
		// v4 material transpose: true when the probe folded the shell pool's
		// materials onto the hull itself. The hull IS the cloak mesh
		// afterwards - only one mesh set ever draws, which is the point:
		// the duplicate authoring was pure waste on a phone.
		private bool cloakMaterialAdopted;
		private float lastCloakAmount = -1f;
		private float lastCloakFloor = -1f;
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
			cloakMaterialPropertyBlock = new MaterialPropertyBlock();
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
			// Open Frontier: peel in the faction livery, in post. This runs
			// on every ActiveUnit creation - which is every active-sector
			// entry - so existing units get the transplant without any
			// separate sweep. Same class gate as the cloak probe: legacy
			// hulls without the decal slots keep their authored look.
			ShipDecals.ApplyToUnit(unit);
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
			// The cloak shader is the single fade authority (the renamed
			// Cloaked materials carry it, and every shell is cloak-driven):
			// alpha is visibility, the shader gets the complement as its
			// amount with no persistent floor, so the wanted alpha is
			// exactly the alpha shown. Units with no cloak-capable material
			// never had a ghost of their own - their shells were empty no-ops.
			if (IsCloakCapableHull())
			{
				ApplyCloakToUnitRenderers(1f - Mathf.Clamp01(alpha), 0f);
			}
		}

		private bool IsCloakCapableHull()
		{
			if (!cloakCapableHullChecked)
			{
				cloakCapableHullChecked = true;
				// One probe at first use: the hulls' materials are fixed per
				// prefab (UpdateOpaqueMaterial was an empty stub, nothing swaps
				// them at runtime), so this verdict never goes stale. The one
				// mutation the probe performs is the v4 transpose below, which
				// is also one-shot and idempotent.
				MeshRenderer meshRenderer = (LevelOfDetail && UnitRenderers.Count > 0) ? UnitRenderers[0] : UnitRenderer;
				Material hullMaterial = ((meshRenderer != null) ? meshRenderer.sharedMaterial : null);
				cloakCapableHull = ((hullMaterial != null) && hullMaterial.HasProperty("_CloakAmount"));
				if (!cloakCapableHull)
				{
					// v4 material transpose. A hull that does not already
					// wear the fold adopts it once, here, onto itself: the
					// fold materials come from the authored shells the
					// prefab still carries (same slot index, matching slot
					// count), or from the hand-assigned Cloak Material for
					// hulls whose shell meshes are gone. The fold shader
					// was built so a hull material can simply WEAR it (see
					// OpenFrontier_ShipCloak.shader: a Ship Surface
					// material transposed onto Ship Cloak keeps its
					// bindings and flags intact and looks IDENTICAL at
					// amount 0), so the hull becomes the ship's only
					// clothed mesh from then on, and the per-frame cloak
					// flow underneath is unchanged.
					cloakMaterialAdopted = TryAdoptCloakMaterialOnHull();
					if (cloakMaterialAdopted)
					{
						cloakCapableHull = true;
					}
				}
				ReportCloakProbe();
			}
			return cloakCapableHull;
		}

		// One print per unit, and it is the POST-transpose state: the hull
		// renderers with the material each slot now wears. The pre-state
		// names ('MaterialShuttle' and friends) say nothing about whether
		// the fold actually landed - the verdict does. Editor-only.
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private void ReportCloakProbe()
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			builder.Append("[CloakProbe] '").Append(name).Append("'");
			if (LevelOfDetail)
			{
				for (int i = 0; i < UnitRenderers.Count; i++)
				{
					AppendRendererProbe(builder, UnitRenderers[i]);
				}
			}
			else
			{
				AppendRendererProbe(builder, UnitRenderer);
			}
			builder.Append(" -> capable ").Append(cloakCapableHull)
				.Append(", transposed ").Append(cloakMaterialAdopted);
			Debug.Log(builder.ToString());
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private void AppendRendererProbe(System.Text.StringBuilder builder, MeshRenderer meshRenderer)
		{
			if (meshRenderer == null)
			{
				builder.Append(" '<none>'/(null)");
				return;
			}
			builder.Append(" '").Append(meshRenderer.name).Append("'/");
			Material[] mats = meshRenderer.sharedMaterials;
			for (int i = 0; i < mats.Length; i++)
			{
				if (i > 0)
				{
					builder.Append("|");
				}
				builder.Append(mats[i] != null ? mats[i].name : "<null>");
			}
		}

		// v4 material transpose. The evidence this encodes, read off every
		// shell-issued prefab in the repo (Ares, Creon, Drake, Flyer, Hauler,
		// Hornet, Magnus and its variants, Overlord, Pioneer, Ranger, Raptor,
		// Shuttle, Thunder, Venture, Satellite, StorageLocker, Bar2, Factory,
		// Refinery, Shipyard, SmallOutpost, TradeStation, WeaponsPlatform):
		//
		//   - hulls and shells reference the SAME mesh assets; the shell set
		//     is a duplicate renderer pass wearing the fold materials
		//   - every ship wears ONE distinct fold-capable material
		//   - second slots (Glass Golden and friends) are identical materials
		//     on both pools and never carry the fold
		//
		// So pairing is structural, not by name: a hull material slot adopts
		// the fold that the authored shells wore at the SAME slot index of a
		// shell renderer with the SAME slot count. Glass stays glass, body
		// goes fold, and even Drake's odd LOD5 pair (fold on slot 1 there)
		// reproduces the authored look exactly, because that is the shell
		// itself speaking. Any slot with no fold to adopt stays authored,
		// which is also what those slots render in the legacy shell path.
		//
		// A hand-assigned CloakMaterial (for prefab-cleaned hulls whose shell
		// meshes are gone) wins for slot 0 - the body slot in every hull,
		// verified across all of the above. Runs once, per unit, at probe
		// time; the per-frame cloak flow is untouched underneath.
		private bool TryAdoptCloakMaterialOnHull()
		{
			bool changed = false;
			if (LevelOfDetail)
			{
				for (int i = 0; i < UnitRenderers.Count; i++)
				{
					changed |= TransposeHullRenderer(UnitRenderers[i]);
				}
			}
			else
			{
				changed = TransposeHullRenderer(UnitRenderer);
			}
			return changed;
		}

		private bool TransposeHullRenderer(MeshRenderer hullRenderer)
		{
			if (hullRenderer == null)
			{
				return false;
			}
			Material[] materials = hullRenderer.sharedMaterials;
			if (materials.Length == 0)
			{
				return false;
			}
			bool edited = false;
			for (int slot = 0; slot < materials.Length; slot++)
			{
				Material slotMaterial = materials[slot];
				if (slotMaterial != null && slotMaterial.HasProperty("_CloakAmount"))
				{
					continue;
				}
				Material foldMaterial = FoldMaterialForSlot(slot);
				if (foldMaterial != null && foldMaterial != slotMaterial)
				{
					materials[slot] = foldMaterial;
					edited = true;
				}
			}
			if (edited)
			{
				hullRenderer.sharedMaterials = materials;
			}
			return edited;
		}

		private Material FoldMaterialForSlot(int slot)
		{
			// The hand-assigned material carries the fold by being a real
			// cloak material; a misassignment must not claim capability for
			// the whole hull, so the property check is the gate.
			return (slot == 0 && CloakMaterial != null && CloakMaterial.HasProperty("_CloakAmount"))
				? CloakMaterial : null;
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
			// _CloakFloor is how much hull material survives at amount 1
			// (0 = the hull fully cedes to the bend). Appearance is the
			// material's state alone: the fold surface is never toggled by
			// visibility, so at full cloak the silhouette keeps drawing as
			// pure bent space instead of turning into nothing.
			if (cloakAmount != lastCloakAmount || visibleFloor != lastCloakFloor)
			{
				lastCloakAmount = cloakAmount;
				lastCloakFloor = visibleFloor;
				if (cloakMaterialPropertyBlock == null)
				{
					cloakMaterialPropertyBlock = new MaterialPropertyBlock();
				}
				if (LevelOfDetail)
				{
					foreach (MeshRenderer unitRenderer in UnitRenderers)
					{
						unitRenderer.enabled = true;
						ApplyCloakMaterialPropertyBlock(unitRenderer, cloakAmount, visibleFloor);
					}
				}
				else
				{
					if (UnitRenderer != null)
					{
						UnitRenderer.enabled = true;
						ApplyCloakMaterialPropertyBlock(UnitRenderer, cloakAmount, visibleFloor);
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

		private void ApplyCloakMaterialPropertyBlock(MeshRenderer meshRenderer, float cloakAmount, float visibleFloor)
		{
			meshRenderer.GetPropertyBlock(cloakMaterialPropertyBlock);
			cloakMaterialPropertyBlock.SetFloat("_CloakAmount", cloakAmount);
			cloakMaterialPropertyBlock.SetFloat("_CloakFloor", visibleFloor);
			meshRenderer.SetPropertyBlock(cloakMaterialPropertyBlock);
		}

		public void UpdateRenderMode()
		{
			ActiveUnitRenderMode = ((!unit.IsFullyDecloaked || unit.IsUnderConstructionOrDismantling) ? ActiveUnitRenderMode.Fade : ActiveUnitRenderMode.Opaque);
		}

		private void UpdateFadeMaterial()
		{
			// Owned-ship visibility while construction/cloak fading
			// belongs to the materials now (the fold shader answers its
			// own amount; non-cloak hulls keep their shell, which was
			// always the visible thing here).
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
			// The cloak shader owns hull visibility through _CloakAmount;
			// outlines stay dark for good, the hull draws until nothing is
			// left to draw (ApplyCloakToUnitRenderers toggles it per frame).
			// Non-capable units have no fold anywhere, so their visibility is
			// the render mode alone.
			if (IsCloakCapableHull())
			{
				if (LevelOfDetail)
				{
					foreach (MeshRenderer unitRenderer in UnitRenderers)
					{
						unitRenderer.enabled = true;
					}
				}
				else if (UnitRenderer != null)
				{
					UnitRenderer.enabled = true;
				}
				return;
			}
			if (LevelOfDetail)
			{
				foreach (MeshRenderer unitRenderer in UnitRenderers)
				{
					unitRenderer.enabled = activeUnitRenderMode == ActiveUnitRenderMode.Opaque;
				}
			}
			else if (UnitRenderer != null)
			{
				UnitRenderer.enabled = activeUnitRenderMode == ActiveUnitRenderMode.Opaque;
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
