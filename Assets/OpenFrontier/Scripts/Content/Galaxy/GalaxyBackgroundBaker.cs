using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// CPU bake of the one generated texture the galaxy renderer owns: a square
	/// top-down image of the disc, produced from the blueprint's density field
	/// (painted map or analytic spiral) and palette.
	///
	/// ED has no such texture in its carve: StellarForgeSkyboxMap generates
	/// GalaxyBackgroundSkybox at system entry and DistantMilkyWay projects it
	/// through the galactic plane. This is the rebuild's equivalent, with ED's
	/// GalaxyBackground resolutions (512/1024/2048) and one texture serving
	/// both the band and the dust (dust is the inverted luminance in-shader).
	///
	/// Output is RGBA32 sRGB with mips. RGB is the colour signal; A is only a
	/// geometric disc mask. The band derives coverage from Rec.709 luminance
	/// so faint outer arms survive, and the dust gates on luminance (the dark
	/// side of the band) so the black outside the disc cannot become fog.
	/// </summary>
	public static class GalaxyBackgroundBaker
	{
		public static Texture2D Bake(GalaxyBlueprint blueprint, int size, GalaxyQualityRung rung, float clumpNoise)
		{
			if (blueprint == null) return null;
			size = Mathf.Clamp(size, 256, 2048);
			int seed = blueprint.seed;
			var pixels = new Color32[size * size];
			float inv = 1f / size;

			for (int y = 0; y < size; y++)
			{
				float z = ((y + 0.5f) * inv) * 2f - 1f;
				for (int x = 0; x < size; x++)
				{
					float px = ((x + 0.5f) * inv) * 2f - 1f;
					float r01 = Mathf.Sqrt(px * px + z * z);

					// Domain-warp the density sample before reading it. The
					// painted map is only 512^2 and its arms are smooth; a
					// 5% warp breaks the airbrushed edge into ragged spiral
					// structure without inventing a second galaxy.
					float sx = px;
					float sz = z;
					if (r01 < 1f)
					{
						sx += (ValueNoise(px * 4.1f + 13.7f, z * 4.1f + 7.3f, seed + 71) - 0.5f) * 0.055f;
						sz += (ValueNoise(px * 4.7f + 3.1f, z * 4.7f + 19.9f, seed + 89) - 0.5f) * 0.055f;
					}
					float sr2 = sx * sx + sz * sz;
					float d = (r01 >= 1f || sr2 >= 1f) ? 0f : blueprint.SampleDensity(sx, sz);
					if (r01 < 1f)
						d *= 1f - Mathf.SmoothStep(0.94f, 1f, r01);
					else
						d = 0f;

					// Painted maps ignore the analytic discFalloff, so the
					// outer disc stays as bright as the arms and the band
					// reads as a flat glowing pancake. ED's map dims toward
					// the rim and lets the billboard field carry the edge;
					// this is the painted-map equivalent.
					d *= 1f - 0.45f * Mathf.SmoothStep(0f, 1f, r01);

					// Two octaves of value noise: breaks the analytic/painted
					// field into clumps without inventing structure that the
					// billboard cloud does not share. The same density map
					// drives both, so the band and the cloud cannot disagree.
					float n = ValueNoise(px * 7f, z * 7f, seed) * clumpNoise
						+ ValueNoise(px * 23f, z * 23f, seed + 17) * clumpNoise * 0.35f;
					d = Mathf.Clamp01(d * (0.90f + n) - n * 0.10f);

					// Dust filaments. ED's DistantMilkyWayDust is the inverse
					// of the band's luminance, so the band must actually
					// contain dark structure for the dust layer to reveal.
					// Restrict the filaments to the arms; the inter-arm field
					// is already low and should not become a black web.
					float dustN = ValueNoise(px * 37f, z * 37f, seed + 131) * 0.65f
						+ ValueNoise(px * 67f, z * 67f, seed + 197) * 0.35f;
					float dust = Mathf.SmoothStep(0.52f, 0.74f, dustN)
						* Mathf.SmoothStep(0.06f, 0.45f, d);
					d *= 1f - 0.55f * dust;

					// Sparse blue-white knots: HII regions / young clusters.
					// They give the band a colour axis beyond amber without
					// turning it into a confetti field.
					float knotN = ValueNoise(px * 83f, z * 83f, seed + 211);
					float knot = Mathf.SmoothStep(0.84f, 0.96f, knotN)
						* Mathf.SmoothStep(0.35f, 0.75f, d);

					// Palette matches GalaxyBlueprint.Generate: core -> arm ->
					// edge by normalized radius, brightened by local density.
					// The arm/edge split sits closer to the core than the old
					// 50/50 so the warm amber arms cover most of the disc; the
					// screenshot of ED's own map is amber well past halfway.
					float t = Mathf.Clamp01(r01);
					Color col = t < 0.35f
						? Color.Lerp(blueprint.coreColor, blueprint.armColor, t / 0.35f)
						: Color.Lerp(blueprint.armColor, blueprint.edgeColor, (t - 0.35f) / 0.65f);
					col *= 0.20f + 1.25f * Mathf.Clamp01(d);
					col = Color.Lerp(col, new Color(0.72f, 0.83f, 1.0f), knot * 0.6f);
					col *= 1f + knot * 1.6f;
					// Brightness belongs to the band material's _BandGain;
					// multiplying here as well squared the knob and exposed
					// the dust/glow through a gain they do not own.

					// A is a geometric coverage mask, not the density. The
					// density is already in RGB; multiplying A by it as well
					// made the band quadratic and, worse, let any low-density
					// pixel vanish entirely. ED's dust derives its lanes from
					// the band's luminance, so RGB is the signal and A only
					// says "inside the disc".
					float a = 1f - Mathf.SmoothStep(0.94f, 1f, r01);
					if (r01 >= 1f) col = Color.black;
					pixels[y * size + x] = new Color32(
						(byte)(Mathf.Clamp01(col.r) * 255f),
						(byte)(Mathf.Clamp01(col.g) * 255f),
						(byte)(Mathf.Clamp01(col.b) * 255f),
						(byte)(Mathf.Clamp01(a) * 255f));
				}
			}

			var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
			{
				name = $"GalaxyBackground_{rung}_{size}",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
				anisoLevel = 2,
				hideFlags = HideFlags.HideAndDontSave
			};
			tex.SetPixels32(pixels);
#if UNITY_EDITOR
			// Diagnostic only. Read the uploaded texture, not the managed
			// array that Apply() is allowed to reuse: the old probe ran after
			// Apply and reported 0/1 even when the bake was correct. The
			// band/density/dust now use RGB luminance, so this alpha is only
			// the geometric disc mask; the log verifies it is not empty.
			// Never shipped (editor-only readable copy).
			tex.Apply(true, false);
			Color32[] readback = tex.GetPixels32();
			byte aMin = 255, aMax = 0;
			long aSum = 0;
			for (int i = 0; i < readback.Length; i++)
			{
				byte a = readback[i].a;
				if (a < aMin) aMin = a;
				if (a > aMax) aMax = a;
				aSum += a;
			}
			Debug.Log($"[GalaxyBackdrop] bake alpha min={aMin} max={aMax} avg={(double)aSum / readback.Length:F1} (0-255, readback).");
#else
			tex.Apply(true, true);
#endif
			return tex;
		}

		static float ValueNoise(float x, float z, int seed)
		{
			int xi = Mathf.FloorToInt(x);
			int zi = Mathf.FloorToInt(z);
			float fx = x - xi;
			float fz = z - zi;
			fx = fx * fx * (3f - 2f * fx);
			fz = fz * fz * (3f - 2f * fz);
			float a = Hash(xi, zi, seed);
			float b = Hash(xi + 1, zi, seed);
			float c = Hash(xi, zi + 1, seed);
			float d = Hash(xi + 1, zi + 1, seed);
			return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
		}

		static float Hash(int x, int z, int seed)
		{
			unchecked
			{
				uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041);
				h = (h ^ (h >> 13)) * 1274126177u;
				h ^= h >> 16;
				return (h & 0x00FFFFFFu) / 16777215f;
			}
		}
	}
}
