using UnityEngine;

namespace OpenFrontier.Galaxy
{
	/// <summary>
	/// Shared deterministic distribution math for the galaxy's discrete
	/// dressing (GalaxyStars, GalaxyClouds).
	///
	/// Determinism contract: every random DECISION comes from an integer
	/// splitmix64 stream seeded per component -- same seed, same galaxy,
	/// every session, every platform. The float math that follows (angles,
	/// positions, the exp/cos shapes) is scenery: it never reaches sim
	/// state, saves or IDs, so the integer-only seed rule is honoured
	/// where it bites (the decisions) while the geometry stays readable.
	///
	/// TWO SAMPLERS, one per painted structure (GalaxyDisk shader):
	/// - SampleDisk: the thin disk. Exponential radial falloff, arm
	///   snapping onto the SAME logarithmic spiral the shader paints --
	///   its band peaks exactly where the snapping places points.
	/// - SampleCore: the bulge/bar. A tri-axial gaussian ellipsoid --
	///   long along the painted bar axis (the shader squashes its
	///   rotated frame's second axis; same frame here), plus a tighter
	///   nucleus share, vertically thick. The core is where the galaxy's
	///   vertical density lives; the thin disk deliberately has almost
	///   none.
	/// - SampleHalo: the halo. A round, sparse spheroid around
	///   everything -- the Elite behaviour of systems far above the
	///   plane. No painted counterpart: the disk shader stays a sheet;
	///   the halo lives only in the discrete populations.
	///
	/// Plus the LOCAL-SKY helpers (GalaxyLocalStars): CellSeed gives a
	/// cell-keyed deterministic seed -- the same spatial cell of the
	/// same base seed is the same local sky, every revisit, nothing
	/// stored -- and LocalPopulationWeights evaluates the samplers'
	/// density AT A POINT, each population carrying its normalisation
	/// constant (the sigma-volume factor), so the local population MIX
	/// is physically proportioned, not just shape-proportioned.
	/// </summary>
	public static class GalaxyDistribution
	{
		/// <summary>The splitmix64 gamma -- also the stream's initial bump.</summary>
		private const ulong Golden = 0x9E3779B97F4A7C15UL;

		/// <summary>Share of core samples landing in the NUCLEUS: the same
		/// bar ellipsoid at NucleusScale sigma, so the centre is a hot
		/// second component rather than one smooth gradient.</summary>
		private const float NucleusShare = 0.3f;
		private const float NucleusScale = 0.35f;

		/// <summary>A fresh splitmix64 state from an integer seed. Distinct
		/// seeds land far apart in the stream (integer multiply, not float
		/// arithmetic), so seed 1 and seed 2 are wholly different galaxies.</summary>
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

		/// <summary>Uniform float in 0..1 from the top 24 bits of the next
		/// word -- an exact float conversion, no rounding drift.</summary>
		public static float Unit(ref ulong state)
		{
			return (float)(Next(ref state) >> 40) * (1f / 16777216f);
		}

		/// <summary>A standard-normal sample via Box-Muller, scaled by
		/// sigma. Two uniforms per call, all from the integer stream.</summary>
		public static float Gaussian(ref ulong state, float sigma)
		{
			float u1 = Mathf.Max(Unit(ref state), 1e-7f);
			float u2 = Unit(ref state);
			return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2) * sigma;
		}

		/// <summary>
		/// One point in the THIN DISK (unit radius 0..1): an exponential
		/// radial falloff; armWeight of samples snap to the nearest arm
		/// crest with gaussian spread armSpread (radians per arm), the
		/// remainder uniform (inter-arm haze). Arm snapping is suppressed
		/// inside bulgeRadius * 0.5 -- the painted arms are swamped by the
		/// core glow there anyway. The caller owns the vertical: disk
		/// heights come from the disk's own thickness profile.
		/// </summary>
		public static void SampleDisk(
			ref ulong state,
			float diskFalloff,
			float armCount, float winding, float armWeight, float armSpread,
			float bulgeRadius,
			out float radius, out float angle)
		{
			// Inverse CDF of the exponential falloff, normalised so the
			// whole population spans radius 0..1:
			//   F(r) = (1 - exp(-f*r)) / (1 - exp(-f))
			//   r    = -ln(1 - v*(1 - exp(-f))) / f
			float f = Mathf.Max(diskFalloff, 0.5f);
			float v = Unit(ref state);
			radius = -Mathf.Log(1f - v * (1f - Mathf.Exp(-f))) / f;
			radius = Mathf.Clamp(radius, 0f, 1f);

			int arms = Mathf.Max(1, Mathf.RoundToInt(armCount));
			if (Unit(ref state) < armWeight && radius > bulgeRadius * 0.5f)
			{
				// The shader's band, sin(N*theta + winding*ln(r)), peaks
				// where N*theta + winding*ln(r) = pi/2 + 2*pi*k:
				//   theta = (pi/2 + 2*pi*k) / N - winding*ln(r) / N
				int arm = (int)(Unit(ref state) * arms) % arms;
				angle = (Mathf.PI * 0.5f + 2f * Mathf.PI * arm) / arms
					- winding * Mathf.Log(Mathf.Max(radius, 0.03f)) / arms;
				angle += Gaussian(ref state, armSpread / arms);
			}
			else
			{
				angle = Unit(ref state) * (Mathf.PI * 2f);
			}
		}

		/// <summary>
		/// One point in the CORE population. x/z come back in unit-disk
		/// radii; y is a unit normal (sigma 1) in half-thickness units --
		/// the caller multiplies it by the disk's half-thickness and its
		/// CoreHeight multiplier, and shades it with a curve of its own.
		///
		/// The bar is gaussian along each axis: density peaks at the
		/// centre and falls off smoothly outward -- a real radial
		/// gradient, unlike the flat uniform-area circle this replaced.
		///
		/// The bar frame matches the disk shader's painted core: the
		/// shader rotates the sample frame by CoreRotation and squashes
		/// the frame's SECOND axis, so at 0 degrees the long axis runs
		/// along local Z. Points are sampled as (short, long) in the bar
		/// frame and mapped back with the frame's inverse,
		/// lp = R(-theta) * (a, b).
		/// </summary>
		public static void SampleCore(
			ref ulong state,
			float barLength, float barWidth, float barAngleDegrees,
			out Vector3 core)
		{
			float s = Unit(ref state) < NucleusShare ? NucleusScale : 1f;
			float a = Gaussian(ref state, barWidth * s);
			float b = Gaussian(ref state, barLength * s);
			// The vertical clamps at 4 sigma: the bounds the components
			// bake assume exactly this reach, and an unclamped gaussian
			// tail would eventually poke a star outside them.
			float h = Mathf.Clamp(Gaussian(ref state, 1f), -4f, 4f);
			float ang = barAngleDegrees * Mathf.Deg2Rad;
			float cs = Mathf.Cos(ang);
			float sn = Mathf.Sin(ang);
			core = new Vector3(a * cs + b * sn, h, -a * sn + b * cs);
		}

		/// <summary>
		/// One point in the HALO: a big, round, sparse spheroid around
		/// the whole galaxy -- the Elite behaviour of systems far above
		/// the plane. x/z are gaussian at haloRadius, in unit-disk radii,
		/// ignoring the plane's radial structure BY DESIGN (a stretched
		/// disk slab is not a halo); y is a unit normal in half-thickness
		/// units, for the caller's height multiplier and shading curve.
		/// All three axes clamp at 4 sigma: the components' baked bounds
		/// assume exactly this reach.
		/// </summary>
		public static void SampleHalo(
			ref ulong state, float haloRadius, out Vector3 halo)
		{
			float a = Mathf.Clamp(
				Gaussian(ref state, haloRadius), -4f * haloRadius, 4f * haloRadius);
			float b = Mathf.Clamp(
				Gaussian(ref state, haloRadius), -4f * haloRadius, 4f * haloRadius);
			float h = Mathf.Clamp(Gaussian(ref state, 1f), -4f, 4f);
			halo = new Vector3(a, h, b);
		}

		/// <summary>
		/// A deterministic seed for a spatial cell. The same cell of the
		/// same base seed always yields the same local sky -- the
		/// "billions of stars, revisit-stable" contract with a bounded
		/// buffer and nothing stored. Integer math throughout: platform
		/// stable by construction.
		/// </summary>
		public static ulong CellSeed(int baseSeed, int cx, int cy, int cz)
		{
			ulong state = Stream(baseSeed)
				^ ((ulong)(uint)cx * 0x9E3779B97F4A7C15UL)
				^ ((ulong)(uint)cy * 0xBF58476D1CE4E5B9UL)
				^ ((ulong)(uint)cz * 0x94D049BB133111EBUL);
			return Next(ref state);
		}

		/// <summary>
		/// The galaxy model's density AT A POINT, split by population.
		/// Inputs are in model units: xz in unit-disk radii, h in
		/// half-thickness units. The weights are the population's global
		/// MASS x its normalised pdf at the point -- the sigma-volume
		/// factors (1/sigma products, and the exponential disk's rate
		/// factor) are what make the local MIX honest: without them a
		/// sparse wide halo would out-numbers the dense thin disk
		/// everywhere outside the core.
		///
		/// Shapes mirror the samplers exactly: the disk's radial
		/// exponential and sheet vertical profile (at ABSOLUTE height,
		/// through the DiskHeight stretch), the bar's rotated tri-axial
		/// gaussian (body sigmas only -- the nucleus is a brightness
		/// detail, not a density structure), and the halo's round
		/// gaussian.
		/// </summary>
		public static void LocalPopulationWeights(
			Vector2 xz, float h,
			float diskFalloff, float diskHeight, float verticalFalloff,
			float barLength, float barWidth, float barAngleDegrees,
			float coreHeight,
			float haloRadius, float haloHeight,
			float diskMass, float coreMass, float haloMass,
			out float diskW, out float coreW, out float haloW)
		{
			// Disk: radial exponential x the sheet's vertical profile.
			// hD converts ABSOLUTE half-thicknesses into the disk's own
			// stretched units (the sampler's hN = y / (half x DiskHeight)).
			float f = Mathf.Max(diskFalloff, 0.5f);
			float hD = h / Mathf.Max(diskHeight, 0.05f);
			float edge = Mathf.Exp(-verticalFalloff);
			float diskV = Mathf.Max(0f,
				(Mathf.Exp(-hD * hD * verticalFalloff) - edge) / (1f - edge));
			// 0.45: the disk sampler's vertical sigma, in half-thickness
			// units (mirrored by GalaxyLocalStars).
			diskW = diskMass * Mathf.Exp(-xz.magnitude * f) * f * diskV
				/ (0.45f * Mathf.Max(diskHeight, 0.05f));

			// Core: the bar ellipsoid in the shader's rotated frame --
			// the same rotation SampleCore inverts. bx is the SHORT-axis
			// coordinate (sigma barWidth), by the LONG (sigma barLength).
			float ang = barAngleDegrees * Mathf.Deg2Rad;
			float cs = Mathf.Cos(ang);
			float sn = Mathf.Sin(ang);
			float bx = xz.x * cs - xz.y * sn;
			float by = xz.x * sn + xz.y * cs;
			float sw = Mathf.Max(barWidth, 0.01f);
			float sl = Mathf.Max(barLength, 0.01f);
			float sh = Mathf.Max(coreHeight, 0.05f);
			coreW = coreMass
				* Mathf.Exp(-(bx * bx / (2f * sw * sw)
					+ by * by / (2f * sl * sl)))
				* Mathf.Exp(-(h / sh) * (h / sh) * 0.5f)
				/ (sw * sl * sh);

			// Halo: round gaussian, same treatment.
			float sr = Mathf.Max(haloRadius, 0.01f);
			float shh = Mathf.Max(haloHeight, 0.05f);
			haloW = haloMass
				* Mathf.Exp(-(xz.x * xz.x + xz.y * xz.y) / (2f * sr * sr))
				* Mathf.Exp(-(h / shh) * (h / shh) * 0.5f)
				/ (sr * sr * shh);
		}
	}
}
