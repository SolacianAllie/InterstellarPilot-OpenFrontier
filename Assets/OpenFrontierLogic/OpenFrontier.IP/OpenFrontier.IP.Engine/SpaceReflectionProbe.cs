using UnityEngine;
using UnityEngine.Rendering;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Open Frontier: ONE realtime reflection probe at the SECTOR ANCHOR -
	/// the same viewpoint the pinned SpaceCamera renders the sky from.
	/// (URP no longer blends two probes the way BiRP did, so the sky/near
	/// split collapsed to a single anchor probe.)
	///
	/// The starfield is a 20000-wide cube pinned at the world origin and
	/// the nebulae are world-pinned too, so the sky only captures
	/// correctly from the anchor; since cubemaps are sampled by direction
	/// only, one anchor capture serves the whole sector. The mask spans
	/// the sky (DeepSpace/30: starfield + nebulae), gas clouds (29),
	/// planets (23) and background planets (26), plus the probe-only
	/// anchor sun (SkyProbe/3) - world objects sit far enough from the
	/// anchor that their parallax error is sub-degree.
	///
	/// Clears to opaque black; re-renders once per second or on demand via
	/// RequestRender (rate-limited to 0.1s). Spawned by
	/// ActiveSectorData under the EngineASX root.
	/// </summary>
	public class SpaceReflectionProbe : MonoBehaviour
	{
		public static SpaceReflectionProbe Instance { get; private set; }

		// Steady-state capture interval (user-tuned). Cheap because the
		// mask is sky layers only and timeSlicingMode spreads the six
		// faces across frames.
		private const float SecondsBetweenRenders = 0.5f;

		// Just after world ready the sky may still be generating - a
		// capture then bakes a white/empty void. Burst-capture for the
		// first few seconds so a good capture lands as soon as there is
		// anything real to see.
		private const float SettlingSecondsBetweenRenders = 0.25f;

		private const float SettlingDuration = 4f;

		private const float MinSecondsBetweenRequestedRenders = 0.1f;

		// DeepSpace (30 - starfield + nebulae) | UnitGasCloud (29) |
		// BackgroundPlanet (26) | Planet (23) | SkyProbe (3 - anchor sun)
		private const int SkyCullingMask = 1073741824 | 536870912 | 67108864 | 8388608 | 8;

		private ReflectionProbe skyProbe;

		private float nextRenderTime;

		private float lastRenderTime = -1f;

		private bool renderRequested;

		private void Awake()
		{
			Instance = this;
			skyProbe = CreateProbe("SkyReflectionProbe", SkyCullingMask, 100000f, new Vector3(30000f, 30000f, 30000f), 1);
		}

		private ReflectionProbe CreateProbe(string objectName, int cullingMask, float farClip, Vector3 size, int importance)
		{
			GameObject gameObject = new GameObject(objectName);
			gameObject.transform.SetParent(transform, worldPositionStays: false);
			ReflectionProbe reflectionProbe = gameObject.AddComponent<ReflectionProbe>();
			reflectionProbe.mode = ReflectionProbeMode.Realtime;
			reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
			// HDR capture is back ON: the white-sheen culprit turned out
			// to be the sun core material's color sitting at 1.27e+30
			// (fixed to 2 by the user), not HDR itself. The probe is
			// runtime-spawned, so this must live in code - editor
			// toggles die with the session.
			reflectionProbe.hdr = true;
			// Mobile gets a cheaper probe; reflections are blurry by nature.
			reflectionProbe.resolution = (Application.isMobilePlatform ? 64 : 128);
			reflectionProbe.farClipPlane = farClip;
			reflectionProbe.cullingMask = cullingMask;
			reflectionProbe.clearFlags = ReflectionProbeClearFlags.SolidColor;
			reflectionProbe.backgroundColor = new Color(0f, 0f, 0f, 1f);
			reflectionProbe.boxProjection = false;
			reflectionProbe.importance = importance;
			reflectionProbe.size = size;
			// One face per frame: no single-frame spike from the 6-face
			// capture, which is what makes the 0.5s interval affordable.
			reflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
			return reflectionProbe;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		// Ask for a fresh render (e.g. godmode appearance edits). Renders
		// happen at most once per 0.1 seconds regardless of call rate.
		public void RequestRender()
		{
			renderRequested = true;
		}

		private bool wasLoadedAndReady;

		private float readyTime = -1f;

		private void LateUpdate()
		{
			// Never capture before the world is ready: a mid-load capture
			// grabs the empty/initializing sky (white void / unloaded
			// starfield) and every smooth surface wears it as a sheen.
			// When readiness flips true, force a fresh capture so any
			// early garbage never survives a frame longer than needed.
			if (!EngineASX.LoadedAndReady)
			{
				wasLoadedAndReady = false;
				return;
			}
			if (!wasLoadedAndReady)
			{
				wasLoadedAndReady = true;
				readyTime = Time.unscaledTime;
				renderRequested = true;
			}
			// The probe sits at the sector anchor (the SpaceCamera's
			// viewpoint); fall back to the camera when no sector is loaded.
			if (EngineASX.Instance != null && EngineASX.Instance.ActiveSector != null)
			{
				skyProbe.transform.position = EngineASX.Instance.ActiveSector.transform.position;
			}
			else
			{
				Camera camera2 = WorldCamera.Resolve();
				if (camera2 != null)
				{
					skyProbe.transform.position = camera2.transform.position;
				}
			}
			bool flag = renderRequested && Time.unscaledTime >= lastRenderTime + MinSecondsBetweenRequestedRenders;
			if (flag || Time.unscaledTime >= nextRenderTime)
			{
				renderRequested = false;
				lastRenderTime = Time.unscaledTime;
				nextRenderTime = lastRenderTime + ((Time.unscaledTime - readyTime < SettlingDuration) ? SettlingSecondsBetweenRenders : SecondsBetweenRenders);
				if (skyProbe.RenderProbe() == -1)
				{
					Debug.LogWarning("[SpaceReflectionProbe] RenderProbe scheduling failed", this);
				}
			}
		}
	}
}
