using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// The galaxy's authorable blueprint: one asset describing the whole
	/// visual band - a top-down density description (an optional painted
	/// texture, otherwise the analytic spiral below) plus shape, palette and
	/// point-count parameters, and a seed. GalaxyInstanceCloud turns it into
	/// an instanced point cloud; nothing is baked.
	///
	/// This is the "one non-programmatic resource" shape: a future builder
	/// (seed-to-spiral generator + painter) authors these assets, and two
	/// machines with the same blueprint always generate the same sky -
	/// everything is deterministic in (seed, params) via a System.Random.
	///
	/// Sampling is a 256x256 density grid + CDF built once per reseed: cells
	/// weighted by density, inverted by binary search, jittered inside the
	/// cell; each point then gets a vertical gaussian whose sigma swells
	/// over the bulge. Painted maps REPLACE the analytic terms entirely and
	/// drive density off their brightest channel; the round disc mask and a
	/// soft rim fade apply to both sources so the band never rings at r=1.
	/// </summary>
	[CreateAssetMenu(menuName = "OpenFrontier/Galaxy Blueprint", fileName = "NewGalaxyBlueprint.asset")]
	public class GalaxyBlueprint : ScriptableObject
	{
		/// <summary>
		/// Sampling grid resolution - matches the shipped GalaxyDensityMap
		/// (512x512) so its GetPixels32 fast path hits. The procedural
		/// fallback pays ~50ms once per reseed at this size (the map path
		/// skips the per-cell spiral math entirely).
		/// </summary>
		public const int GridResolution = 512;

		/// <summary>GalaxyStarSprites.png is an 8x4 sheet of ED star sprites (fx.w indexes cells).</summary>
		public const int SpriteCellsX = 8;
		public const int SpriteCellCount = 32;

		/// <summary>fx.w >= 240 selects the high-res ED lens-flare sprites: haze, then the three bright-star cells.</summary>
		public const float HazeVariant = 241f;
		public const float FlareVariant = 242f;
		public const float SpikeVariant = 243f;
		public const float SpiralVariant = 244f;

		/// <summary>Hard ceiling in one cloud; raise deliberately, mobile-first.</summary>
		public const int MaxStarCount = 48000;

		[Header("Density Source")]
		[Tooltip("Optional 256x256 top-down density map over the disc (READABLE asset required - Read/Write on import). Brightest channel = density. Null = analytic spiral below.")]
		public Texture2D densityMap;

		[Tooltip("Reseed key. Same seed + same params = the same sky, always. Change it via the cloud's context menu (or the per-jump hook, phase 2).")]
		public int seed = 1;

		[Header("Shape")]
		[Tooltip("Disc radius in world units.")]
		public float radius = 3500f;

		[Tooltip("Vertical gaussian sigma of the thin disc, world units.")]
		public float thickness = 70f;

		[Tooltip("Bulge core radius as a fraction of the disc radius.")]
		[Range(0.02f, 0.5f)] public float bulgeRadiusFraction = 0.14f;

		[Tooltip("How many times thicker the bulge is than the thin disc.")]
		[Range(1f, 8f)] public float bulgeThicknessBoost = 2.4f;

		[Tooltip("Bulge density boost - the bright core mass.")]
		[Range(0f, 4f)] public float bulgeStrength = 1.6f;

		[Header("Procedural Spiral (used when no density map)")]
		[Tooltip("Exponential disc falloff - higher means stars hug the centre.")]
		public float discFalloff = 2.1f;

		[Range(1, 6)] public int armCount = 2;

		[Tooltip("Spiral pitch angle in degrees (small = tightly wound, large = open).")]
		[Range(4f, 45f)] public float armPitchDegrees = 13f;

		[Tooltip("Arm half-width in degrees, measured across the spiral angle.")]
		[Range(2f, 40f)] public float armWidthDegrees = 9f;

		[Range(0f, 3f)] public float armContrast = 1.15f;

		[Range(0f, 360f)] public float armPhaseDegrees = 0f;

		[Tooltip("Bar half-length as a fraction of the radius. 0 = no bar (pure spiral).")]
		[Range(0f, 0.6f)] public float barLengthFraction = 0f;

		[Tooltip("Bar half-width as a fraction of the radius (bar runs along +X).")]
		[Range(0.02f, 0.5f)] public float barWidthFraction = 0.1f;

		[Header("Points")]
		[Range(0, MaxStarCount)] public int starCount = 12000;

		[Tooltip("Sprite size range in world units (bulge stars run larger still).")]
		public float sizeMin = 2.2f;
		public float sizeMax = 6.5f;

		[Tooltip("Global brightness multiplier baked into every point colour.")]
		[Range(0f, 4f)] public float brightness = 1f;

		[Header("Haze & Twinkle")]
		[Tooltip("Share of points that become large dim haze sprites - the glow that welds the stars into a band.")]
		[Range(0f, 0.6f)] public float hazeFraction = 0.35f;

		[Tooltip("Haze sprite size range, world units (fewer huge than small).")]
		public float hazeSizeMin = 25f;
		public float hazeSizeMax = 90f;

		[Tooltip("Haze peak alpha - additive stacking builds the band's glow. Keep low; it stacks.")]
		[Range(0f, 0.3f)] public float hazeAlpha = 0.05f;

		[Tooltip("Twinkle amplitude range for stars (haze is steady).")]
		public float twinkleAmpMin = 0.06f;
		public float twinkleAmpMax = 0.22f;

		[Tooltip("Twinkle speed range, radians/second (unscaled time - runs while the stage is paused).")]
		public float twinkleSpeedMin = 1.2f;
		public float twinkleSpeedMax = 3.8f;

		/// <summary>Bump when the default look retunes; older assets migrate once via OnValidate / Generate.</summary>
		internal const int CurrentLookVersion = 3;

		[SerializeField, HideInInspector] int lookVersion;

		[Header("Palette")]
		[Tooltip("Core colour (warm).")]
		public Color coreColor = new Color(1.00f, 0.90f, 0.74f);

		[Tooltip("Mid/arm colour (cool white).")]
		public Color armColor = new Color(0.85f, 0.90f, 1.00f);

		[Tooltip("Outer rim colour (dim blue).")]
		public Color edgeColor = new Color(0.50f, 0.60f, 0.85f);

		[Tooltip("Warm<->cool temperature spread. Stars stay near-white; this only tints them.")]
		[Range(0f, 0.5f)] public float colorJitter = 0.09f;

		/// <summary>True when the blueprint samples a painted map instead of the analytic spiral.</summary>
		public bool HasDensityMap => densityMap != null;

		/// <summary>
		/// Top-down density of the analytic spiral at a point on the unit disc
		/// (x/z in -1..1). Public because the phase-4 painter will render
		/// exactly this function into a texture asset.
		/// </summary>
		public float ProceduralDensity(float x, float z)
		{
			float r01 = Mathf.Sqrt(x * x + z * z);
			if (r01 >= 1f) return 0f;

			// exponential disc - the base mass
			float density = Mathf.Exp(-r01 * discFalloff);

			// logarithmic spiral arms: arm angle advances with ln(r), pitch
			// sets how fast, each arm offset evenly around the circle
			float twist = 1f / Mathf.Tan(armPitchDegrees * Mathf.Deg2Rad);
			float armPhase = armPhaseDegrees * Mathf.Deg2Rad;
			float armWidthRad = Mathf.Max(armWidthDegrees, 0.5f) * Mathf.Deg2Rad;
			float armSum = 0f;
			for (int k = 0; k < armCount; k++)
			{
				float armAngle = twist * Mathf.Log(Mathf.Max(r01, 0.04f))
					+ armPhase + k * (Mathf.PI * 2f / armCount);
				float diff = WrapAngle(Mathf.Atan2(z, x) - armAngle);
				armSum += Mathf.Exp(-0.5f * Sq(diff / armWidthRad));
			}
			// arms live in the disc proper - fade them out over the bulge and the rim
			float armMask = Mathf.SmoothStep(0.08f, 0.3f, r01) * (1f - Mathf.SmoothStep(0.75f, 0.98f, r01));
			density += armSum / armCount * armContrast * armMask;

			// central bulge
			density += bulgeStrength * Mathf.Exp(-Sq(r01 / Mathf.Max(bulgeRadiusFraction, 0.001f)));

			// optional central bar along +X: a flattened gaussian
			if (barLengthFraction > 0.001f)
			{
				float barL = Mathf.Max(barLengthFraction, 0.001f);
				float barW = Mathf.Max(barWidthFraction, 0.001f);
				density += Mathf.Exp(-(Sq(x / barL) + Sq(z / barW)));
			}

			return Mathf.Max(0f, density);
		}

		/// <summary>
		/// Vertical gaussian sigma at a normalized radius - thin disc, swollen bulge.
		/// </summary>
		public float VerticalSigma(float r01)
		{
			return thickness * (1f + bulgeThicknessBoost * Mathf.Exp(-Sq(r01 / Mathf.Max(bulgeRadiusFraction, 0.001f))));
		}

		/// <summary>
		/// Samples the galaxy. Fills positionsOut (CLOUD-LOCAL: disc in x/z, y
		/// up), colorsOut (linear rgb, alpha = per-point strength), sizesOut
		/// (sprite size, world units) and fxOut (phase, twinkle amplitude,
		/// twinkle speed, sprite variant), returning how many points were
		/// written (<= starCount and <= each array's length). Deterministic in
		/// (seedOverride, params). A few ms for 12k points.
		/// </summary>
		public int Generate(int seedOverride, Vector3[] positionsOut, Color[] colorsOut, float[] sizesOut, Vector4[] fxOut)
		{
			if (positionsOut == null || colorsOut == null || sizesOut == null || fxOut == null) return 0;
			int count = Mathf.Min(Mathf.Min(starCount, positionsOut.Length),
				Mathf.Min(Mathf.Min(colorsOut.Length, sizesOut.Length), fxOut.Length));
			if (count <= 0) return 0;

			// safety net for builds (OnValidate is editor-only): old assets
			// pick the current look up once, at first use
			if (lookVersion < CurrentLookVersion) ApplyDefaultLook();

			float maxWeight;
			float[] weights = BuildWeightGrid(out maxWeight);

			// cumulative distribution: cell i owns (cdf[i], cdf[i+1]]
			var cdf = new double[weights.Length + 1];
			for (int i = 0; i < weights.Length; i++) cdf[i + 1] = cdf[i] + weights[i];
			double total = cdf[weights.Length];
			if (total <= 0.0) return 0; // empty blueprint - e.g. an all-black map

			var rng = new System.Random(seedOverride);
			float bulgeFrac = Mathf.Max(bulgeRadiusFraction, 0.001f);

			// haze walks the SAME density field as the stars - the band's glow
			// follows its structure; stars claim the front of the array
			int starPoints = Mathf.Clamp(
				count - Mathf.RoundToInt(count * Mathf.Clamp01(hazeFraction)), 0, count);

			for (int i = 0; i < count; i++)
			{
				// ---- sample the disc: invert the CDF, jitter inside the cell
				double u = rng.NextDouble() * total;
				int lo = 0, hi = weights.Length; // largest lo with cdf[lo] <= u
				while (hi - lo > 1)
				{
					int mid = (lo + hi) >> 1;
					if (cdf[mid] <= u) lo = mid; else hi = mid;
				}
				int cellX = lo % GridResolution;
				int cellY = lo / GridResolution;
				float fx01 = (cellX + (float)rng.NextDouble()) / GridResolution;
				float fy01 = (cellY + (float)rng.NextDouble()) / GridResolution;
				float px = fx01 * 2f - 1f;
				float pz = fy01 * 2f - 1f;
				float r01 = Mathf.Sqrt(px * px + pz * pz);
				if (r01 > 1f) { float s = 1f / r01; px *= s; pz *= s; r01 = 1f; }

				// ---- vertical: box-muller gaussian, sigma swells over the bulge
				double u1 = Math.Max(rng.NextDouble(), 1e-12);
				double u2 = rng.NextDouble();
				float y = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2))
					* VerticalSigma(r01);
				y = Mathf.Clamp(y, -radius * 0.5f, radius * 0.5f);

				// ---- palette: core -> arm -> edge by radius
				float t = Mathf.Clamp01(r01);
				Color c = t < 0.35f
					? Color.Lerp(coreColor, armColor, t / 0.35f)
					: Color.Lerp(armColor, edgeColor, (t - 0.35f) / 0.65f);

				float dens = maxWeight > 0f ? Mathf.Clamp01(weights[lo] / maxWeight) : 0f;
				float rJitter = (float)rng.NextDouble();

				if (i < starPoints)
				{
					// ---- a star: denser regions brighter; temperature jitter
					// slides the star warm/cool AROUND white (correlated, not
					// the per-channel rainbow of the first pass)
					float boost = brightness * (0.85f + 0.35f * dens);
					float warm = (rJitter - 0.5f) * 2f * colorJitter;
					c.r *= boost * (1f + warm);
					c.g *= boost * (1f + warm * 0.3f);
					c.b *= boost * (1f - warm * 0.8f);
					c.a = Mathf.Clamp01(0.55f + 0.45f * dens) * (0.85f + 0.15f * (float)rng.NextDouble());

					float size = Mathf.Lerp(sizeMin, sizeMax, (float)rng.NextDouble());
					size *= 0.9f + 0.9f * Mathf.Exp(-Sq(r01 / bulgeFrac)); // bulge stars run larger

					// sprite variant: a random cell of the ED star sheet
					// (GalaxyStarSprites, 8x4), except the brightest few,
					// which draw the high-res ED lens-flare cells - heavy
					// flare, diffraction spikes, or (rarely) the spiral flare
					float variant;
					if (dens > 0.85f && rng.NextDouble() < 0.4)
					{
						double roll = rng.NextDouble();
						variant = roll < 0.55 ? FlareVariant
							: roll < 0.9 ? SpikeVariant : SpiralVariant;
					}
					else
					{
						variant = (float)rng.Next(SpriteCellCount);
					}
					if (variant > 240f) size *= 1.25f;

					fxOut[i] = new Vector4(
						(float)(rng.NextDouble() * Math.PI * 2.0),
						Mathf.Lerp(twinkleAmpMin, twinkleAmpMax, (float)rng.NextDouble()),
						Mathf.Lerp(twinkleSpeedMin, twinkleSpeedMax, (float)rng.NextDouble()),
						variant);
					sizesOut[i] = size;
				}
				else
				{
					// ---- haze: large, very dim, steady - additive stacking
					// welds the stars into a continuous band (the ED dust tier)
					float boost = brightness * 0.9f;
					float warm = (rJitter - 0.5f) * colorJitter;
					c.r *= boost * (1f + warm);
					c.g *= boost * (1f + warm * 0.3f);
					c.b *= boost * (1f - warm * 0.8f);
					c.a = hazeAlpha * (0.4f + 0.6f * dens);

					// more small than huge - the square biases toward the floor
					float size = Mathf.Lerp(hazeSizeMin, hazeSizeMax, rJitter * rJitter);
					size *= 0.9f + 0.9f * Mathf.Exp(-Sq(r01 / bulgeFrac));

					fxOut[i] = new Vector4(0f, 0f, 0f, HazeVariant); // haze profile, no twinkle
					sizesOut[i] = size;
				}

				positionsOut[i] = new Vector3(px * radius, y, pz * radius);
				colorsOut[i] = c;
			}
			return count;
		}

		/// <summary>
		/// The 256x256 density grid: painted map (brightest channel) or the
		/// analytic spiral. The round disc mask and the soft rim fade apply
		/// to BOTH sources - the galaxy is round even when the map isn't.
		/// </summary>
		float[] BuildWeightGrid(out float maxWeight)
		{
			var weights = new float[GridResolution * GridResolution];
			maxWeight = 0f;

			Color32[] mapPixels = null;
			if (HasDensityMap)
			{
				try { mapPixels = densityMap.GetPixels32(); }
				catch (Exception e)
				{
					Debug.LogWarning($"[GalaxyBlueprint] '{name}': density map is not readable, falling back to the analytic spiral. Enable Read/Write on the texture asset. ({e.GetType().Name})", this);
				}
			}
			bool fastMap = mapPixels != null && mapPixels.Length == weights.Length;

			for (int y = 0; y < GridResolution; y++)
			{
				for (int x = 0; x < GridResolution; x++)
				{
					float cx = (x + 0.5f) / GridResolution * 2f - 1f;
					float cz = (y + 0.5f) / GridResolution * 2f - 1f;
					float r01 = Mathf.Sqrt(cx * cx + cz * cz);

					float d;
					if (fastMap)
					{
						Color32 p = mapPixels[y * GridResolution + x];
						d = Mathf.Max(p.r, Mathf.Max(p.g, p.b)) / 255f;
					}
					else if (mapPixels != null)
					{
						// readable but not 256x256 - sample it as a full-square map
						Color p = densityMap.GetPixelBilinear(
							(x + 0.5f) / GridResolution, (y + 0.5f) / GridResolution);
						d = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
					}
					else
					{
						d = ProceduralDensity(cx, cz);
					}

					if (r01 >= 1f) d = 0f;
					else d *= 1f - Mathf.SmoothStep(0.96f, 1f, r01);

					weights[y * GridResolution + x] = d;
					if (d > maxWeight) maxWeight = d;
				}
			}
			return weights;
		}

		void OnValidate()
		{
			if (lookVersion < CurrentLookVersion) ApplyDefaultLook();
			radius = Mathf.Max(radius, 1f);
			thickness = Mathf.Max(thickness, 0.1f);
			sizeMin = Mathf.Max(sizeMin, 0.01f);
			if (sizeMax < sizeMin) sizeMax = sizeMin;
			starCount = Mathf.Clamp(starCount, 0, MaxStarCount);
			hazeFraction = Mathf.Clamp01(hazeFraction);
			if (hazeSizeMax < hazeSizeMin) hazeSizeMax = hazeSizeMin;
			if (twinkleAmpMax < twinkleAmpMin) twinkleAmpMax = twinkleAmpMin;
			if (twinkleSpeedMax < twinkleSpeedMin) twinkleSpeedMax = twinkleSpeedMin;
		}

		[ContextMenu("Reset To Default Look")]
		public void ResetToDefaultLook()
		{
			ApplyDefaultLook();
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		/// <summary>
		/// The look-version 3 retune: the procedural fallback gets real arm
		/// contrast (broader, slower-winding arms) and a tighter haze tier;
		/// stars now draw from the ED sprite sheet. Existing assets migrate
		/// once via lookVersion (OnValidate + the Generate guard for builds);
		/// this is also the context-menu reset.
		/// </summary>
		void ApplyDefaultLook()
		{
			coreColor = new Color(1.00f, 0.90f, 0.74f);
			armColor = new Color(0.85f, 0.90f, 1.00f);
			edgeColor = new Color(0.50f, 0.60f, 0.85f);
			colorJitter = 0.09f;
			brightness = 1f;
			discFalloff = 1.8f;
			armPitchDegrees = 22f;
			armWidthDegrees = 7f;
			armContrast = 1.6f;
			hazeFraction = 0.3f;
			hazeSizeMin = 18f;
			hazeSizeMax = 60f;
			hazeAlpha = 0.05f;
			twinkleAmpMin = 0.06f;
			twinkleAmpMax = 0.22f;
			twinkleSpeedMin = 1.2f;
			twinkleSpeedMax = 3.8f;
			lookVersion = CurrentLookVersion;
		}

		static float Sq(float v) { return v * v; }

		static float WrapAngle(float a)
		{
			return Mathf.Repeat(a + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;
		}
	}
}
