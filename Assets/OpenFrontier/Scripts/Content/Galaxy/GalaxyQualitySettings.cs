using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>The galaxy render ladder. Ordered, so int comparisons work.</summary>
	public enum GalaxyQualityRung
	{
		Mobile = 0,
		Desktop = 1,
		Hero = 2
	}

	/// <summary>
	/// The galaxy renderer's quality table, shaped after ED's
	/// GraphicsConfiguration.xml blocks this rebuild replaces:
	///
	///   GalaxyBackground  TextureSize 512 / 1024 / 2048  (Low / Medium / High;
	///                     Ultra aliases High, there is no fourth block)
	///   GalaxyMap         MilkyWayInstancesCount 8000 / 12000 / 16000
	///                     MilkywayInstancesBrightness 1.5 / 1.3 / 1.0
	///                     MilkywayInstancesSize 1.3 / 1.15 / 1.0
	///                     StarInstanceCount 2000 / 2000 / 4000 (the map's
	///                     textured sheet sprites, mapStarSprites; the sky
	///                     catalog keeps its own skyStarCount budget)
	///                     NebulasCount 10 / 20 / 30
	///                     LocalDustBrightness 0.13 / 0.033 / 0.0
	///
	/// The billboard COUNT is deliberately higher than ED's (20k / 40k /
	/// 80k): the miniature is viewed across a wider zoom range and its band
	/// bake is softer than StellarForge's, so ED's 12k leaves visible gaps.
	/// The inverse brightness/size compensation stays ED's - fewer billboards
	/// are individually brighter and larger so the band reads the same; more
	/// billboards keep their per-sprite size and let density do the work.
	///
	/// This asset is the single source for a future in-depth graphics screen:
	/// every field is a named, inspectable knob, and the rung only rewrites the
	/// preset-controlled block (the artistic knobs below the headers keep their
	/// values when the rung changes).
	/// </summary>
	[CreateAssetMenu(menuName = "OpenFrontier/Galaxy/Render Quality", fileName = "GalaxyQuality")]
	public class GalaxyQualitySettings : ScriptableObject
	{
		[Header("Ladder")]
		[Tooltip("Mobile = ED Low, Desktop = ED Medium, Hero = ED High/Ultra.")]
		public GalaxyQualityRung rung = GalaxyQualityRung.Desktop;

		[Header("Galaxy Background (ED <GalaxyBackground>)")]
		[Tooltip("512 / 1024 / 2048. ED has no Ultra block; Ultra reuses High.")]
		public int backgroundTextureSize = 1024;
		[Tooltip("Multiplies the baked band colour before the additive draw.")]
		public float bandGain = 1.05f;
		[Tooltip("Warm cream tint over the baked band. ED's map reads amber, not white.")]
		public Color bandTint = new Color(1f, 0.97f, 0.92f, 1f);
		[Tooltip("Bake-time clump noise, relative to the density value.")]
		[Range(0f, 0.5f)] public float clumpNoise = 0.16f;

		[Header("Dust lanes (same texture, inverted)")]
		[Tooltip("Peak opacity of the dark lanes. ED's lanes are bold; below ~0.5 the disc just looks dirty.")]
		[Range(0f, 1f)] public float dustStrength = 0.82f;
		[Tooltip("Moves the lane threshold. Higher keeps the dust out of the bright arms and only deepens the gaps.")]
		[Range(0.5f, 3f)] public float dustContrast = 1.3f;
		[ColorUsage(false)] public Color dustColour = new Color(0.045f, 0.028f, 0.022f, 1f);

		[Header("Density haze (ED StarMapDensityMap)")]
		[Tooltip("ED's density-map immediates are {50.0, 1.2}; the split into gain/exponent is unresolved (findings §7.3). Gain multiplies the bake's coverage.")]
		public float densityGain = 1.05f;
		[Tooltip("Gamma on the haze. 1 = linear coverage.")]
		[Min(0.01f)] public float densityGamma = 1f;
		[ColorUsage(false)] public Color densityTint = Color.white;

		[Header("Core glow (ED GalaxyGlow taps)")]
		[Tooltip("12 = phone, 24 = desktop/hero. A shader keyword, so the loop is compile-time.")]
		[Range(12, 24)] public int glowTaps = 24;
		public float glowGain = 2.4f;
		public float glowFalloff = 3f;
		[Tooltip("Tap radius as a fraction of the galaxy texture width.")]
		public float glowRadius = 0.18f;
		[Tooltip("Glow quad diameter as a fraction of the galaxy diameter.")]
		[Range(0.1f, 1f)] public float glowSizeFraction = 0.32f;

		[Header("Plane distance fades (map camera only)")]
		[Tooltip("ED's gAlphaFadeRange / gAlphaFadeRadius: the plane fades as the map camera approaches it.")]
		public float fadeNearRange = 20f;
		public float fadeNearRadius = 400f;
		[Tooltip("ED's gAlphaFadeRangeFar / gAlphaFadeRadiusFar: hand over to billboards at extreme zoom-out.")]
		public float fadeFarRange = 40000f;
		public float fadeFarRadius = 90000f;
		[Tooltip("Legacy virtual plane height. The sky now uses a cylindrical band, so this is kept only so old assets deserialize; it is not read.")]
		public float skyPlaneHeight = 500f;
		[Tooltip("Half-angle of the in-system sky band, radians. 0.30 = a 34-degree-wide band around the galactic equator.")]
		[Range(0.1f, 1.2f)] public float skyBandHalfAngle = 0.30f;

		[Header("Milky Way billboards (ED GalaxyMap)")]
		public int starCloudInstances = 40000;
		public float starCloudBrightness = 1.25f;
		public float starCloudSize = 1f;
		public int localDustCards = 40;
		[Range(0f, 1f)] public float localDustBrightness = 0.033f;
		[Tooltip("ED NebulasCount. The volumetric subsystem has its own rung; this is the UI-facing count.")]
		public int nebulaVolumes = 20;

		[Header("Cloud sprite pixel sizes (map and sky share these)")]
		[Tooltip("Dimmest unresolved star, screen pixels. The cloud is a star field, not a set of world-sized discs.")]
		[Min(0.25f)] public float cloudStarPxMin = 1f;
		[Tooltip("Brightest unresolved star, screen pixels.")]
		[Min(0.25f)] public float cloudStarPxMax = 2.6f;
		[Tooltip("Smallest haze sprite, screen pixels.")]
		[Min(1f)] public float cloudHazePxMin = 3f;
		[Tooltip("Largest haze sprite, screen pixels. Keep small or the band turns into cotton balls in perspective.")]
		[Min(1f)] public float cloudHazePxMax = 8f;

		[Header("Map star sprites (ED StarInstanceCount)")]
		[Tooltip("Textured StarMapStar sprites drawn by the orthographic map. ED: 2000 / 2000 / 4000.")]
		public int mapStarSprites = 2000;

		[Header("Sky stars (procedural, ED SkyboxStar model)")]
		public int skyStarCount = 5000;
		public float skyStarBrightness = 1.15f;
		public float skyStarSizeScale = 1f;

		[Header("Sky star magnitude curve (ED immediates)")]
		public float starDimmestMag = 6.5f;
		public float starCappedMag = -1.5f;
		[Tooltip("Stars brighter than this APPARENT magnitude take ED's super-bright branch (pow 3).")]
		public float starSuperBrightMag = 1f;
		public float starSizeMin = 1.6f;
		public float starSizeMax = 12f;
		[Range(0.5f, 4f)] public float starRadPow = 3f;
		[Range(0.5f, 3f)] public float starAlphaPow = 1.6f;
		[Range(0f, 1f)] public float haloStart = 0.35f;
		public float haloStrength = 0.35f;
		[Range(0f, 1f)] public float spikeStart = 0.15f;
		public float spikeStrength = 0.06f;
		[Range(0f, 1f)] public float ringStart = 0.9f;
		public float ringStrength = 0.12f;
		[Range(0f, 0.5f)] public float minVisAlpha = 0.015f;

		[SerializeField, HideInInspector] GalaxyQualityRung _appliedRung = (GalaxyQualityRung)(-1);

		/// <summary>
		/// Fires after this asset is edited in the inspector. Galaxy components
		/// subscribe while enabled (editor only) to apply/reseed live, so a
		/// slider drag is visible in the Scene view without entering play mode.
		/// </summary>
		public static event System.Action Changed;

		/// <summary>Rewrites the preset-controlled block. Artistic knobs are left alone.</summary>
		public void ApplyPreset(GalaxyQualityRung next)
		{
			rung = next;
			_appliedRung = next;
			// ED's own rungs are 8k / 12k / 16k MilkyWayInstancesCount. The
			// miniature is viewed across a much wider zoom range than ED's
			// map (and its band bake is softer than StellarForge's), so the
			// counts scale up while the inverse brightness/size compensation
			// stays ED's. 20k sprites is still ~40k triangles, one draw.
			switch (next)
			{
				case GalaxyQualityRung.Mobile: // ED Low
					backgroundTextureSize = 512;
					starCloudInstances = 20000;
					starCloudBrightness = 1.3f;
					starCloudSize = 1.1f;
					localDustCards = 24;
					localDustBrightness = 0.13f;
					nebulaVolumes = 10;
					glowTaps = 12;
					skyStarCount = 2500;
					mapStarSprites = 2000;
					break;
				case GalaxyQualityRung.Hero: // ED High / Ultra
					backgroundTextureSize = 2048;
					starCloudInstances = 80000;
					starCloudBrightness = 0.95f;
					starCloudSize = 0.95f;
					localDustCards = 60;
					localDustBrightness = 0.0f;
					nebulaVolumes = 30;
					glowTaps = 24;
					skyStarCount = 8000;
					mapStarSprites = 4000;
					break;
				default: // ED Medium
					backgroundTextureSize = 1024;
					starCloudInstances = 40000;
					starCloudBrightness = 1.05f;
					starCloudSize = 1f;
					localDustCards = 40;
					localDustBrightness = 0.033f;
					nebulaVolumes = 20;
					glowTaps = 24;
					skyStarCount = 5000;
					mapStarSprites = 2000;
					break;
			}
			ApplyToShaders();
		}

		[ContextMenu("Apply Rung Preset")]
		void ApplyRungPresetMenu()
		{
			ApplyPreset(rung);
		}

		/// <summary>Pushes the shared parts of the ladder into the systems that live outside this asset.</summary>
		public void ApplyToShaders()
		{
			// One rung for the whole galaxy: the volumetric ladder already
			// owns LOD/steps/draw budget, it only needs to hear the switch.
			NebulaQuality.Rung = (NebulaQualityRung)(int)rung;
		}

		void OnValidate()
		{
			backgroundTextureSize = backgroundTextureSize <= 512 ? 512
				: backgroundTextureSize <= 1024 ? 1024 : 2048;
			starCloudInstances = Mathf.Clamp(starCloudInstances, 0, GalaxyBlueprint.MaxStarCount);
			skyStarCount = Mathf.Clamp(skyStarCount, 0, 200000);
			mapStarSprites = Mathf.Clamp(mapStarSprites, 0, GalaxyBlueprint.MaxStarCount);
			localDustCards = Mathf.Clamp(localDustCards, 0, GalaxyBlueprint.MaxStarCount);
			localDustBrightness = Mathf.Clamp01(localDustBrightness);
			cloudStarPxMin = Mathf.Clamp(cloudStarPxMin, 0.25f, 12f);
			cloudStarPxMax = Mathf.Clamp(cloudStarPxMax, cloudStarPxMin, 24f);
			cloudHazePxMin = Mathf.Clamp(cloudHazePxMin, 1f, 24f);
			cloudHazePxMax = Mathf.Clamp(cloudHazePxMax, cloudHazePxMin, 48f);
			glowTaps = glowTaps <= 12 ? 12 : 24;
			if (_appliedRung != rung) ApplyPreset(rung);
			Changed?.Invoke();
		}

		static GalaxyQualitySettings _runtimeDefault;

		/// <summary>
		/// A transient preset for scenes that have not installed the asset.
		/// Mobile detects to the phone ladder; a desktop editor previews Desktop.
		/// </summary>
		public static GalaxyQualitySettings Default
		{
			get
			{
				if (_runtimeDefault == null)
				{
					_runtimeDefault = CreateInstance<GalaxyQualitySettings>();
					_runtimeDefault.name = "GalaxyQuality (runtime default)";
					_runtimeDefault.hideFlags = HideFlags.DontSave;
					bool mobile = Application.isMobilePlatform
						|| Application.platform == RuntimePlatform.Switch;
					_runtimeDefault.ApplyPreset(mobile ? GalaxyQualityRung.Mobile : GalaxyQualityRung.Desktop);
				}
				return _runtimeDefault;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ResetStatics()
		{
			_runtimeDefault = null;
		}
	}
}
