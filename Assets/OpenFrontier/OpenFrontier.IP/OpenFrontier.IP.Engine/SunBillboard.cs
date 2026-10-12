using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Open Frontier: the sector's directional light has no visible source.
	/// This builds a simple two-layer sun (bright core quad + soft glow quad,
	/// additive) placed along the light's direction at a constant angular
	/// size, following the camera every frame so it reads as infinitely
	/// distant. Added at runtime by ActiveSectorData onto the sector's
	/// directional light.
	///
	/// The materials (Assets/Material/SunCore.mat, SunGlow.mat) are the
	/// source of truth for textures and base brightness; StarTint is
	/// multiplied in per frame, so material edits apply live.
	/// </summary>
	public class SunBillboard : MonoBehaviour
	{
		public Material CoreMaterial;

		public Material GlowMaterial;

		// Per-sector star color (set by ActiveSectorData via
		// StarColorGenerator.ResolveCurrent); defaults to white.
		public Color StarTint = Color.white;

		// Placement: camera-origin, every frame - the sun sits on the
		// light's axis at a fixed distance from the camera. Zero parallax,
		// so it NEVER drifts or snaps no matter how far the camera flies.
		// (World-pinning fails: flying 9000 units in hand-crafted sectors
		// drags the disk ~65 degrees off the light axis. Child-of-camera
		// fails: rotation glues it to the screen. Per-frame camera-origin
		// is the only scheme that is stable under both translation and
		// rotation - it IS the sky.)
		private const float CoreAngularDiameter = 6.6f;

		private const float GlowAngularDiameter = 60f;

		// Shared unit quad (XY plane, -Z front face); also used by the
		// StarColorMarker editor preview.
		public static Mesh QuadMesh => SharedMeshes.Quad;

		// The sun quads live on the BackgroundPlanet layer (26) - rendered
		// by the PlanetCamera (far 500000, sandwiched between the starfield
		// SpaceCamera and the world GameCamera), the same trick the planets
		// use: the sun sits at 20000u, beyond the sky, with planets and
		// world objects still occluding it naturally through the shared
		// depth order. NOT DeepSpace (30): that layer is rendered only by
		// the pinned SpaceScene camera, which made the sun draw in the
		// backdrop's frame instead of the world's.
		// NOTE: looked up lazily - NameToLayer (a native call) is forbidden
		// from a MonoBehaviour's type initializer; a static readonly there
		// throws TypeInitializationException and poisons the whole type.
		private static int sunLayer = -2;

		private static int SunLayer
		{
			get
			{
				if (sunLayer == -2)
				{
					sunLayer = LayerMask.NameToLayer("BackgroundPlanet");
				}
				return sunLayer;
			}
		}

		// The reflection probe sits at the sector anchor (see
		// SpaceReflectionProbe); from there the camera-origin sun above is
		// parallax-shifted off the light axis. So a SECOND sun pair rides
		// the probe-only SkyProbe layer (3 - no camera renders it), placed
		// anchor-origin along the light axis: hulls reflect a sun that
		// matches the lighting. 12000 puts it beyond sector content
		// (asteroid fields reach ~9000) so the reflected sun occludes
		// like the camera sun; the probe's far plane is 100000.
		private const float AnchorSunDistance = 12000f;

		private static int skyProbeLayer = -2;

		private static int SkyProbeLayer
		{
			get
			{
				if (skyProbeLayer == -2)
				{
					skyProbeLayer = LayerMask.NameToLayer("SkyProbe");
					if (skyProbeLayer < 0)
					{
						skyProbeLayer = 3;
					}
				}
				return skyProbeLayer;
			}
		}

		private Light cachedLight;

		private Transform coreTransform;

		private Transform glowTransform;

		private Transform anchorCoreTransform;

		private Transform anchorGlowTransform;

		private MeshRenderer coreRenderer;

		private MeshRenderer glowRenderer;

		private MeshRenderer anchorCoreRenderer;

		private MeshRenderer anchorGlowRenderer;

		private MaterialPropertyBlock propertyBlock;

		private MaterialPropertyBlock anchorPropertyBlock;

		private void Awake()
		{
			cachedLight = GetComponent<Light>();
			propertyBlock = new MaterialPropertyBlock();
			anchorPropertyBlock = new MaterialPropertyBlock();
		}

		private void OnDestroy()
		{
			if (coreTransform != null)
			{
				Object.Destroy(coreTransform.gameObject);
			}
			if (glowTransform != null)
			{
				Object.Destroy(glowTransform.gameObject);
			}
			if (anchorCoreTransform != null)
			{
				Object.Destroy(anchorCoreTransform.gameObject);
			}
			if (anchorGlowTransform != null)
			{
				Object.Destroy(anchorGlowTransform.gameObject);
			}
		}

		// Materials are assigned AFTER AddComponent runs Awake, so the quads
		// are built lazily on the first LateUpdate that has them.
		private void Build()
		{
			coreTransform = CreateQuad("SunCore", CoreMaterial, SunLayer, out coreRenderer);
			glowTransform = CreateQuad("SunGlow", GlowMaterial, SunLayer, out glowRenderer);
			anchorCoreTransform = CreateQuad("SunCoreSkyProbe", CoreMaterial, SkyProbeLayer, out anchorCoreRenderer);
			anchorGlowTransform = CreateQuad("SunGlowSkyProbe", GlowMaterial, SkyProbeLayer, out anchorGlowRenderer);
		}

		private Transform CreateQuad(string objectName, Material material, int layer, out MeshRenderer meshRenderer)
		{
			GameObject gameObject = new GameObject(objectName);
			gameObject.transform.SetParent(transform, worldPositionStays: false);
			if (layer >= 0)
			{
				gameObject.layer = layer;
			}
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = SharedMeshes.Quad;
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = material;
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			return gameObject.transform;
		}

		private void LateUpdate()
		{
			if (coreTransform == null)
			{
				if (CoreMaterial == null || GlowMaterial == null)
				{
					return;
				}
				Build();
			}
			Camera main = WorldCamera.Resolve();
			if (main == null || cachedLight == null)
			{
				return;
			}
			// Hide while between sectors (wormhole transition): only the
			// tunnel should be visible in the void.
			bool flag = EngineASX.Instance == null || EngineASX.Instance.ActiveSector != null;
			if (coreRenderer.enabled != flag)
			{
				coreRenderer.enabled = flag;
				glowRenderer.enabled = flag;
				anchorCoreRenderer.enabled = flag;
				anchorGlowRenderer.enabled = flag;
			}
			Vector3 vector = -cachedLight.transform.forward;
			// Publish the sun direction for the planet atmosphere shader
			// (day/night scattering follows the same light the sun shows).
			Shader.SetGlobalVector("_SunDirectionWorld", new Vector4(vector.x, vector.y, vector.z, 0f));
			// 20000u on the planet layer: out with the planets, beyond
			// the sky, so the sun can never clip into the backdrop.
			// Asteroids/ships still occlude it (they render later in the
			// stack), and the angular-size math keeps the apparent size
			// identical at any distance.
			float num = 20000f;
			Vector3 position = main.transform.position + vector * num;
			// True billboard: face the camera dead-on (forward along the view
			// ray, up aligned to the camera's up) so the sun reads correctly
			// from any angle, including near the zenith/nadir.
			Vector3 vector3 = position - main.transform.position;
			Quaternion rotation = ((vector3.sqrMagnitude > 0.0001f) ? Quaternion.LookRotation(vector3, main.transform.up) : Quaternion.identity);
			float num2 = Vector3.Distance(main.transform.position, position);
			// Open Frontier: sun size scales with the star's INTENSITY
			// (the 1-3 brightness the generator/slider writes into the
			// sector's DirectionLightColor, game-wide) - hue and
			// saturation play no part. Light.intensity is NOT this: it
			// is a cloud-dimmed base multiplier, not the star's output.
			StarColorGenerator.RgbToHsv(StarTint, out float _, out float _, out float v);
			float num3 = Mathf.Lerp(0.7f, 1.6f, Mathf.InverseLerp(0.5f, 3f, v));
			float num4 = 2f * num2 * Mathf.Tan(CoreAngularDiameter * 0.5f * (Mathf.PI / 180f)) * num3;
			coreTransform.SetPositionAndRotation(position, rotation);
			coreTransform.localScale = new Vector3(num4, num4, 1f);
			glowTransform.SetPositionAndRotation(position + vector, rotation);
			float num5 = num4 * (GlowAngularDiameter / CoreAngularDiameter);
			glowTransform.localScale = new Vector3(num5, num5, 1f);
			// The probe-only anchor sun (SkyProbe layer): rides the light
			// axis from the sector anchor so the anchor-positioned
			// reflection probe captures a sun that matches the lighting.
			// NOT intensity-scaled: it is invisible to players and only
			// feeds the reflection capture - scaling it up injected
			// extra HDR energy into ship reflections (white blowout).
			Vector3 vector4 = (EngineASX.Instance != null && EngineASX.Instance.ActiveSector != null) ? EngineASX.Instance.ActiveSector.transform.position : main.transform.position;
			Vector3 position2 = vector4 + vector * AnchorSunDistance;
			Quaternion rotation2 = Quaternion.LookRotation(position2 - vector4, main.transform.up);
			float num6 = 2f * AnchorSunDistance * Mathf.Tan(CoreAngularDiameter * 0.5f * (Mathf.PI / 180f));
			anchorCoreTransform.SetPositionAndRotation(position2, rotation2);
			anchorCoreTransform.localScale = new Vector3(num6, num6, 1f);
			anchorGlowTransform.SetPositionAndRotation(position2 + vector, rotation2);
			anchorGlowTransform.localScale = new Vector3(num6 * (GlowAngularDiameter / CoreAngularDiameter), num6 * (GlowAngularDiameter / CoreAngularDiameter), 1f);
			// Dim with the cloud's starlight block: the visible sun
			// breathes with the same lerped factor the directional
			// light drops to inside gas clouds (additive quads scale
			// with color).
			float num7 = ((EngineASX.Instance != null && EngineASX.Instance.EnvironmentController != null) ? EngineASX.Instance.EnvironmentController.CurrentDirectionalLightFactor : 1f);
			Color starTint = StarTint * num7;
			// Material base color x star tint; the core is additionally
			// pulled towards white so it reads as the blinding disk.
			Color value = new Color((starTint.r + 1f) * 0.5f, (starTint.g + 1f) * 0.5f, (starTint.b + 1f) * 0.5f, 1f);
			propertyBlock.SetColor("_Color", CoreMaterial.GetColor("_Color") * value);
			coreRenderer.SetPropertyBlock(propertyBlock);
			propertyBlock.SetColor("_Color", GlowMaterial.GetColor("_Color") * starTint);
			glowRenderer.SetPropertyBlock(propertyBlock);
			// The ANCHOR sun feeds the reflection probe: an HDR tint in
			// the capture blurs across the cubemap's low mips and every
			// smooth ship wears it as a white sheen. Clamp the captured
			// color to LDR - a 1.0 sun still reads as a glint.
			Color value2 = new Color(Mathf.Min(value.r, 1f), Mathf.Min(value.g, 1f), Mathf.Min(value.b, 1f), 1f);
			anchorPropertyBlock.SetColor("_Color", CoreMaterial.GetColor("_Color") * value2);
			anchorCoreRenderer.SetPropertyBlock(anchorPropertyBlock);
			Color starTint2 = new Color(Mathf.Min(starTint.r, 1f), Mathf.Min(starTint.g, 1f), Mathf.Min(starTint.b, 1f), 1f);
			anchorPropertyBlock.SetColor("_Color", GlowMaterial.GetColor("_Color") * starTint2);
			anchorGlowRenderer.SetPropertyBlock(anchorPropertyBlock);
		}
	}
}
