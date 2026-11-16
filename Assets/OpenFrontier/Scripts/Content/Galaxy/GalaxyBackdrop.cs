using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// The ED-shaped galaxy backdrop: one baked square texture, projected
	/// through the galactic plane for the map and sampled as a cylindrical
	/// band for the sky.
	///
	///   Band    queue 2970  additive, the generated texture
	///   Glow    queue 2975  additive, 12/24 ED taps around the core
	///   Dust    queue 2976  alpha-blended inverted luminance of the same texture
	///   Density queue 2972  additive in-plane disc haze, map camera only
	///
	/// The band/dust geometry is ONE screen-covering quad. It is still not a
	/// naive screen-space effect: the fragment reconstructs the camera ray
	/// through UNITY_MATRIX_I_VP. The map intersects that ray with the galactic
	/// plane (parallel rays for the ortho MAP camera), while the sky converts
	/// it to galactic longitude/latitude and samples the bake as a cylindrical
	/// band around the pole, registered to the observer's true core direction.
	/// The old closed sphere was drawn with Cull Off, which put two identical
	/// band/dust fragments on every pixel; the quad removes that double
	/// additive/alpha pass. The map uses the true camera height and ED's near/
	/// far plane fades; the sky uses the finite latitude band and a warm veil so
	/// it is readable even when the system sits almost exactly in the plane.
	///
	/// Zero per-frame CPU except the transform dirty check; the renderers are
	/// child MeshRenderers, not a DrawMesh call in Update.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public class GalaxyBackdrop : MonoBehaviour
	{
		public GalaxyBlueprint blueprint;
		public GalaxyQualitySettings quality;

		const string BandShaderName = "OpenFrontier/Galaxy/Band";
		const string DustShaderName = "OpenFrontier/Galaxy/Dust";
		const string DensityShaderName = "OpenFrontier/Galaxy/Density";
		const string GlowShaderName = "OpenFrontier/Galaxy/Glow";
		// Layer 30 is the map presentation layer shared with GalaxyMapStars.
		// The density disc is a map-pipeline layer in ED; putting it here
		// keeps it out of the in-sky camera (layer 31) instead of relying on
		// an edge-on disc happening to contribute nothing.
		const int MapOnlyLayer = 30;

		static readonly int BandTexId = Shader.PropertyToID("_BandTex");
		static readonly int DensityTexId = Shader.PropertyToID("_DensityTex");
		static readonly int BandTintId = Shader.PropertyToID("_BandTint");
		static readonly int BandGainId = Shader.PropertyToID("_BandGain");
		static readonly int DensityTintId = Shader.PropertyToID("_DensityTint");
		static readonly int DensityGainId = Shader.PropertyToID("_DensityGain");
		static readonly int DensityGammaId = Shader.PropertyToID("_DensityGamma");
		static readonly int WorldToGalaxyId = Shader.PropertyToID("_WorldToGalaxy");
		static readonly int GalaxyRadiusId = Shader.PropertyToID("_GalaxyRadius");
		static readonly int FadeNearRangeId = Shader.PropertyToID("_FadeNearRange");
		static readonly int FadeNearRadiusId = Shader.PropertyToID("_FadeNearRadius");
		static readonly int FadeFarRangeId = Shader.PropertyToID("_FadeFarRange");
		static readonly int FadeFarRadiusId = Shader.PropertyToID("_FadeFarRadius");
		static readonly int SkyBandHalfAngleId = Shader.PropertyToID("_SkyBandHalfAngle");
		static readonly int DustColourId = Shader.PropertyToID("_DustColour");
		static readonly int DustStrengthId = Shader.PropertyToID("_DustStrength");
		static readonly int DustContrastId = Shader.PropertyToID("_DustContrast");
		static readonly int GlowCenterId = Shader.PropertyToID("_GlowCenter");
		static readonly int GlowSizeId = Shader.PropertyToID("_GlowSize");
		static readonly int GlowGainId = Shader.PropertyToID("_GlowGain");
		static readonly int GlowFalloffId = Shader.PropertyToID("_GlowFalloff");
		static readonly int GlowRadiusId = Shader.PropertyToID("_GlowRadius");
		static readonly int CoreColourId = Shader.PropertyToID("_CoreColour");

		Texture2D _texture;
		Material _bandMaterial;
		Material _dustMaterial;
		Material _densityMaterial;
		Material _glowMaterial;
		Mesh _sphere;
		Mesh _glowQuad;
		Mesh _discMesh;
		Transform _sphereRoot;
		Transform _dustRoot;
		Transform _densityRoot;
		Transform _glowRoot;
		GalaxyQualitySettings _settings;
		bool _built;
		bool _warned;

		void OnEnable()
		{
#if UNITY_EDITOR
			GalaxyBlueprint.Changed += ScheduleEditRebuild;
			GalaxyQualitySettings.Changed += ScheduleEditRebuild;
#endif
			Build();
		}

		void OnDisable()
		{
#if UNITY_EDITOR
			GalaxyBlueprint.Changed -= ScheduleEditRebuild;
			GalaxyQualitySettings.Changed -= ScheduleEditRebuild;
			GalaxyEditPreview.Cancel(ApplyEditRebuild);
#endif
			Teardown();
		}

		void OnDestroy()
		{
			Teardown();
		}

		void Update()
		{
			// Edit-mode preview: the band is scene geometry, so building it
			// once is enough for the Scene view to show the galaxy without
			// play mode. Inspector/asset changes debounce into one rebake.
			if (!_built || !transform.hasChanged) return;
			transform.hasChanged = false;
			SyncTransform();
		}

#if UNITY_EDITOR
		/// <summary>Debounced preview: asset edits schedule one rebake, not one per keystroke.</summary>
		void ScheduleEditRebuild()
		{
			if (Application.isPlaying || !isActiveAndEnabled) return;
			GalaxyEditPreview.Schedule(ApplyEditRebuild, 0.6f);
		}

		void ApplyEditRebuild()
		{
			if (isActiveAndEnabled) Rebuild();
		}
#endif

		void OnValidate()
		{
#if UNITY_EDITOR
			ScheduleEditRebuild();
#endif
		}

		/// <summary>Rebuild after the blueprint/quality asset changed. Not on the render path.</summary>
		[ContextMenu("Rebake")]
		public void Rebuild()
		{
			Teardown();
			if (isActiveAndEnabled) Build();
		}

		void Build()
		{
			if (blueprint == null)
			{
				Warn("no blueprint assigned - the band cannot be baked. Run OpenFrontier/Galaxy/Install ED Renderer.");
				return;
			}
			_settings = quality != null ? quality : GalaxyQualitySettings.Default;
			_settings.ApplyToShaders();

			Shader bandShader = Shader.Find(BandShaderName);
			Shader dustShader = Shader.Find(DustShaderName);
			Shader densityShader = Shader.Find(DensityShaderName);
			Shader glowShader = Shader.Find(GlowShaderName);
			if (bandShader == null || dustShader == null || densityShader == null || glowShader == null)
			{
				Warn($"shader missing (band={bandShader != null}, dust={dustShader != null}, density={densityShader != null}, glow={glowShader != null}) - import the OpenFrontier_Galaxy{{Band,Dust,Density,Glow}}.shader assets.");
				return;
			}

			var sw = System.Diagnostics.Stopwatch.StartNew();
			_texture = GalaxyBackgroundBaker.Bake(
				blueprint,
				_settings.backgroundTextureSize,
				_settings.rung,
				_settings.clumpNoise);
			if (_texture == null) return;
			sw.Stop();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.Log($"[GalaxyBackdrop] baked {_settings.rung} {_texture.width}x{_texture.height} band in {sw.ElapsedMilliseconds} ms " +
				$"(blueprint '{blueprint.name}', seed {blueprint.seed}).", this);
#endif

			// Transparent layer order (material queues, not shader tags):
			// band 2970, density 2972, core glow 2975, dust 2976, local
			// dust cards 2979, billboard cloud 2990, map stars 2991.
			// Density must draw UNDER the dust, or the additive haze washes
			// the lanes back out; the cards then sit over the global lanes.
			_bandMaterial = NewMaterial(bandShader, 2970);
			_dustMaterial = NewMaterial(dustShader, 2976);
			_densityMaterial = NewMaterial(densityShader, 2972);
			_glowMaterial = NewMaterial(glowShader, 2975);
			if (_bandMaterial == null || _dustMaterial == null || _densityMaterial == null || _glowMaterial == null) return;

			_bandMaterial.SetTexture(BandTexId, _texture);
			_bandMaterial.SetColor(BandTintId, _settings.bandTint);
			_bandMaterial.SetColor(CoreColourId, blueprint.coreColor);
			_bandMaterial.SetFloat(BandGainId, _settings.bandGain);

			_dustMaterial.SetTexture(BandTexId, _texture);
			_dustMaterial.SetColor(DustColourId, _settings.dustColour);
			_dustMaterial.SetFloat(DustStrengthId, _settings.dustStrength);
			_dustMaterial.SetFloat(DustContrastId, _settings.dustContrast);

			_densityMaterial.SetTexture(DensityTexId,
				blueprint.densityMap != null ? blueprint.densityMap : _texture);
			_densityMaterial.SetColor(DensityTintId, _settings.densityTint);
			_densityMaterial.SetFloat(DensityGainId, _settings.densityGain);
			_densityMaterial.SetFloat(DensityGammaId, _settings.densityGamma);

			_glowMaterial.SetTexture(BandTexId, _texture);
			_glowMaterial.SetFloat(GlowGainId, _settings.glowGain);
			_glowMaterial.SetFloat(GlowFalloffId, _settings.glowFalloff);
			_glowMaterial.SetFloat(GlowRadiusId, _settings.glowRadius);
			_glowMaterial.SetColor(CoreColourId, blueprint.coreColor);
			_glowMaterial.DisableKeyword("GLOW_12");
			_glowMaterial.DisableKeyword("GLOW_24");
			_glowMaterial.EnableKeyword(_settings.glowTaps >= 24 ? "GLOW_24" : "GLOW_12");

			SyncMaterials();
			BuildGeometry();
			_built = true;
		}

		void BuildGeometry()
		{
			_sphere = BuildScreenQuad();
			_glowQuad = BuildQuad();
			_discMesh = BuildDisc();

			// Band/dust are one screen-covering quad each. The shader owns
			// the clip-space position, so the child transform is irrelevant;
			// a huge mesh bound keeps Unity from frustum-culling the proxy.
			_sphereRoot = NewRenderer("Galaxy Band Screen", _sphere, _bandMaterial, 1f);
			_dustRoot = NewRenderer("Galaxy Dust Screen", _sphere, _dustMaterial, 1f);
			// the density disc lies IN the galactic plane: its UV is exact
			// under the ortho map camera (spec §2 layer 3), and an angled
			// perspective map that includes layer 30 sees the same disc
			// correctly. The in-sky camera (layer 31) masks it out entirely.
			_densityRoot = NewRenderer("Galaxy Density Disc", _discMesh, _densityMaterial, Mathf.Max(2f, blueprint.radius * 2f));
			_densityRoot.gameObject.layer = MapOnlyLayer;

			float parentScale = MaxAbsScale();
			float glowWorldSize = Mathf.Max(1f, blueprint.radius * parentScale * _settings.glowSizeFraction);
			_glowRoot = NewRenderer("Galaxy Core Glow", _glowQuad, _glowMaterial, glowWorldSize / Mathf.Max(parentScale, 1e-4f));
			_glowRoot.localPosition = Vector3.zero;

			SyncMaterials();
		}

		void SyncTransform()
		{
			if (!_built) return;
			SyncMaterials();
			if (_glowRoot != null)
			{
				float parentScale = MaxAbsScale();
				float glowWorldSize = Mathf.Max(1f, blueprint.radius * parentScale * _settings.glowSizeFraction);
				_glowRoot.localScale = Vector3.one * (glowWorldSize / Mathf.Max(parentScale, 1e-4f));
			}
		}

		void SyncMaterials()
		{
			Matrix4x4 worldToGalaxy = transform.worldToLocalMatrix;
			float radius = blueprint != null ? Mathf.Max(blueprint.radius, 1f) : 1f;
			if (_bandMaterial != null)
			{
				_bandMaterial.SetMatrix(WorldToGalaxyId, worldToGalaxy);
				_bandMaterial.SetFloat(GalaxyRadiusId, radius);
			}
			if (_dustMaterial != null)
			{
				_dustMaterial.SetMatrix(WorldToGalaxyId, worldToGalaxy);
				_dustMaterial.SetFloat(GalaxyRadiusId, radius);
			}
			if (_glowMaterial != null)
			{
				float parentScale = MaxAbsScale();
				_glowMaterial.SetVector(GlowCenterId, transform.position);
				_glowMaterial.SetFloat(GlowSizeId, Mathf.Max(1f, radius * parentScale * _settings.glowSizeFraction));
			}
			SetFades(_bandMaterial);
			SetFades(_dustMaterial);
		}

		void SetFades(Material mat)
		{
			if (mat == null) return;
			mat.SetFloat(FadeNearRangeId, _settings.fadeNearRange);
			mat.SetFloat(FadeNearRadiusId, Mathf.Max(_settings.fadeNearRadius, _settings.fadeNearRange + 0.01f));
			mat.SetFloat(FadeFarRangeId, _settings.fadeFarRange);
			mat.SetFloat(FadeFarRadiusId, Mathf.Max(_settings.fadeFarRadius, _settings.fadeFarRange + 0.01f));
			mat.SetFloat(SkyBandHalfAngleId, Mathf.Clamp(_settings.skyBandHalfAngle, 0.05f, 1.5f));
		}

		Material NewMaterial(Shader shader, int queue)
		{
			var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
			mat.renderQueue = queue;
			return mat;
		}

		Transform NewRenderer(string childName, Mesh mesh, Material material, float scale)
		{
			var go = new GameObject(childName)
			{
				hideFlags = HideFlags.HideAndDontSave,
				layer = gameObject.layer
			};
			go.transform.SetParent(transform, false);
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = Vector3.one * scale;

			var filter = go.AddComponent<MeshFilter>();
			filter.sharedMesh = mesh;
			var renderer = go.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = material;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			renderer.lightProbeUsage = LightProbeUsage.Off;
			renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
			return go.transform;
		}

		float MaxAbsScale()
		{
			Vector3 s = transform.lossyScale;
			return Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
		}

		void Teardown()
		{
			_built = false;
			DestroyChild(ref _sphereRoot);
			DestroyChild(ref _dustRoot);
			DestroyChild(ref _densityRoot);
			DestroyChild(ref _glowRoot);
			DestroyObject(_sphere); _sphere = null;
			DestroyObject(_glowQuad); _glowQuad = null;
			DestroyObject(_discMesh); _discMesh = null;
			DestroyObject(_bandMaterial); _bandMaterial = null;
			DestroyObject(_dustMaterial); _dustMaterial = null;
			DestroyObject(_densityMaterial); _densityMaterial = null;
			DestroyObject(_glowMaterial); _glowMaterial = null;
			DestroyObject(_texture); _texture = null;
		}

		void DestroyChild(ref Transform child)
		{
			if (child == null) return;
			DestroyObject(child.gameObject);
			child = null;
		}

		static void DestroyObject(Object obj)
		{
			if (obj == null) return;
			if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
		}

		void Warn(string message)
		{
			if (_warned) return;
			_warned = true;
			Debug.LogWarning($"[GalaxyBackdrop] '{name}': {message}", this);
		}

		/// <summary>
		/// One screen-covering quad. The vertex shader writes clip space
		/// directly, so the mesh only needs four corners; the huge bounds
		/// stop Unity's frustum culling from rejecting it when the camera is
		/// far outside the galaxy. No closed sphere means exactly one
		/// band/dust fragment per pixel, not a doubled additive pass.
		/// </summary>
		static Mesh BuildScreenQuad()
		{
			var mesh = new Mesh { name = "GalaxyBandScreenQuad", hideFlags = HideFlags.HideAndDontSave };
			mesh.vertices = new Vector3[4]
			{
				new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f),
				new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f)
			};
			mesh.uv = new Vector2[4]
			{
				new Vector2(0f, 0f), new Vector2(1f, 0f),
				new Vector2(0f, 1f), new Vector2(1f, 1f)
			};
			mesh.triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e8f);
			return mesh;
		}

		static Mesh BuildQuad()
		{
			var mesh = new Mesh { name = "GalaxyGlowQuad", hideFlags = HideFlags.HideAndDontSave };
			mesh.vertices = new Vector3[4]
			{
				new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
				new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
			};
			mesh.uv = new Vector2[4]
			{
				new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f)
			};
			mesh.triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
			return mesh;
		}

		/// <summary>
		/// The density disc: a quad IN the galactic plane (local XZ, y = 0),
		/// spanning the full disc diameter. Its UV maps 1:1 onto the baked
		/// texture's unit disc, so under the ortho map camera the sample is
		/// exact - no ray projection, no grid interpolation (the resolution
		/// of the spec §4 "coarse grid vs per-pixel ray" conflict for this
		/// layer: a flat quad in the plane needs neither).
		/// </summary>
		static Mesh BuildDisc()
		{
			var mesh = new Mesh { name = "GalaxyDensityDisc", hideFlags = HideFlags.HideAndDontSave };
			mesh.vertices = new Vector3[4]
			{
				new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
				new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f)
			};
			mesh.uv = new Vector2[4]
			{
				new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f)
			};
			mesh.triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
			return mesh;
		}
	}
}
