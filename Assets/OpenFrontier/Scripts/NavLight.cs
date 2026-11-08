using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	// Navigation light chip - the generic, drop-anywhere prefab (built by
	// the editor's NavLightPrefabBuilder) whose inspector is the whole
	// authoring surface: colour, flare texture, flash pattern, period, size,
	// intensity and phase. Hand-place instances on a ship wherever you like.
	//
	// What it is NOT: there is no bake step, no per-ship mesh asset, and no
	// per-frame CPU anywhere in this component. Everything visual lives in
	// OpenFrontier_NavLights.shader (billboard, blink, occlusion).
	//
	// REDESIGN FOR THE REBUILD (2026-11-08): the former engine couplings are
	// gone. There is no ActiveUnit lookup (the owner concept belongs to the
	// new core and does not exist yet) and no NavLightTicker (it existed to
	// hide docked/destroyed/cloaked chips every 2s by asking
	// ActiveUnit.ShouldEffectsBeDrawn). A chip is therefore always lit - the
	// old "station" case, applied everywhere. When the new core knows what a
	// unit's visibility is, it can drive these chips directly: prefer
	// SetVisible(bool) here, or its own renderer toggles - no engine type is
	// needed for either.
	//
	// BATCHING
	// The SRP Batcher cannot carry per-instance colour/blink state, so these
	// draws batch by GPU instancing instead: one shared quad mesh + one
	// shared material with "Enable GPU Instancing", and the per-instance
	// values ride a MaterialPropertyBlock written ONCE (spawn + inspector
	// tweaks), never per frame. The shader declares the matching instanced
	// properties, so a whole sector of chips folds into one instanced draw
	// per material.
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

		// The billboard adds the hardware z-test, so frustum culling must
		// keep the renderer alive beyond the quad's own geometry or a
		// light pops out on cull boundaries the shader cannot see. This
		// radius is baked into the SHARED mesh's bounds (below), so no
		// per-instance bounds overrides and no per-instance code ever has
		// to run for culling. 10 covers any chip size a ship will carry
		// (the rendered flare is Size wide at the chip's transform) with
		// margin; it previously read 100, which kept every chip a draw
		// candidate up to a hundred metres past any frustum edge and also
		// made scene-view picking meaningless - clicking anywhere near a
		// ship returned whichever chip's 100-unit box came off the tie-break
		// first. Tighten costs nothing and buys both back (2026-11-07).
		private const float WorldCullRadius = 10f;

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

		// The explicit sync anchor, now the ONLY anchor (the old unit-backed
		// automatic is gone with the engine): two chips referencing the same
		// object flash with the same phase. Drop the ship root here and a
		// ship's lamps - including duplicates - flash as one; referencing a
		// prefab asset makes every ship of that class flash together, which
		// is a look, not a bug. Ship roots re-point per instance, so ships
		// stay desynced from each other while a ship's wings stay together.
		// Anything that is a UnityEngine.Object works (a UI object, a
		// faction root, a station) - the hash only needs anchors to point at
		// the same thing. Empty = the chip seeds from itself, so un-anchored
		// chips flash independently of each other; that is the opt-out, not
		// a bug.
		[Tooltip("Optional sync anchor (any GameObject/transform). Every chip referencing the same object flashes with the same phase - drop the ship root here so a ship's lamps (including duplicates) flash as one. Empty = independent.")]
		public Object PhaseGroup;

		[Tooltip("Offset from the beat the object's sync gives - the chip flashes this fraction of a cycle AFTER the object-derived phase. 0.5 = half a cycle after it.")]
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

		// Lazily resolved ids (see ResolvePropertyIds - the AGENTS.md trap
		// forbids native calls like Shader.PropertyToID in static
		// initializers, because type initialization can run during
		// serialization).
		private static bool propertyIdsResolved;
		private static int ColorId;
		private static int FlashId;
		private static int SizeId;

		// Per-texture material variants are cached so chips with the same
		// flare texture still share ONE material - the instancing batch key
		// is (mesh, material). Runtime clones, HideAndDontSave, same shape
		// ShipDecals uses for its material clones. Keyed by Unity 6's
		// EntityId (EnginePooler used the same as dictionary keys), so no
		// id-to-int casts anywhere. The textureless base
		// material lives in the same dictionary under key 0, so there is
		// exactly one caching story for both variants.
		private static Dictionary<EntityId, Material> materialByTexture;

		private MaterialPropertyBlock propertyBlock;

		private void Awake()
		{
			SetupRenderer();
		}

		private void OnEnable()
		{
			SetupRenderer();
			ApplyInstanceData();
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

		// Phase in [0,1). ALWAYS derived from an object, with ManualPhase
		// riding on top as the authored offset (user-requested 2026-11-07:
		// the Auto Phase toggle is gone - the object's beat is
		// unconditional, and the slider no longer replaces the phase, it
		// offsets the time the object gives). The seed, in explicit-first
		// order:
		//   1. PhaseGroup - the shared anchor (what the user asked for:
		//      two copied wing chips pointing at one thing flash together).
		//   2. The chip's own id - the un-anchored fallback (stations, bare
		//      scene ships). These chips desync from each other by design;
		//      the anchor is the way to opt into togetherness.
		// (The former middle case - the chip's owning unit - is gone with
		// the engine; anchoring to a generic transform now does that job
		// explicitly, which is the whole point of this redesign.)
		// The hash supplies the base beat and the slider staggers chips off
		// it, so a ship's kit keeps flashing as one while its lamps can be
		// ordered along the wing without losing that togetherness.
		// Nothing is saved or networked - the same deterministic-draw promise
		// ShipDecals makes for paint. Unity 6 made GetInstanceID
		// obsolete-with-error and its GetEntityId replacement returns an
		// EntityId struct whose int cast is error-obsolete too, so the hash
		// walks GetHashCode().
		private float Phase()
		{
			EntityId seed = PhaseGroup != null ? PhaseGroup.GetEntityId()
				: gameObject.GetEntityId();
			uint hashed = (uint)seed.GetHashCode();
			hashed ^= hashed >> 16;
			// The shader frac()s the phase slot (OpenFrontierNavLights.hlsl),
			// so a sum past 1 wraps without any maths here.
			return (hashed & 0xFFFF) / 65536f + ManualPhase;
		}

		// Visibility seam for the rebuild: the legacy engine hid chips by
		// docking/destroy/cloak state through NavLightTicker; the new core
		// will decide that itself. Until then a chip is always lit (the old
		// station rule). This helper is what a driver calls - one cached
		// bool compare and one renderer property write, not an Update.
		public void SetVisible(bool want)
		{
			if (quadRenderer != null)
			{
				quadRenderer.enabled = want;
			}
		}

#if UNITY_EDITOR
		// Game-view/scene-view marker so the "is the flare at the lamp or
		// displaced" argument ends in observation (2026-11-08 pivot
		// question). Magenta ball = the chip's anchor, the ONE position the
		// billboard maths cannot displace: the flare's centre always
		// projects here - the billboard is planted AT the transform origin,
		// as the strip restored. Keep it cheap and honest: one sphere.
		// (The pre-strip gizmo also drew a cyan z-relief line and read
		// _GrowReferenceDistance/_MaxGrowWorldSize/_PushScale from the
		// material - three properties the stripped shader no longer
		// defines, so reading them spammed the console; that gizmo is
		// gone with its knobs.)
		private void OnDrawGizmos()
		{
			Camera cam = Camera.current;
			if (cam == null)
			{
				return;
			}

			Vector3 anchor = transform.position;
			Vector3 toCamera = cam.transform.position - anchor;
			float dist = toCamera.magnitude;
			if (dist < 1e-4f)
			{
				return;
			}

			// The bead scales with distance so it stays findable at the
			// kilometre ranges these lights are meant to be read at.
			Gizmos.color = Color.magenta;
			Gizmos.DrawSphere(anchor, dist * 0.004f + 0.05f);
		}

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
		// builder writes this same data into a persisted asset via
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
			// error-obsolete) - so is this cache. The textureless default
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
				// tweaks on it must reach play, and a runtime rebuild from
				// Shader.Find would silently drop them. The asset lives under
				// a Resources root so play can reach it.
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
