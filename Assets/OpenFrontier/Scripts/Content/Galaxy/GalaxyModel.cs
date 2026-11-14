using UnityEngine;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// The galaxy's model: ONE source of truth for what the galaxy looks
	/// like, shared by the editor bake (GalaxyBaker renders it into a
	/// texture) and the runtime dressing (GalaxyBackdrop / GalaxyLocalStars
	/// place their stars on the SAME structures the texture painted).
	///
	/// Model space: the disk face is the XZ plane, unit radius (1.0 = the
	/// rim), Y in the same unit (the disk is thin: sigma_y ~0.013). The
	/// dressing root scales this to world (x3000 by default).
	///
	/// The realism argument (vs the scrapped 200k-point build): a Milky
	/// Way photo is CONTINUOUS structure -- the core glow, the arm haze
	/// and above all the dust are not points. The bake renders this model
	/// per-pixel (smooth, correct occlusion by construction: dust
	/// darkens the core inside the same buffer), and only the DISCRETE
	/// populations -- hero stars, HII knots, the near-field shell -- are
	/// billboards. Structure lives in the texture; grain and parallax
	/// live in the stars.
	///
	/// Populations, in the order they matter:
	/// - Bulge + nucleus + bar: old, WARM (golden). The photo lives or
	///   dies here. Sérsic-ish power law, not a gaussian blob.
	/// - Two MAJOR arms (the Milky Way's Scutum-Centaurus / Perseus) with
	///   two minor companions: young BLUE-WHITE knots on a patchy band,
	///   launched from the bar ends (the crest constant is derived from
	///   the bar, so arms grow out of the bar -- one model, no seam).
	/// - Dust: lanes riding the arms' inner side + the great rift
	///   crossing the core, fbm-fractured. Dust is EXTINCTION: it
	///   multiplies light away, redder than it darkens (tau_blue >
	///   tau_red) -- the single most load-bearing realism rule.
	/// - Old disk: pale warm sheet everywhere between the arms.
	///
	/// Determinism: every random DECISION comes from splitmix64 streams
	/// seeded by Params.Seed (integer math only) -- same seed, same
	/// galaxy, every platform. Float shapes are scenery.
	/// </summary>
	public static class GalaxyModel
	{
		// ------------------------------------------------------------
		// Parameters
		// ------------------------------------------------------------

		/// <summary>Tunable structure. Serialised on GalaxyBackdrop so
		/// the bake and the runtime read the SAME knobs; the bake tool
		/// takes its params from the scene's backdrop when one exists,
		/// so a tuned galaxy re-bakes as tuned.</summary>
		public struct Params
		{
			public int Seed;

			[Tooltip("Log-spiral pitch angle in degrees (the Milky Way is ~12-13).")]
			public float PitchDegrees;

			[Tooltip("Bar gaussian sigma along the LONG axis (visible bar ~2.2 sigma).")]
			public float BarLength;

			[Tooltip("Bar gaussian sigma across the SHORT axis.")]
			public float BarWidth;

			[Tooltip("Bar position angle in degrees (also seeds where the major arms start).")]
			public float BarAngleDegrees;

			[Tooltip("Bulge effective radius (Sérsic-like), unit-disk radii.")]
			public float BulgeRadius;

			[Tooltip("Old-disk exponential scale radius, unit-disk radii.")]
			public float DiskScale;

			[Tooltip("Arm band sharpness: higher = thinner, crisper crests.")]
			public float ArmSharpness;

			[Tooltip("Large-scale clumpiness of the arm glow (0 = smooth, 1 = patchy).")]
			public float Patchiness;

			[Tooltip("Global dust opacity multiplier (0 = no rifts).")]
			public float DustStrength;

			[Tooltip("Great-rift strength multiplier (the lane crossing the core).")]
			public float RiftStrength;
		}

		public static Params Default(int seed)
		{
			Params p = default;
			p.Seed = seed;
			p.PitchDegrees = 13f;
			p.BarLength = 0.11f;
			p.BarWidth = 0.04f;
			p.BarAngleDegrees = 25f;
			p.BulgeRadius = 0.10f;
			p.DiskScale = 0.36f;
			p.ArmSharpness = 3.0f;
			p.Patchiness = 1.0f;
			p.DustStrength = 1.0f;
			p.RiftStrength = 1.0f;
			return p;
		}

		// ------------------------------------------------------------
		// Palette (linear-ish, HDR-friendly; the quad material adds it
		// over black, so these are the actual light colours)
		// ------------------------------------------------------------

		/// <summary>Bulge/bar: old warm population.</summary>
		public static readonly Color CoreColor = new Color(1.00f, 0.84f, 0.62f);

		/// <summary>Old disk sheet.</summary>
		public static readonly Color DiskColor = new Color(1.00f, 0.92f, 0.80f);

		/// <summary>Young arm knots: hot blue-white.</summary>
		public static readonly Color ArmColor = new Color(0.74f, 0.85f, 1.00f);

		/// <summary>HII regions: ionised hydrogen rose.</summary>
		public static readonly Color HiiColor = new Color(1.00f, 0.40f, 0.46f);

		/// <summary>Extinction optical depth per unit Dust at R/G/B.
		/// Deliberately NOT grey: blue is absorbed ~2x red, so rifts
		/// redden what they dim -- the photo's signature.</summary>
		public static readonly Vector3 DustTau = new Vector3(2.6f, 3.6f, 5.0f);

		// ------------------------------------------------------------
		// Structure: the face of the galaxy, evaluated per point
		// ------------------------------------------------------------

		/// <summary>Density of each population at a point on the face
		/// (y = 0 plane). The bake maps these to light; the placer uses
		/// Dust for rift rejection and the population ratios for colour.</summary>
		public struct Face
		{
			public float Bulge;
			public float Bar;
			public float DiskOld;
			public float Young;
			public float Dust;

			/// <summary>All old-population light (warm) in one number.</summary>
			public float OldTotal => Bulge + Bar + DiskOld;
		}

		/// <summary>Minor-arm mass fraction of a major arm.</summary>
		private const float MinorArmMass = 0.45f;

		/// <summary>The spiral-crest phase constant: crests sit where
		/// 2*theta + winding*ln(r) == CrestPhase(p) + 2*pi*k. Derived
		/// from the BAR so the major arms grow out of the bar ends:
		/// at r = bar end, theta = bar angle must satisfy the crest.</summary>
		public static float CrestPhase(in Params p)
		{
			float winding = Winding(p);
			float barEnd = Mathf.Max(2.2f * p.BarLength, 0.02f);
			return 2f * p.BarAngleDegrees * Mathf.Deg2Rad + winding * Mathf.Log(barEnd);
		}

		/// <summary>winding = -2 * tan(pitch): the crest theta drifts
		/// dtheta/dlnr = winding/2, which is a log spiral of pitch
		/// `PitchDegrees` (sign = trailing).</summary>
		private static float Winding(in Params p)
		{
			return -2f * Mathf.Tan(Mathf.Clamp(p.PitchDegrees, 2f, 40f) * Mathf.Deg2Rad);
		}

		/// <summary>Value-noise fbm on the face: deterministic integer
		/// hash lattice, quintic interpolation, amplitude halving.</summary>
		public static float Fbm(float u, float v, int octaves, uint seed)
		{
			float sum = 0f;
			float amp = 0.5f;
			float fu = u;
			float fv = v;
			for (int i = 0; i < octaves; i++)
			{
				sum += amp * Noise(fu, fv, seed + (uint)i * 0x68E31DA4u);
				amp *= 0.5f;
				fu *= 2.03f;
				fv *= 1.97f;
			}
			return sum / (1f - amp * 2f); // normalise the octave budget
		}

		private static float Noise(float u, float v, uint seed)
		{
			float uFloor = Mathf.Floor(u);
			float vFloor = Mathf.Floor(v);
			float fu = u - uFloor;
			float fv = v - vFloor;
			// Quintic smoothstep on both axes.
			float sx = fu * fu * fu * (fu * (fu * 6f - 15f) + 10f);
			float sy = fv * fv * fv * (fv * (fv * 6f - 15f) + 10f);
			uint xi = (uint)(int)uFloor;
			uint yi = (uint)(int)vFloor;
			float n00 = HashToFloat(xi, yi, seed);
			float n10 = HashToFloat(xi + 1u, yi, seed);
			float n01 = HashToFloat(xi, yi + 1u, seed);
			float n11 = HashToFloat(xi + 1u, yi + 1u, seed);
			return Mathf.Lerp(Mathf.Lerp(n00, n10, sx), Mathf.Lerp(n01, n11, sx), sy);
		}

		private static float HashToFloat(uint x, uint y, uint seed)
		{
			uint h = seed ^ (x * 0x9E3779B9u) ^ (y * 0x85EBCA77u);
			h ^= h >> 16;
			h *= 0x7FEB352Du;
			h ^= h >> 15;
			h *= 0x846CA68Bu;
			h ^= h >> 16;
			return h * (1f / 4294967296f);
		}

		/// <summary>Structure at (x, z) on the face. r is clamped at
		/// 1.25 -- beyond the rim every population is essentially 0 and
		/// the clamped math stays finite.</summary>
		public static Face EvaluateFace(float x, float z, in Params p)
		{
			Face f = default;
			float r = Mathf.Sqrt(x * x + z * z);
			float rC = Mathf.Clamp(r, 1e-4f, 1.25f);
			float theta = Mathf.Atan2(z, x);
			float winding = Winding(p);
			float phase = 2f * theta + winding * Mathf.Log(rC);

			// --- old disk: exponential sheet ---
			f.DiskOld = 0.50f * Mathf.Exp(-r / Mathf.Max(p.DiskScale, 0.05f));

			// --- bulge: Sérsic n~2 approximated by a sqrt-law falloff,
			// plus a hot nuclear kernel ---
			float re = Mathf.Max(p.BulgeRadius, 0.02f);
			f.Bulge = 2.2f * Mathf.Exp(-3.7f * Mathf.Sqrt(rC / re))
				+ 1.4f * Mathf.Exp(-(rC * rC) / (0.035f * 0.035f));

			// --- bar: rotated tri-axial gaussian ---
			float ang = p.BarAngleDegrees * Mathf.Deg2Rad;
			float cs = Mathf.Cos(ang);
			float sn = Mathf.Sin(ang);
			float bx = x * cs - z * sn;
			float by = x * sn + z * cs;
			float sw = Mathf.Max(p.BarWidth, 0.01f);
			float sl = Mathf.Max(p.BarLength, 0.01f);
			f.Bar = 1.1f * Mathf.Exp(
				-(bx * bx) / (2f * sw * sw) - (by * by) / (2f * sl * sl));

			// --- arms: crest bands off the bar end. Major pair at the
			// crest phase, minor pair half a turn away, both suppressed
			// near the centre where the bulge swamps them, both
			// patchified by fbm (a real arm is clumpy, not a smooth
			// ribbon). ---
			float crest = CrestPhase(p);
			float dMajor = Mathf.Sin(phase - crest);
			float dMinor = Mathf.Sin(phase - crest + Mathf.PI);
			float centerFade = Mathf.SmoothStep(0.10f, 0.26f, rC);
			float patch = 0.45f + 0.9f * Fbm(rC * 5.5f, phase * 1.6f, 3, (uint)p.Seed + 0x51ED27u);
			float clump = 0.7f + 0.6f * Fbm(rC * 14f, phase * 4f, 2, (uint)p.Seed + 0xA3D9u);
			float clumpiness = Mathf.Lerp(1f, patch * clump, Mathf.Clamp01(p.Patchiness));
			f.Young = centerFade * clumpiness
				* (Mathf.Pow(Mathf.Max(dMajor, 0f), Mathf.Max(p.ArmSharpness, 1f))
					+ MinorArmMass * Mathf.Pow(Mathf.Max(dMinor, 0f), Mathf.Max(p.ArmSharpness, 1f)));

			// --- dust: lanes riding just inside the arm crests (dust
			// piles on the arm's inner edge) + the great rift crossing
			// the core. Both fbm-fractured; the rift is strongest over
			// the bulge. Dust is an optical DEPTH, consumers convert to
			// transmission. ---
			float lane = Mathf.Pow(Mathf.Max(dMajor, 0f), Mathf.Max(p.ArmSharpness, 1f) * 0.55f)
				+ 0.4f * Mathf.Pow(Mathf.Max(dMinor, 0f), Mathf.Max(p.ArmSharpness, 1f) * 0.55f);
			float laneFractal = 0.35f + 0.85f * Fbm(rC * 3.1f, phase * 1.1f, 4, (uint)p.Seed + 0x7C95u);
			float psi = ang + 0.35f;
			float riftDist = Mathf.Abs(x * Mathf.Sin(psi) - z * Mathf.Cos(psi));
			float rift = (0.75f + 0.45f * Fbm(x * 4.4f, z * 4.4f, 3, (uint)p.Seed + 0x2B1Fu))
				* (0.55f + 0.50f * Mathf.Exp(-r * 2f))
				* Mathf.Exp(-(riftDist * riftDist) / (0.10f * 0.10f));
			f.Dust = Mathf.Clamp(
				(lane * laneFractal + rift * p.RiftStrength) * Mathf.Max(p.DustStrength, 0f),
				0f, 1.6f);

			return f;
		}

		/// <summary>Face light -> linear RGB (pre-exposure). The one
		/// place population density becomes COLOUR, shared by bake and
		/// runtime so the texture and the stars agree.</summary>
		public static Color FaceToLight(in Face f)
		{
			Color c = CoreColor * f.Bulge;
			c += CoreColor * (f.Bar * 0.9f);
			c += DiskColor * f.DiskOld;
			c += ArmColor * (f.Young * 0.55f);
			// Beer-Lambert extinction through the dust, per channel --
			// the reddening rule.
			c.r *= Mathf.Exp(-f.Dust * DustTau.x);
			c.g *= Mathf.Exp(-f.Dust * DustTau.y);
			c.b *= Mathf.Exp(-f.Dust * DustTau.z);
			return c;
		}

		// ------------------------------------------------------------
		// Deterministic RNG (splitmix64 -- carried over from the scrapped
		// GalaxyDistribution, proven in two builds)
		// ------------------------------------------------------------

		private const ulong Golden = 0x9E3779B97F4A7C15UL;

		public static ulong Stream(int seed)
		{
			return (ulong)seed * 0x2545F4914F6CDD1DUL + Golden;
		}

		public static ulong Next(ref ulong state)
		{
			state += Golden;
			ulong z = state;
			z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
			z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
			return z ^ (z >> 31);
		}

		public static float Unit(ref ulong state)
		{
			return (float)(Next(ref state) >> 40) * (1f / 16777216f);
		}

		public static float Gaussian(ref ulong state, float sigma)
		{
			float u1 = Mathf.Max(Unit(ref state), 1e-7f);
			float u2 = Unit(ref state);
			return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2) * sigma;
		}

		/// <summary>Same cell-seeding contract as before: the same
		/// spatial cell of the same base seed is the same sky.</summary>
		public static ulong CellSeed(int baseSeed, int cx, int cy, int cz)
		{
			ulong state = Stream(baseSeed)
				^ ((ulong)(uint)cx * 0x9E3779B97F4A7C15UL)
				^ ((ulong)(uint)cy * 0xBF58476D1CE4E5B9UL)
				^ ((ulong)(uint)cz * 0x94D049BB133111EBUL);
			return Next(ref state);
		}

		// ------------------------------------------------------------
		// Runtime placement: hero stars on the model
		// ------------------------------------------------------------

		/// <summary>One discrete star: model-space position (unit-disk
		/// radii), view-quad size (model units) and premultiplied
		/// colour (palette x brightness).</summary>
		public struct Star
		{
			public Vector3 Pos;
			public float Size;
			public Color Col;
		}

		/// <summary>Population shares of the hero budget. The HII share
		/// is the photo's rose accent; halo keeps the rim alive.</summary>
		private const float ArmShare = 0.52f;
		private const float CoreShare = 0.26f;
		private const float DiskShare = 0.12f;
		private const float HaloShare = 0.07f; // HiiShare = the remainder

		/// <summary>Vertical sigmas, MODEL units (x3000 world at the
		/// standard scale): thin young disk, thicker old sheet, fat
		/// core, round halo.</summary>
		private const float SigmaArmY = 0.013f;
		private const float SigmaDiskY = 0.020f;
		private const float SigmaCoreY = 0.045f;
		private const float SigmaHaloY = 0.30f;

		/// <summary>Dust rejection threshold for placement: a hero star
		/// landing inside a rift would punch a hole in the silhouette;
		/// up to tries re-rolls, then accepts (never stall).</summary>
		private const float DustReject = 0.55f;
		private const int DustTries = 4;

		/// <summary>Fill <paramref name="stars"/> with <paramref name="count"/>
		/// deterministic hero stars on the model. Positions carry NO
		/// transform: the dressing root scales model to world.</summary>
		public static void PlaceHeroStars(in Params p, int count, Star[] stars)
		{
			ulong state = Stream(p.Seed);
			for (int i = 0; i < count; i++)
			{
				float pick = Unit(ref state);
				if (pick < ArmShare)
				{
					stars[i] = PlaceArmStar(ref state, p, onMinor: false);
				}
				else if (pick < ArmShare + CoreShare)
				{
					stars[i] = PlaceCoreStar(ref state, p);
				}
				else if (pick < ArmShare + CoreShare + DiskShare)
				{
					stars[i] = PlaceDiskStar(ref state, p);
				}
				else if (pick < ArmShare + CoreShare + DiskShare + HaloShare)
				{
					stars[i] = PlaceHaloStar(ref state, p);
				}
				else
				{
					// HII knot: rose glow riding an arm crest, tighter
					// jitter than field stars, bigger quad, dimmer.
					Star s = PlaceArmStar(ref state, p, onMinor: true);
					s.Size *= 2.6f;
					float bright = 0.30f + 0.55f * Mathf.Pow(Unit(ref state), 1.6f);
					s.Col = HiiColor * bright;
					stars[i] = s;
				}
			}
		}

		/// <summary>One star on an arm crest of the pair; HII knots use
		/// the same geometry (their callers pass onMinor:true for the
		/// minor pair's crest).</summary>
		private static Star PlaceArmStar(ref ulong state, in Params p, bool onMinor)
		{
			// Radius: truncated exponential over the arm span (beyond
			// the rim the crest keeps coiling but we stop at 1.0).
			float scale = Mathf.Max(p.DiskScale, 0.05f);
			float u = Unit(ref state);
			float r = 0.15f - scale * Mathf.Log(1f - u * (1f - Mathf.Exp(-(1.0f - 0.15f) / scale)));
			r = Mathf.Clamp(r, 0.15f, 1.0f);

			float winding = Winding(p);
			float crest = CrestPhase(p);
			// Solve the crest for theta at this radius; k picks which
			// of the pair (0/1), minor arms offset half a turn.
			float spread = Gaussian(ref state, 0.055f + 0.05f * Unit(ref state));
			float jitter = (Fbm(r * 6f, Unit(ref state) * 40f, 2, (uint)p.Seed + 0x11u) - 0.5f) * 0.10f;
			float phaseTarget = crest + (onMinor ? Mathf.PI : 0f)
				+ ((Unit(ref state) < 0.5f) ? 0f : 2f * Mathf.PI);
			float theta = 0.5f * (phaseTarget - winding * Mathf.Log(r)) + spread + jitter;

			Vector3 pos = default;
			float dust = 0f;
			for (int tries = DustTries; tries >= 0; tries--)
			{
				pos = new Vector3(r * Mathf.Cos(theta), Gaussian(ref state, SigmaArmY), r * Mathf.Sin(theta));
				dust = EvaluateFace(pos.x, pos.z, p).Dust;
				if (dust <= DustReject)
				{
					break;
				}
				// Re-roll sideways along the crest, same radius.
				theta += Gaussian(ref state, 0.08f);
			}

			float sizeT = Mathf.Pow(Unit(ref state), 2.2f);
			float size = Mathf.Lerp(0.0005f, 0.0047f, sizeT);
			// A handful of true heroes per arm population.
			if (Unit(ref state) < 0.01f)
			{
				size = Mathf.Lerp(size, 0.010f, Unit(ref state));
			}

			float bright = 0.25f + 2.2f * Mathf.Pow(Unit(ref state), 2.6f);
			// Colour: the young palette with per-star drift toward
			// white-hot or pale blue.
			float drift = Mathf.Clamp01((bright - 0.8f) * 0.5f);
			Color col = Color.Lerp(ArmColor, Color.white, 0.35f + 0.4f * drift);
			col = Color.Lerp(col, ArmColor * 0.8f, Unit(ref state) * 0.25f);
			col *= bright * (dust > DustReject * 0.7f ? 0.55f : 1f);

			Star s = default;
			s.Pos = pos;
			s.Size = size;
			s.Col = col;
			return s;
		}

		private static Star PlaceCoreStar(ref ulong state, in Params p)
		{
			// The bar ellipsoid in its own frame (short x sigma
			// BarWidth, long z sigma BarLength), 4-sigma clamped; 30%
			// land in a tighter nucleus.
			bool nucleus = Unit(ref state) < 0.3f;
			float k = nucleus ? 0.35f : 1f;
			float bx = Mathf.Clamp(Gaussian(ref state, p.BarWidth * k), -4f * p.BarWidth, 4f * p.BarWidth);
			float by = Mathf.Clamp(Gaussian(ref state, p.BarLength * k), -4f * p.BarLength, 4f * p.BarLength);
			float y = Mathf.Clamp(Gaussian(ref state, SigmaCoreY), -4f * SigmaCoreY, 4f * SigmaCoreY);
			float ang = p.BarAngleDegrees * Mathf.Deg2Rad;
			float cs = Mathf.Cos(ang);
			float sn = Mathf.Sin(ang);
			Vector3 pos = new Vector3(bx * cs + by * sn, y, -bx * sn + by * cs);

			float sizeT = Mathf.Pow(Unit(ref state), 2f);
			float size = Mathf.Lerp(0.0004f, 0.0022f, sizeT);
			float bright = 0.3f + 1.6f * Mathf.Pow(Unit(ref state), 2.2f);
			Color col = Color.Lerp(CoreColor, DiskColor, Unit(ref state) * 0.4f) * bright;

			Star s = default;
			s.Pos = pos;
			s.Size = size;
			s.Col = col;
			return s;
		}

		private static Star PlaceDiskStar(ref ulong state, in Params p)
		{
			float scale = Mathf.Max(p.DiskScale, 0.05f);
			float u = Unit(ref state);
			float r = Mathf.Clamp(-scale * Mathf.Log(1f - u * (1f - Mathf.Exp(-1f / scale))), 0.05f, 1.0f);
			float a = Unit(ref state) * 2f * Mathf.PI;
			Vector3 pos = new Vector3(r * Mathf.Cos(a), Mathf.Clamp(Gaussian(ref state, SigmaDiskY), -4f * SigmaDiskY, 4f * SigmaDiskY), r * Mathf.Sin(a));

			float sizeT = Mathf.Pow(Unit(ref state), 2f);
			float size = Mathf.Lerp(0.00035f, 0.0013f, sizeT);
			float bright = 0.2f + 1.1f * Mathf.Pow(Unit(ref state), 2.4f);
			Color col = DiskColor * bright;

			Star s = default;
			s.Pos = pos;
			s.Size = size;
			s.Col = col;
			return s;
		}

		private static Star PlaceHaloStar(ref ulong state, in Params p)
		{
			float sx = Mathf.Clamp(Gaussian(ref state, 0.5f), -1.3f, 1.3f);
			float sz = Mathf.Clamp(Gaussian(ref state, 0.5f), -1.3f, 1.3f);
			float sy = Mathf.Clamp(Gaussian(ref state, SigmaHaloY), -1.0f, 1.0f);
			Vector3 pos = new Vector3(sx, sy, sz);

			float sizeT = Mathf.Pow(Unit(ref state), 2f);
			float size = Mathf.Lerp(0.0004f, 0.0018f, sizeT);
			float bright = 0.15f + 0.7f * Mathf.Pow(Unit(ref state), 2.4f);
			Color col = Color.Lerp(DiskColor, CoreColor, 0.3f) * bright;

			Star s = default;
			s.Pos = pos;
			s.Size = size;
			s.Col = col;
			return s;
		}
	}
}
