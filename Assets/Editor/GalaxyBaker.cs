using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using OpenFrontier.Galaxy;
// UnityEngine.Debug vs System.Diagnostics.Debug: both imported, so the
// name is pinned explicitly (the editor's compile is the only gate for
// these loose scripts until Unity rebuilds ScriptAssemblies).
using Debug = UnityEngine.Debug;

namespace OpenFrontier.EditorTools
{
	/// <summary>
	/// Bakes the galaxy's face: GalaxyModel.Params rendered per-pixel
	/// into a texture (structure + dust extinction + splatted knots and
	/// field stars), then written to
	/// Assets/OpenFrontier/Textures/Galaxy/GalaxyDisk.png.
	///
	/// WHY BAKE: the photo's continuous body (core glow, arm haze, dust
	/// rifts) cannot come from runtime billboards -- the scrapped build
	/// proved that (200k points of haze soup, dust switched off). The
	/// bake resolves extinction INSIDE the buffer -- dust darkens the
	/// core by construction -- and costs the runtime ONE quad.
	///
	/// PARAMS SOURCE: scene-first. Any loaded GalaxyBackdrop's Params
	/// are the model being baked; with none loaded, the defaults. A
	/// tuned component + a re-run of this menu re-bakes the tuned
	/// galaxy (the installer only bakes when the texture is MISSING --
	/// re-tuning is a deliberate re-bake).
	///
	/// DETERMINISM: every splat decision comes from GalaxyModel's
	/// splitmix64 stream keyed by Params.Seed -- same params, byte-
	/// identical PNG, every run.
	///
	/// EDITOR-ONLY CPU cost: ~4M structure evaluations + ~35k splats,
	/// seconds on a dev box. Nothing here ships to device except the
	/// compressed texture this produces.
	/// </summary>
	public static class GalaxyBaker
	{
		private const string Tag = "[GalaxyBaker]";
		public const string TexturePath = "Assets/OpenFrontier/Textures/Galaxy/GalaxyDisk.png";
		private const string TextureFolder = "Assets/OpenFrontier/Textures/Galaxy";
		private const int Resolution = 2048;

		// Splat budget: field grain + accents. Editor-time only.
		private const int FieldStarCount = 26000;
		private const int BrightKnotCount = 500;
		private const int HiiKnotCount = 900;

		[MenuItem("Open Frontier/Galaxy — Bake Disk Texture (from scene params)")]
		public static void BakeFromMenu()
		{
			GalaxyModel.Params p = ResolveParams();
			Debug.Log($"{Tag} Baking from {(HasSceneBackdrop() ? "the loaded GalaxyBackdrop's params" : "defaults")} (seed {p.Seed}).");
			Bake(p);
		}

		private static bool HasSceneBackdrop()
		{
			return Object.FindAnyObjectByType<Galaxy.GalaxyBackdrop>(FindObjectsInactive.Include) != null;
		}

		/// <summary>The params the bake uses: the scene's backdrop when
		/// one is loaded, otherwise the model defaults.</summary>
		public static GalaxyModel.Params ResolveParams()
		{
			Galaxy.GalaxyBackdrop backdrop = Object.FindAnyObjectByType<Galaxy.GalaxyBackdrop>(FindObjectsInactive.Include);
			return backdrop != null ? backdrop.Model : GalaxyModel.Default(1);
		}

		/// <summary>Installer entry: bake only when the texture is
		/// MISSING (a stale texture is a deliberate re-bake, not an
		/// accident to paper over). Returns false when the bake could
		/// not produce a texture.</summary>
		public static bool EnsureBaked(in GalaxyModel.Params p)
		{
			if (AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) != null)
			{
				return true;
			}
			Debug.Log($"{Tag} {TexturePath} is missing - baking from params (seed {p.Seed}).");
			Bake(p);
			return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) != null;
		}

		public static void Bake(in GalaxyModel.Params p)
		{
			Stopwatch watch = Stopwatch.StartNew();
			int res = Resolution;
			float pixelSpan = 2.1f / res; // model units per pixel (x in [-1.05, 1.05])

			// Linear light accumulation, R/G/B interleaved.
			float[] light = new float[res * res * 3];

			Stopwatch structureWatch = Stopwatch.StartNew();
			float structureSeconds;

			// ---- pass 1: per-pixel structure ----
			for (int py = 0; py < res; py++)
			{
				float z = (py + 0.5f) * pixelSpan - 1.05f;
				for (int px = 0; px < res; px++)
				{
					float x = (px + 0.5f) * pixelSpan - 1.05f;
					GalaxyModel.Face face = GalaxyModel.EvaluateFace(x, z, p);
					Color c = GalaxyModel.FaceToLight(face);
					// Ambient halo: keeps the interior off dead black
					// between the arms (real photos never hit 0).
					// DiskColor, not CoreColor: a warm ambience tinted
					// the whole first preview sepia.
					float r = Mathf.Sqrt(x * x + z * z);
					float ambient = 0.009f * Mathf.Exp(-r / 0.75f);
					c += GalaxyModel.DiskColor * ambient;
					// Rim fade: the quad's edge dissolves over the last
					// ~12% of the radius (no hard rim from any angle).
					// SmoothRange, not Mathf.SmoothStep: the latter
					// interpolates VALUES at t -- the first bake's
					// 1 - SmoothStep(0.90, 1.02, r) returned
					// 1 - [0.90..1.02], multiplying the whole disk by
					// 0.10 and the rim band by less than zero.
					float fade = 1f - GalaxyModel.SmoothRange(0.90f, 1.02f, r);
					c *= fade;

					int o = (py * res + px) * 3;
					light[o] = c.r;
					light[o + 1] = c.g;
					light[o + 2] = c.b;
				}
			}
			structureWatch.Stop();
			structureSeconds = structureWatch.ElapsedMilliseconds / 1000.0f;

			// ---- pass 2: splats (the grain and the accents) ----
			// Positions come from rejection sampling of the density the
			// same math just painted; colours re-evaluate the face at
			// each sample. Uniform-proposal rejection is EXACT.
			float maxDensity = 5.4f; // bulge(2.2)+nucleus(1.4)+bar(1.1)+disk peak
			ulong state = GalaxyModel.Stream(p.Seed);

			// Splat brightness carries the SAME rim fade as the face:
			// grain added past the disk's rim read as a snow-globe
			// boundary in the first preview.
			float SplatFade(float sx, float sz)
			{
				float sr = Mathf.Sqrt(sx * sx + sz * sz);
				// Rim fade + the field grain follows the old-disk
				// falloff: uniform grain to the rim read as a flat grey
				// sheet in the previews.
				float diskFall = 0.25f + 0.75f * Mathf.Exp(-sr / 0.45f);
				return diskFall * (1f - GalaxyModel.SmoothRange(0.85f, 1.0f, sr));
			}

			for (int i = 0; i < FieldStarCount; i++)
			{
				SampleByRejection(ref state, p, maxDensity, out float x, out float z, out GalaxyModel.Face face);
				float w = face.OldTotal + face.Young * 1.6f;
				float youngFrac = Mathf.Clamp01(face.Young / Mathf.Max(w, 1e-5f));
				// Field colour: population mix with a warm/cool drift.
				Color baseCol = Color.Lerp(GalaxyModel.DiskColor, GalaxyModel.ArmColor, youngFrac);
				float drift = GalaxyModel.Unit(ref state);
				baseCol = Color.Lerp(baseCol, GalaxyModel.CoreColor, drift * 0.35f * (1f - youngFrac));
				baseCol = Color.Lerp(baseCol, Color.white, 0.08f);

				float bright = 0.16f + 0.55f * Mathf.Pow(GalaxyModel.Unit(ref state), 2.6f);
				float u = GalaxyModel.Unit(ref state);
				float sizePx = u < 0.86f ? 1.0f : (u < 0.97f ? 2.0f : 3.0f);
				SplatPx(light, res, ModelToPixel(x, res), ModelToPixel(z, res), sizePx, baseCol * (bright * SplatFade(x, z)));
			}

			for (int i = 0; i < BrightKnotCount; i++)
			{
				SampleByRejection(ref state, p, maxDensity, out float x, out float z, out GalaxyModel.Face face);
				float youngFrac = Mathf.Clamp01(face.Young / Mathf.Max(face.OldTotal + face.Young, 1e-5f));
				Color baseCol = Color.Lerp(GalaxyModel.DiskColor, GalaxyModel.ArmColor, youngFrac);
				float bright = 1.2f + 1.6f * Mathf.Pow(GalaxyModel.Unit(ref state), 1.8f);
				float sizePx = 2.2f + 1.3f * GalaxyModel.Unit(ref state);
				float fade = SplatFade(x, z);
				SplatPx(light, res, ModelToPixel(x, res), ModelToPixel(z, res), sizePx, baseCol * (bright * fade));
				// A soft glow pad behind every knot: the photo's stars
				// are never bare points.
				SplatPx(light, res, ModelToPixel(x, res), ModelToPixel(z, res), sizePx * 2.6f, baseCol * (0.25f * bright * fade));
			}

			// HII: rose knots that may only land ON the arms (the
			// accept test is the young density itself). Capped retries
			// so a dust-suppressed seed still terminates.
			int hiiPlaced = 0;
			int attempts = 0;
			int maxAttempts = HiiKnotCount * 40;
			while (hiiPlaced < HiiKnotCount && attempts < maxAttempts)
			{
				attempts++;
				SampleByRejection(ref state, p, maxDensity, out float x, out float z, out GalaxyModel.Face face);
				if (face.Young < 0.35f)
				{
					continue;
				}
				hiiPlaced++;
				float bright = 0.35f + 0.55f * GalaxyModel.Unit(ref state);
				float sizePx = 2.5f + 5.5f * Mathf.Pow(GalaxyModel.Unit(ref state), 1.6f);
				Color hii = GalaxyModel.HiiColor * (bright * SplatFade(x, z));
				SplatPx(light, res, ModelToPixel(x, res), ModelToPixel(z, res), sizePx, hii);
				SplatPx(light, res, ModelToPixel(x, res), ModelToPixel(z, res), 1.2f, hii * 2.2f);
			}
			if (hiiPlaced < HiiKnotCount)
			{
				Debug.LogWarning($"{Tag} HII knots placed {hiiPlaced}/{HiiKnotCount} (retry cap) - check the seed's arm patchiness.");
			}

			// ---- pass 3: tonemap + encode ----
			Color32[] pixels = new Color32[res * res];
			for (int i = 0; i < res * res; i++)
			{
				float r = light[i * 3];
				float g = light[i * 3 + 1];
				float b = light[i * 3 + 2];
				pixels[i] = new Color32(
					Encode(1f - Mathf.Exp(-r)),
					Encode(1f - Mathf.Exp(-g)),
					Encode(1f - Mathf.Exp(-b)),
					255);
			}

			Texture2D texture = new Texture2D(res, res, TextureFormat.RGBA32, false, true);
			texture.SetPixels32(pixels);
			texture.Apply();
			byte[] png = texture.EncodeToPNG();
			Object.DestroyImmediate(texture);

			if (!AssetDatabase.IsValidFolder("Assets/OpenFrontier/Textures"))
			{
				AssetDatabase.CreateFolder("Assets/OpenFrontier", "Textures");
			}
			if (!AssetDatabase.IsValidFolder(TextureFolder))
			{
				AssetDatabase.CreateFolder("Assets/OpenFrontier/Textures", "Galaxy");
			}
			System.IO.File.WriteAllBytes(TexturePath, png);
			AssetDatabase.ImportAsset(TexturePath);
			ConfigureImporter(AssetImporter.GetAtPath(TexturePath) as TextureImporter);
			AssetDatabase.SaveAssets();

			watch.Stop();
			Debug.Log($"{Tag} Baked {res}x{res} disk to {TexturePath} ({png.Length / 1024} KB PNG): " +
				$"structure {structureSeconds:0.0}s, total {watch.ElapsedMilliseconds / 1000.0:0.0}s, " +
				$"splats {FieldStarCount}+{BrightKnotCount}+{hiiPlaced}, seed {p.Seed}.");
		}

		private static byte Encode(float linear)
		{
			float gamma = Mathf.Pow(Mathf.Clamp01(linear), 1f / 2.2f);
			return (byte)Mathf.RoundToInt(gamma * 255f);
		}

		private static float ModelToPixel(float model, int res)
		{
			return (model + 1.05f) / 2.1f * res;
		}

		/// <summary>Uniform-proposal rejection sampling of the face's
		/// light density (exact for any pdf under its max). Seeds x/z
		/// first so the caller can re-evaluate the face at the accepted
		/// point without a second sample pass.</summary>
		private static void SampleByRejection(
			ref ulong state, in GalaxyModel.Params p, float maxDensity,
			out float x, out float z, out GalaxyModel.Face face)
		{
			for (int tries = 0; tries < 64; tries++)
			{
				float u = GalaxyModel.Unit(ref state) * 2.1f - 1.05f;
				float v = GalaxyModel.Unit(ref state) * 2.1f - 1.05f;
				GalaxyModel.Face candidate = GalaxyModel.EvaluateFace(u, v, p);
				float w = candidate.OldTotal + candidate.Young * 1.6f;
				if (GalaxyModel.Unit(ref state) < w / maxDensity)
				{
					x = u;
					z = v;
					face = candidate;
					return;
				}
			}
			// 64 straight rejections: land the sample in the halo (the
			// emptiest legal ground) rather than stalling the bake.
			float a = GalaxyModel.Unit(ref state) * Mathf.PI * 2f;
			float rr = 0.5f + 0.5f * GalaxyModel.Unit(ref state);
			x = Mathf.Cos(a) * rr;
			z = Mathf.Sin(a) * rr;
			face = GalaxyModel.EvaluateFace(x, z, p);
		}

		/// <summary>Additive gaussian splat into the linear buffer.
		/// Weight = exp(-(d/size)^2 * 4.5): visually compact, zero
		/// past ~2.2x size, so bounds stay small.</summary>
		private static void SplatPx(float[] light, int res, float cx, float cy, float size, Color col)
		{
			float radius = size * 2.2f;
			int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
			int x1 = Mathf.Min(res - 1, Mathf.CeilToInt(cx + radius));
			int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
			int y1 = Mathf.Min(res - 1, Mathf.CeilToInt(cy + radius));
			if (col.r <= 0f && col.g <= 0f && col.b <= 0f)
			{
				return;
			}

			for (int py = y0; py <= y1; py++)
			{
				float dy = py + 0.5f - cy;
				for (int px = x0; px <= x1; px++)
				{
					float dx = px + 0.5f - cx;
					float d2 = (dx * dx + dy * dy) / (size * size);
					float w = Mathf.Exp(-d2 * 4.5f);
					int o = (py * res + px) * 3;
					light[o] += col.r * w;
					light[o + 1] += col.g * w;
					light[o + 2] += col.b * w;
				}
			}
		}

		private static void ConfigureImporter(TextureImporter importer)
		{
			if (importer == null)
			{
				Debug.LogError($"{Tag} No TextureImporter for {TexturePath} - the bake wrote a PNG Unity did not import.");
				return;
			}
			importer.mipmapEnabled = true;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.filterMode = FilterMode.Trilinear;
			importer.anisoLevel = 2;
			importer.sRGBTexture = true;

			// Mobile-first: ASTC 6x6 on Android (~2 MB for 2048xRGBA);
			// the editor/desktop keeps the default compressor.
			TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
			android.overridden = true;
			android.maxTextureSize = 2048;
			android.format = TextureImporterFormat.ASTC_6x6;
			android.compressionQuality = 50;
			importer.SetPlatformTextureSettings(android);

			importer.SaveAndReimport();
		}
	}
}
