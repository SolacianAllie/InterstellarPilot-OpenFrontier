using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// The per-field budget for raymarched nebula volumes: ranks the volumes
	/// under this transform by projected size and keeps only the rung's
	/// MaxActive drawing, each at a step count that follows how much of the
	/// screen it covers. Ranking runs at 4 Hz on unscaled time, never per
	/// frame - a step count, a draw budget and an LOD swap do not need 60 Hz.
	///
	/// It also owns the uploaded LOD: a volume large on screen gets the
	/// rung's maximum resolution, a mid-size one at most 128^3, a distant
	/// one 64^3, with 0.75 shrink hysteresis at both thresholds. The
	/// volume's own `quality` slider is the per-instance ceiling, so the
	/// ladder can only spend up to what the author allowed.
	///
	/// Put one on the stage object that parents the nebula placables. With no
	/// field in the scene every volume draws at its authored quality and
	/// native step count, which is correct for a scene holding one or two
	/// volumes.
	///
	/// [ExecuteAlways]: in the editor the field ranks against the active
	/// Scene view camera, so the galaxy is inspectable without play mode.
	/// Visibility stays uncapped there (every placed volume is visible), but
	/// LOD and step count already respond to the Scene view - what you see
	/// while tuning is the in-play budget's effect.
	///
	/// The budget is a draw count, not a hard visibility rule: a volume below
	/// MinScreenFraction is skipped even when the budget has room, because a
	/// two-percent silhouette cannot pay for a raymarch on a phone.
	/// </summary>
	[ExecuteAlways]
	[DefaultExecutionOrder(-100)]
	[DisallowMultipleComponent]
	public sealed class GalaxyNebulaField : MonoBehaviour
	{
		[Tooltip("Force a rung instead of letting the platform decide.")]
		public bool overrideRung;

		[Tooltip("Rung used when overrideRung is on.")]
		public NebulaQualityRung rung = NebulaQualityRung.Desktop;

		[Tooltip("Seconds between rankings. Cheap, but not free - do not run it per frame.")]
		[Min(0.05f)] public float refreshSeconds = 0.25f;

		[Tooltip("Optional view. Falls back to Camera.main.")]
		public Camera view;

		GalaxyNebulaVolume[] _volumes = System.Array.Empty<GalaxyNebulaVolume>();
		float[] _score = System.Array.Empty<float>();
		float[] _fraction = System.Array.Empty<float>();
		bool[] _drawn = System.Array.Empty<bool>();
		int[] _order = System.Array.Empty<int>();
		Camera _cachedView;
		float _nextRank;

		void OnEnable()
		{
			// Runs before the volumes' OnEnable when the execution order holds;
			// if it does not, GalaxyNebulaVolume skips a rebuild whose asset
			// did not change, so the cost is one extra Pick(), not an upload.
			if (overrideRung) NebulaQuality.Rung = rung;
			NebulaQuality.Changed += Rebuild;
			Rebuild();
		}

		void OnDisable()
		{
			NebulaQuality.Changed -= Rebuild;
			RestoreAll();
		}

		void OnValidate()
		{
			refreshSeconds = Mathf.Max(0.05f, refreshSeconds);
		}

		/// <summary>Re-scan children after volumes are spawned or removed at runtime.</summary>
		public void Refresh()
		{
			Rebuild();
		}

		void Rebuild()
		{
			_volumes = GetComponentsInChildren<GalaxyNebulaVolume>(true);
			int n = _volumes.Length;
			_score = new float[n];
			_fraction = new float[n];
			_drawn = new bool[n];
			_order = new int[n];
			for (int i = 0; i < n; i++) _order[i] = i;
			_nextRank = 0f;
		}

		void LateUpdate()
		{
			if (_volumes.Length == 0) return;
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				// Editor preview: rank against the Scene view camera so LOD
				// and steps are the ones play mode would pick.
				UnityEditor.SceneView sceneView = UnityEditor.SceneView.lastActiveSceneView;
				Camera sceneCamera = sceneView != null ? sceneView.camera : null;
				if (sceneCamera == null) return;
				float editNow = Time.realtimeSinceStartup;
				if (editNow < _nextRank) return;
				_nextRank = editNow + Mathf.Max(0.1f, refreshSeconds);
				Rank(sceneCamera);
				return;
			}
#endif
			float now = Time.unscaledTime;
			if (now < _nextRank) return;
			_nextRank = now + Mathf.Max(0.05f, refreshSeconds);
			Rank(ResolveView());
		}

		Camera ResolveView()
		{
			if (view != null) return view;
			if (_cachedView != null && _cachedView.isActiveAndEnabled) return _cachedView;
			_cachedView = Camera.main;
			return _cachedView;
		}

		void Rank(Camera cam)
		{
			if (cam == null)
			{
				RestoreAll();
				return;
			}

			bool ortho = cam.orthographic;
			float halfHeight = Mathf.Max(cam.orthographicSize, 0.001f);
			float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
			Vector3 eye = cam.transform.position;
			float minFraction = NebulaQuality.MinScreenFraction;
			int maxActive = Mathf.Min(NebulaQuality.MaxActive, _volumes.Length);

			for (int i = 0; i < _volumes.Length; i++)
			{
				GalaxyNebulaVolume volume = _volumes[i];
				if (volume == null)
				{
					_score[i] = 0f;
					_fraction[i] = 0f;
					continue;
				}

				Bounds bounds = volume.WorldBounds;
				float radius = bounds.extents.magnitude;
				float distance = Mathf.Max(Vector3.Distance(eye, bounds.center), 0.001f);
				float fraction = ortho ? radius / halfHeight : radius / (distance * tanHalf);
				fraction = Mathf.Clamp01(fraction);

				_fraction[i] = fraction;
				float score = fraction * fraction * Mathf.Max(volume.brightness, 0.01f);
				// Hysteresis at the draw-budget edge: a volume already drawing
				// keeps its slot against a newcomer of near-equal score instead
				// of flickering in and out every refresh.
				if (_drawn[i]) score *= 1.15f;
				_score[i] = score;
			}

			// Partial selection sort: the first maxActive entries end up in
			// descending score order, the rest stay a permutation. No lambdas,
			// no allocations, O(n^2) on <= a few dozen volumes at 4 Hz.
			for (int i = 0; i < maxActive; i++)
			{
				int best = i;
				for (int j = i + 1; j < _order.Length; j++)
					if (_score[_order[j]] > _score[_order[best]]) best = j;
				int swap = _order[i];
				_order[i] = _order[best];
				_order[best] = swap;
			}

			for (int rank = 0; rank < _volumes.Length; rank++)
			{
				int i = _order[rank];
				GalaxyNebulaVolume volume = _volumes[i];
				if (volume == null) continue;

				bool draw = rank < maxActive && _fraction[i] >= minFraction;
				volume.SetVisible(draw);
				_drawn[i] = draw;
				if (!draw)
				{
					// A volume not worth drawing does not keep an 8/64 MB
					// texture resident; 64^3 is 1 MB and reloads on approach.
					if (_fraction[i] < minFraction) volume.SetLodLevel(0);
					continue;
				}
				volume.SetLodLevel(DesiredLod(volume, _fraction[i]));
				volume.SetMarchSteps(StepsFor(volume, _fraction[i]));
			}
		}

		/// <summary>
		/// Uploaded LOD from projected size, with the shrink hysteresis:
		/// a volume already at a rung keeps it until the fraction falls 25%
		/// below the threshold that promoted it, so a volume hovering at a
		/// boundary does not rebuild its texture every ranking tick.
		/// </summary>
		static int DesiredLod(GalaxyNebulaVolume volume, float fraction)
		{
			int max = volume.MaxAllowedLod;
			float nearCut = volume.UploadedLod >= 2
				? NebulaQuality.LodNearFraction * 0.75f
				: NebulaQuality.LodNearFraction;
			float midCut = volume.UploadedLod >= 1
				? NebulaQuality.LodMidFraction * 0.75f
				: NebulaQuality.LodMidFraction;
			if (fraction >= nearCut) return max;
			if (fraction >= midCut) return Mathf.Min(max, 1);
			return 0;
		}

		static float StepsFor(GalaxyNebulaVolume volume, float fraction)
		{
			// NativeSteps is two texels per step at the uploaded edge; a mobile
			// 64^3 volume tops out at 24 with ED's own Low-rung step count,
			// while the same volume at Hero can be asked for its full 32.
			float cap = Mathf.Min(NebulaQuality.StepBudget, volume.NativeSteps);
			float ramp = Mathf.Clamp01(fraction / NebulaQuality.FullStepFraction);
			return Mathf.Lerp(NebulaQuality.MinSteps, cap, ramp);
		}

		void RestoreAll()
		{
			for (int i = 0; i < _volumes.Length && i < _drawn.Length; i++)
			{
				GalaxyNebulaVolume volume = _volumes[i];
				if (volume == null) continue;
				volume.SetVisible(true);
				// Deliberately no LOD restore: OnDisable runs while the scene
				// is closing, and asking every volume for its 256^3 texture
				// would upload up to 64 MB each only to destroy it moments
				// later. The resident rung is already correct for whatever
				// camera last ranked; the volume's own OnDisable releases it.
				volume.SetMarchSteps(volume.NativeSteps);
				_drawn[i] = true;
			}
		}
	}
}
