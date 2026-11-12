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
	/// The arm angle here is the SAME logarithmic spiral the GalaxyDisk
	/// shader paints -- its band peaks exactly where this snapping places
	/// points, so the discrete population sits on the painted arms.
	/// </summary>
	public static class GalaxyDistribution
	{
		/// <summary>The splitmix64 gamma -- also the stream's initial bump.</summary>
		private const ulong Golden = 0x9E3779B97F4A7C15UL;

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
		/// One point in the unit disk: bulgeFraction of samples inside a
		/// bulge of bulgeRadius (uniform area density), the rest on an
		/// exponential disk; of those, armWeight snap to the nearest arm
		/// crest with gaussian spread armSpread (radians per arm), the
		/// remainder uniform (inter-arm haze).
		/// </summary>
		public static void SamplePosition(
			ref ulong state,
			float bulgeFraction, float bulgeRadius, float diskFalloff,
			float armCount, float winding, float armWeight, float armSpread,
			out float radius, out float angle)
		{
			if (Unit(ref state) < bulgeFraction)
			{
				// sqrt: uniform density across the bulge's area.
				radius = bulgeRadius * Mathf.Sqrt(Unit(ref state));
				angle = Unit(ref state) * (Mathf.PI * 2f);
				return;
			}

			// Inverse CDF of the exponential falloff, normalised so the
			// whole population spans radius 0..1:
			//   F(r) = (1 - exp(-f*r)) / (1 - exp(-f))
			//   r    = -ln(1 - v*(1 - exp(-f))) / f
			float f = Mathf.Max(diskFalloff, 0.5f);
			float v = Unit(ref state);
			radius = -Mathf.Log(1f - v * (1f - Mathf.Exp(-f))) / f;
			radius = Mathf.Clamp(radius, 0f, 1f);

			int arms = Mathf.Max(1, Mathf.RoundToInt(armCount));
			// Arm snapping is suppressed inside the inner bulge: the core
			// is a uniform swarm (the painted arms there are swamped by
			// the bulge glow anyway).
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
	}
}
