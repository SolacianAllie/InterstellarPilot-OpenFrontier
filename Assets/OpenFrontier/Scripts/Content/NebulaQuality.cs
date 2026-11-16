using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>Volumetric nebula detail rung. Ordered, so int comparisons work.</summary>
	public enum NebulaQualityRung
	{
		Mobile = 0,
		Desktop = 1,
		Hero = 2
	}

	/// <summary>
	/// The quality ladder for the raymarched nebula volumes.
	///
	/// ED ships the same volumes at 64^3 / 128^3 / 256^3 and scales its march
	/// and draw counts with GalaxyMapQuality (Low 25/128 steps and 10 volumes;
	/// High 35/138 and 30 - see docs/GALAXY_RENDER_FINDINGS.md). This is the
	/// rebuild's equivalent knob, with the low rung sized for a phone:
	///
	///   Mobile   64^3 only,   6-24 steps,  10 drawn   (1 MB per volume)
	///   Desktop  128^3 max,  10-64 steps,  20 drawn   (8 MB worst case)
	///   Hero     256^3 max, 16-128 steps,  uncapped   (64 MB worst case)
	///
	/// Steps follow the volume's projected size on top of the rung: a cloud
	/// filling the view gets the cap, a distant smudge gets MinSteps. That is
	/// a bigger saving than a flat per-rung count because the cost is
	/// per covered pixel, and it is still ED's ladder shape - the rung sets
	/// the ceiling, the size picks the point below it.
	///
	/// The rung gates what is uploaded and drawn, not what is packaged: a
	/// prefab's serialized volumeMedium/volumeHigh references are pulled into
	/// any build that places it. See Nebulae/README.md (Memory) and
	/// Assets/Editor/NebulaVolumeBuildCheck.cs.
	///
	/// The rung is a MAXIMUM resolution, not a fixed choice. GalaxyNebulaField
	/// swaps each volume's uploaded LOD by its projected size beneath that
	/// ceiling (near = max, far = 64^3), and a volume's authored `quality`
	/// is a second, lower ceiling for that instance. So the knob reads as
	/// "how much detail may ever be spent", and the LOD ladder decides how
	/// much is actually resident frame to frame.
	/// </summary>
	public static class NebulaQuality
	{
		/// <summary>Fires when the rung changes. Volumes rebuild their texture.</summary>
		public static event System.Action Changed;

		/// <summary>Projected radius / half-height at which a volume reaches StepBudget.</summary>
		public const float FullStepFraction = 0.35f;

		/// <summary>
		/// Projected-size thresholds for the uploaded LOD: at or above the
		/// near fraction a volume may hold the rung's maximum resolution; at
		/// or above the mid fraction it holds at most 128^3; below it 64^3.
		/// The field applies a 0.75 shrink factor when a volume already holds
		/// the higher rung, so a volume hovering at a threshold does not
		/// rebuild its texture every rank tick.
		/// </summary>
		public const float LodNearFraction = 0.18f;
		public const float LodMidFraction = 0.045f;

		static int _rung = -1;

		public static NebulaQualityRung Rung
		{
			get
			{
				if (_rung < 0) _rung = (int)Detect();
				return (NebulaQualityRung)_rung;
			}
			set
			{
				int next = Mathf.Clamp((int)value, 0, 2);
				if (_rung == next) return;
				_rung = next;
				Changed?.Invoke();
			}
		}

		/// <summary>Highest LOD index the rung may upload: 0 = 64^3, 1 = 128^3, 2 = 256^3.</summary>
		public static int MaxLod => (int)Rung;

		/// <summary>
		/// Volumes drawn at once. ED caps 10/20/30; ours are bigger on screen,
		/// so Desktop keeps ED's 20 and Hero is effectively uncapped.
		/// </summary>
		public static int MaxActive => Rung == NebulaQualityRung.Mobile ? 10
			: Rung == NebulaQualityRung.Desktop ? 20 : 64;

		/// <summary>Steps for a volume at the bottom of the size ramp.</summary>
		public static int MinSteps => Rung == NebulaQualityRung.Mobile ? 6
			: Rung == NebulaQualityRung.Desktop ? 10 : 16;

		/// <summary>Steps for a volume that fills the view, capped by its LOD's native count.</summary>
		public static int StepBudget => Rung == NebulaQualityRung.Mobile ? 24
			: Rung == NebulaQualityRung.Desktop ? 64 : 128;

		/// <summary>
		/// Projected radius / half-screen-height below which a volume is not
		/// drawn. The size half of ED's draw budget: a nebula whose whole
		/// silhouette is two percent of the screen is not worth a raymarch.
		/// </summary>
		public static float MinScreenFraction => Rung == NebulaQualityRung.Mobile ? 0.02f
			: Rung == NebulaQualityRung.Desktop ? 0.012f : 0.008f;

		public static bool IsMobile => Rung == NebulaQualityRung.Mobile;

		public static string Label => Rung == NebulaQualityRung.Mobile ? "Mobile"
			: Rung == NebulaQualityRung.Desktop ? "Desktop" : "Hero";

		static NebulaQualityRung Detect()
		{
			// Switch is a handheld first: same 1 MB ceiling as a phone, not the
			// desktop 8 MB one.
			if (Application.isMobilePlatform) return NebulaQualityRung.Mobile;
			if (Application.platform == RuntimePlatform.Switch) return NebulaQualityRung.Mobile;
			return NebulaQualityRung.Desktop;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ResetStatics()
		{
			// Enter Play Mode Options can keep the domain, so a stale rung or a
			// delegate list from the last session would survive. Detect again
			// and drop subscribers (they re-subscribe in OnEnable).
			_rung = -1;
			Changed = null;
		}
	}
}
