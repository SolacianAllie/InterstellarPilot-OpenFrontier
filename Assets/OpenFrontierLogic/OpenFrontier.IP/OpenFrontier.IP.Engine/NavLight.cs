using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	// Navigation light chip - the generic, drop-anywhere prefab (built by
	// Assets/Editor/NavLightPrefabBuilder.cs) whose inspector is the whole
	// authoring surface: colour, flare texture, flash pattern, period, size,
	// intensity and phase. Hand-place instances on a ship wherever you like.
	//
	// What it is NOT: there is no bake step, no per-ship mesh asset, and no
	// per-frame CPU anywhere in this component. Everything visual lives in
	// OpenFrontier_NavLights.shader (billboard, distance grow, blink,
	// occlusion); everything stateful about the owning unit lives in
	// NavLightTicker, which polls on the 2s visibility cadence every other
	// system here already rides.
	//
	// BATCHING
	// The SRP Batcher cannot carry per-instance colour/blink state, so these
	// draws batch by GPU instancing instead: one shared quad mesh + one
	// shared material with "Enable GPU Instancing", and the per-instance
	// values ride a MaterialPropertyBlock written ONCE (spawn + inspector
	// tweaks), never per frame. The shader declares the matching instanced
	// properties, so a whole sector of chips folds into one instanced draw
	// per material.
	//
	// GATING
	// "Off when the hull is hidden": the ticker reads
	// ActiveUnit.ShouldEffectsBeDrawn - not docked, not destroyed, not fully
	// cloaked - the exact gate and rhythm the engine flares already use.
	// A NavLight with no ActiveUnit in its parents (a station, or any scene
	// object) is deliberately legal: it just stays lit. Do not add an error
	// for it; that design predates the component.
	public class NavLight : MonoBehaviour
	{
		public enum FlashPattern
		{
			Solid,
			SingleFlash,
			DoubleFlash
		}

		public const string NavLightShaderName = "OpenFrontier/Nav Lights";
		public const string NavLightMaterialAssetPath = "Assets/OpenFrontier/Resources/NavLight/Nav Lights.mat";
		public const string NavLightMaterialResourcesPath = "NavLight/Nav Lights";

		// The shader can grow a chip to its _MaxGrowWorldSize and the
		// billboard adds hardware z-test, so frustum culling must keep the
		// renderer alive beyond the quad's own geometry or a light pops out
		// on cull boundaries the shader cannot see. This radius is baked into
		// the SHARED mesh's bounds (below), so no per-instance bounds
		// overrides and no per-instance code ever has to run for culling.
		private const float WorldCullRadius = 100f;

		[Header("Look")]
		[Tooltip("Light colour. HDR allowed for glow; alpha is ignored (Intensity is the brightness knob).")]
		public Color LightColor = Color.white;

		[Tooltip("Optional flare texture. Leave empty for the shader's own soft round dot.")]
		public Texture2D FlareTexture;

		[Header("Flash")]
		[Tooltip("Solid, one pulse per cycle, or two pulses (aviation double flash).")]
		public FlashPattern Pattern = FlashPattern.SingleFlash;

		[Tooltip("Seconds per flash cycle.")]
		public float FlashPeriod = 1.2f;

		[Tooltip("Phases the whole ship together from its unit id, so ships desync and a ship's kit flashes as one. Turn off to set the phase yourself.")]
		public bool AutoPhase = true;

		[Tooltip("Phase in the cycle, used only when Auto Phase is off. 0.5 = starts half a cycle in.")]
		[Range(0f, 1f)]
		public float ManualPhase;

		[Header("Shape")]
		[Tooltip("Chip size in world units at close range.")]
		public float Size = 0.5f;

		[Tooltip("Brightness multiplier. Push above 1 (HDR) to bloom on the quality tiers that run bloom.")]
		public float Intensity = 4f;

		// The quad child's renderer. Found lazily; self-heals (creates the
		// child) if a NavLight was added by hand to an empty GameObject.
		private Renderer quadRenderer;

		private ActiveUnit activeUnit;

		// Lazily resolved ids (see OnEnable - the AGENTS.md trap forbids
		// native calls like Shader.PropertyToID in static initializers,
		// because type initialization can run during serialization).
		private static bool propertyIdsResolved;
		private static int ColorId;
		private static int FlashId;
		private static int SizeId;

		// Per-texture material variants are cached so chips with the same
		// flare texture still share ONE material - the instancing batch key
		// is (mesh, material). Runtime clones, HideAndDontSave, same shape
		// ShipDecals uses for its material clones. Keyed by Unity 6's
		// EntityId (EnginePooler uses the same as dictionary keys), so no
		// id-to-int casts anywhere. The textureless base
		// material lives in the same dictionary under key 0, so there is
		// exactly one caching story for both variants.
		private static Dictionary<EntityId, Material> materialByTexture;

		private MaterialPropertyBlock propertyBlock;

		private void Awake()
		{
			// FindInParentsOrSelf, the UnitThruster pattern: the chip's owner
			// is whatever unit the prefab belongs to. Null is legal here (see
			// the gating note in the class doc) - no log, no exception.
			activeUnit = UnityObjectHelper.FindInParentsOrSelf<ActiveUnit>(gameObject);

			SetupRenderer();
		}

		private void OnEnable()
		{
			// A panel-rebuilt or reparented chip may find a new owner; the
			// first lookup normally settled this in Awake already.
			if (activeUnit == null)
			{
				activeUnit = UnityObjectHelper.FindInParentsOrSelf<ActiveUnit>(gameObject);
			}

			SetupRenderer();
			ApplyInstanceData();

			// Gating runs only in play: prefab view and edit scenes do not
			// register anything and the ticker does not exist there.
			if (Application.isPlaying)
			{
				NavLightTicker.Register(this);
			}
		}

		private void OnDisable()
		{
			// Destruction runs OnDisable first, so pooling, scene unload and
			// destroy all land here; the repeated unregister path is a
			// no-op already.
			if (Application.isPlaying)
			{
				NavLightTicker.Unregister(this);
			}
		}

		// Called by NavLightTicker on the shared 2s cadence. Per instance this
		// is one cached bool compare and one renderer property write, twice a
		// second per chip at most - decoration never reaches the frame loop
		// proper.
		public void PollGate()
		{
			if (quadRenderer == null)
			{
				return;
			}

			// No ActiveUnit (stations, bare scene lights) - always on.
			// With one - the same gate the engine flares ride: hull present
			// means the chips burn; fully hidden hull means they die too.
			bool want = activeUnit == null || activeUnit.ShouldEffectsBeDrawn;

			quadRenderer.enabled = want;
		}

		// The Quad child ONLY: a recursive GetComponentInChildren would
		// happily ADOPT a hull's own MeshFilter when a chip is hand-added
		// on a ship root (or any GameObject with deeper meshes under it),
		// and SetupRenderer would then overwrite the hull's mesh with the
		// nav quad - a silent disaster of exactly the sort this component
		// exists to avoid. Chips are one level deep by construction
		// (NavLightPrefabBuilder and the self-heal both author "Quad" as a
		// direct child), so direct children are the true home.
		private static MeshFilter FindQuadMeshFilter(Transform parent)
		{
			int count = parent.childCount;
			for (int i = 0; i < count; i++)
			{
				MeshFilter filter = parent.GetChild(i).GetComponent<MeshFilter>();
				if (filter != null)
				{
					return filter;
				}
			}
			return null;
		}

		private void SetupRenderer()
		{
			if (quadRenderer != null)
			{
				return;
			}

			// Find the quad child, or make one - the prefab author should
			// not have to care, and hand-AddComponent users get a working
			// light without a menu trip. The search is DIRECT CHILDREN
			// ONLY (see FindQuadMeshFilter): a recursive search would
			// adopt a hull's own MeshFilter and rewrite its mesh.
			MeshFilter meshFilter = FindQuadMeshFilter(transform);
			GameObject child;
			if (meshFilter == null)
			{
				child = new GameObject("Quad");
				child.transform.SetParent(transform, false);
				meshFilter = child.AddComponent<MeshFilter>();
			}
			meshFilter.sharedMesh = meshFilter.sharedMesh == null ? SharedQuadMesh() : meshFilter.sharedMesh;

			quadRenderer = meshFilter.GetComponent<Renderer>();
			if (quadRenderer == null)
			{
				quadRenderer = meshFilter.gameObject.AddComponent<MeshRenderer>();
				quadRenderer.sharedMaterial = SharedBaseMaterial();
			}
			// An authored chip keeps its builder-assigned material here: the
			// material swap that ApplyInstanceData performs may land a
			// texture variant on specific textures only, and the asset's
			// material (with any user tweaks) must stay the default.
			else if (quadRenderer.sharedMaterial == null)
			{
				quadRenderer.sharedMaterial = SharedBaseMaterial();
			}
		}

		// One MaterialPropertyBlock write: spawn-time and inspector-time
		// only. This never happens per frame - bliss belongs to the GPU.
		private void ApplyInstanceData()
		{
			ResolvePropertyIds();
			if (quadRenderer == null)
			{
				return;
			}

			// The texture-variant material swap happens BEFORE the MPB so a
			// material swap never draws one frame with the old instance
			// values.
			quadRenderer.sharedMaterial = MaterialFor(FlareTexture);

			if (propertyBlock == null)
			{
				propertyBlock = new MaterialPropertyBlock();
			}

			// HDR colour with the intensity folded into the alpha slot: the
			// shader multiplies tint.rgb * tint.a, so a plain editor colour
			// picker never loses bloom range, and greyed-out alpha nonsense
			// never surprises anyone.
			Vector4 color = new Vector4(
				LightColor.r, LightColor.g, LightColor.b, Mathf.Max(0.01f, Intensity));
			Vector4 flash = new Vector4(
				Mathf.Max(0.05f, FlashPeriod), (float)Pattern, Phase(), 0f);

			propertyBlock.SetVector(ColorId, color);
			propertyBlock.SetVector(FlashId, flash);
			propertyBlock.SetFloat(SizeId, Mathf.Max(0.01f, Size));

			quadRenderer.SetPropertyBlock(propertyBlock);
		}

		// Phase in [0,1). Auto = a stable hash of the OWNING UNIT's id when
		// there is one: all chips on one ship flash together, different
		// ships desync, and nothing is saved or networked - the same
		// deterministic-draw promise ShipDecals makes for paint. Unity 6 made
		// GetInstanceID obsolete-with-error and its GetEntityId replacement
		// returns an EntityId struct whose int cast is error-obsolete too, so
		// the hash walks GetHashCode() - the same brand of accessor
		// EnginePooler's entity-id dictionaries already ride.
		private float Phase()
		{
			if (!AutoPhase)
			{
				return ManualPhase;
			}

			EntityId seed = activeUnit != null ? activeUnit.GetEntityId() : gameObject.GetEntityId();
			uint hashed = (uint)seed.GetHashCode();
			hashed ^= hashed >> 16;
			return (hashed & 0xFFFF) / 65536f;
		}

#if UNITY_EDITOR
		// Live authoring: every inspector change lands in prefab view and in
		// play mode immediately - that is the point of a generic prefab.
		// Deliberately does NOT create GameObjects here: OnValidate runs
		// during prefab save/serialize walks, where GameObject creation is
		// forbidden. The child exists from Awake/OnEnable, or it does not
		// exist and this only updates what is there.
		private void OnValidate()
		{
			ResolvePropertyIds();
			if (quadRenderer == null)
			{
				// Direct children only - same hazard as SetupRenderer.
				MeshFilter meshFilter = FindQuadMeshFilter(transform);
				if (meshFilter == null)
				{
					return;
				}
				quadRenderer = meshFilter.GetComponent<Renderer>();
				if (quadRenderer == null)
				{
					return;
				}
			}
			quadRenderer.sharedMaterial = MaterialFor(FlareTexture);
			ApplyInstanceData();
		}
#endif

		// ---- shared plumbing ---------------------------------------------------

		// The one quad every chip draws: 4 verts, corners (±0.5, ±0.5, 0) and
		// UV 0..1. The shader ignores the geometry except the corner sign,
		// billboards it, and grows it with distance - so the mesh also owns
		// the cull radius (WorldCullRadius, generous beyond the growth cap),
		// which is why no per-instance bounds override exists anywhere.
		// Built lazily on first use for the runtime self-heal path; the editor
		// prefab builder writes this same data into a persisted asset via
		// FillQuadMesh, so there is exactly one shape definition in play.
		public static Mesh SharedQuadMesh()
		{
			if (sharedQuadMesh == null)
			{
				Mesh mesh = new Mesh();
				FillQuadMesh(mesh);
				mesh.hideFlags = HideFlags.HideAndDontSave;
				sharedQuadMesh = mesh;
			}
			return sharedQuadMesh;
		}

		private static Mesh sharedQuadMesh;

		// The shared shape: native calls go through here and the editor
		// builder reuses it for the persisted quad asset, so the runtime
		// fallback and the authored asset cannot drift apart.
		public static void FillQuadMesh(Mesh mesh)
		{
			mesh.name = "NavLightQuad";
			// Corners match the built-in quad's local layout so the shader's
			// corner maths never has a second opinion.
			Vector3[] vertices =
			{
				new Vector3(-0.5f, -0.5f, 0f),
				new Vector3(0.5f, -0.5f, 0f),
				new Vector3(-0.5f, 0.5f, 0f),
				new Vector3(0.5f, 0.5f, 0f)
			};
			mesh.vertices = vertices;
			mesh.uv = new Vector2[]
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(0f, 1f),
				new Vector2(1f, 1f)
			};
			mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
			Vector3 extent = new Vector3(
				WorldCullRadius, WorldCullRadius, WorldCullRadius);
			mesh.bounds = new Bounds(Vector3.zero, extent * 2f);
		}

		// The textureless base material, or its cached per-texture clone.
		// Clones keep Enable GPU Instancing ON; a texture variant is simply
		// its own instanced group.
		public static Material SharedBaseMaterial()
		{
			return MaterialFor(null);
		}

		private static Material MaterialFor(Texture2D texture)
		{
			// Unity 6: ids are EntityId structs (GetInstanceID casts are
			// error-obsolete), and EnginePooler already uses them as
			// dictionary keys - so does this cache. The textureless default
			// material lives under default(EntityId).
			EntityId key = texture != null ? texture.GetEntityId() : default;
			if (materialByTexture != null && materialByTexture.TryGetValue(key, out Material cached) &&
				cached != null)
			{
				return cached;
			}

			if (texture == null)
			{
				// Prefer the PERSISTED asset material: the user's shader-side
				// tweaks on it (grow distance, far fade, round shaping) must
				// reach play, and a runtime rebuild from Shader.Find would
				// silently drop them. The NavLightPrefabBuilder places the
				// asset under a Resources root so play can reach it.
				Material asset = Resources.Load<Material>(NavLightMaterialResourcesPath);
				if (asset != null)
				{
					if (materialByTexture == null)
					{
						materialByTexture = new Dictionary<EntityId, Material>();
					}
					materialByTexture[key] = asset;
					return asset;
				}
			}

			// Texture variants and the fallback base clone from the PERSISTED
			// asset when it exists, so user tweaks on it ride into variants
			// too; Shader.Find is the no-asset fallback for a hand-built chip.
			Material source = texture == null
				? Resources.Load<Material>(NavLightMaterialResourcesPath)
				: MaterialFor(null);
			if (source != null)
			{
				Material variant = new Material(source)
				{
					// GPU instancing is the batching contract for MPB'd chips;
					// without it every light pays an own draw call.
					enableInstancing = true
				};
				variant.name = "Nav Lights (variant, " + (texture != null ? texture.name : "none") + ")";
				variant.hideFlags = HideFlags.HideAndDontSave;
				if (texture != null)
				{
					variant.SetTexture("_FlareTex", texture);
				}
				if (materialByTexture == null)
				{
					materialByTexture = new Dictionary<EntityId, Material>();
				}
				materialByTexture[key] = variant;
				return variant;
			}

			// No persisted asset anywhere: rebuild from the shader. A null
			// shader means the asset broke or was stripped - returning null
			// hands the renderer Unity's magenta error-shader, which is
			// EXACTLY the loud failure AGENTS.md wants: a silent light is a
			// bug that ships.
			Shader shader = Shader.Find(NavLightShaderName);
			if (shader == null)
			{
				return null;
			}
			Material material = new Material(shader)
			{
				enableInstancing = true
			};
			material.name = "Nav Lights (runtime, " + (texture != null ? texture.name : "none") + ")";
			material.hideFlags = HideFlags.HideAndDontSave;
			if (texture != null)
			{
				material.SetTexture("_FlareTex", texture);
			}
			if (materialByTexture == null)
			{
				materialByTexture = new Dictionary<EntityId, Material>();
			}
			materialByTexture[key] = material;
			return material;
		}

		// Lazily resolved ids - native calls belong here, never in static
		// field initializers (AGENTS.md serialization trap).
		private static void ResolvePropertyIds()
		{
			if (propertyIdsResolved)
			{
				return;
			}
			ColorId = Shader.PropertyToID("_Color");
			FlashId = Shader.PropertyToID("_Flash");
			SizeId = Shader.PropertyToID("_Size");
			propertyIdsResolved = true;
		}

		// Domain reload can be OFF (Project Settings). Statics then outlive a
		// play session: invalidate everything static-dependent on the next
		// play's start so no stale id or material survives a reload with
		// Enter Play Mode Options enabled.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticsForNoDomainReload()
		{
			propertyIdsResolved = false;
			sharedQuadMesh = null;
			materialByTexture = null;
		}
	}
}
