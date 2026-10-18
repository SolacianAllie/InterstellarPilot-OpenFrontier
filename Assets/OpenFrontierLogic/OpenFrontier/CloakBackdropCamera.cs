using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OpenFrontier
{
	/// <summary>
	/// Open Frontier: the cloak backdrop grabber -- a vfxmike-style
	/// previous-frame capture (vfxmike.blogspot.com, "Opaque Active
	/// Camouflage Part 1", 2018-06-07; the Ghost Recon Future Soldier
	/// technique) built from boring engine tech on purpose, after the
	/// RenderGraph renderer-feature capture it replaced (v2.2) went
	/// silent: no pass in the frame debugger, no warning, no exception,
	/// nothing to diagnose. A child camera with a target texture is
	/// ground truth - an enabled camera draws, or the console says why.
	///
	/// How it works: a camera childed under the engine's render camera
	/// (same pose and FOV for free) renders the real scene at a FIXED
	/// 256-long-edge into a ping-pong pair of RTs. The cloak shader
	/// samples whichever buffer holds the finished PREVIOUS frame
	/// (_CloakBackdropRT, a global), bending it around the hull's
	/// silhouette. The copy carries everything the stock opaque texture
	/// never could - the nebula and sun quads are transparent-phase
	/// content - because it is a camera rendering the actual view: the
	/// main camera's own culling mask, clipped only of HUD canvases.
	///
	/// Feedback and why it converges: last frame's copy contains the
	/// ship, but painted AS ITS OWN PREVIOUS CLOAK (bent space, not a
	/// hull), and the warp pushes sample points PAST the silhouette
	/// first (grazing normals project wide), so fresh background keeps
	/// entering and the interior converges to space instead of to a
	/// diffused grey. GR:FS shipped exactly this. See the ship cloak
	/// shader's header for the full v1/v2/v3 story.
	///
	/// Cost: while any hull asks (CloakBackdropRequests), every frame
	/// pays one extra small scene render (culling + draws at ~256px
	/// fill). While none do, the camera is disabled and nothing runs.
	/// Shadow maps are not re-rendered for the copy: the sample is
	/// consumed as a bent smear through which a lit difference is not
	/// readable, and skipping the atlas keeps the cloak frame cheap.
	///
	/// Lifecycle: ProvideCamera (engine, at CreateCamera) wires it; the
	/// child camera dies with its parent rig, a scene change rebuilds
	/// it. The RTs persist for the session (fixed dims, static refs) and
	/// are re-created if a domain reload dropped them.
	/// </summary>
	[DefaultExecutionOrder(1000)]
	public sealed class CloakBackdropCamera : MonoBehaviour
	{
		// The player-facing size of the grab. Fixed regardless of screen
		// resolution, so a 4K display and a foldable bend from the same
		// map and the cost does not grow with the device's pixels.
		private const int SnapshotLongEdge = 256;

		/// <summary>The engine's render camera as handed over; Unity-null when its rig is destroyed.</summary>
		private static Camera mainCamera;

		/// <summary>The grab camera, a child of mainCamera. Unity-null after a scene teardown.</summary>
		private static Camera backdropCamera;

		/// <summary>Which buffer is this frame's write target; the other one holds last frame's finished copy.</summary>
		private static bool bufferAIsWrite;

		/// <summary>Aspect the RT pair was sized for; a drift beyond 1% (foldable rotation) rebuilds them.</summary>
		private static float builtAspect = -1f;

		private static RenderTexture bufferA;
		private static RenderTexture bufferB;

		/// <summary>
		/// Wire the grabber onto the engine's render camera. Called by
		/// ProvideCamera wherever the game creates its camera; safe to call
		/// every scene load - it rebuilds only when the camera changed.
		/// </summary>
		public static void Ensure(Camera camera)
		{
			if (camera == null)
			{
				mainCamera = null;
				// The child dies with its rig anyway; switching it off is
				// belt and braces for the frame between teardown and rebuild.
				if (backdropCamera != null)
				{
					backdropCamera.enabled = false;
				}
				return;
			}

			if (mainCamera == camera && backdropCamera != null)
			{
				return;
			}

			mainCamera = camera;

			GameObject grabberObject = new GameObject("CloakBackdropCamera");
			Camera grabber = grabberObject.AddComponent<Camera>();
			// Parented under the main camera with a zero local transform:
			// pose follows the main camera through every LateUpdate on the
			// rig, without this component needing to know script order.
			grabber.transform.SetParent(camera.transform, false);
			grabber.transform.localPosition = Vector3.zero;
			grabber.transform.localRotation = Quaternion.identity;
			grabber.transform.localScale = Vector3.one;

			grabber.orthographic = false;
			grabber.clearFlags = camera.clearFlags;
			grabber.backgroundColor = camera.backgroundColor;
			// The main camera's own scene, minus HUD canvases: the copy
			// must frame the world the hull stands in front of, not the
			// interface painted over it. Everything else travels - ships,
			// nebulae, the sun, particles - which is the entire point.
			int uiLayer = LayerMask.NameToLayer("UI");
			grabber.cullingMask = uiLayer >= 0 ? (camera.cullingMask & ~(1 << uiLayer)) : camera.cullingMask;
			// Early in the frame loop, before its parent renders (depth
			// ascending). With previous-frame sampling the order is only
			// insurance; making it explicit keeps the frame debugger
			// reading top to bottom as a grab then a main pass.
			grabber.depth = camera.depth - 10f;
			grabber.fieldOfView = camera.fieldOfView;
			grabber.nearClipPlane = camera.nearClipPlane;
			grabber.farClipPlane = camera.farClipPlane;
			grabber.enabled = false; // LateUpdate enables it while any hull wants the copy

			// The extras that keep the grab cheap and unambiguous: no post,
			// no shadow atlas re-render, no depth prepass. What is sampled
			// has one frame of lag and a warp displacing it - none of those
			// layers survive to the bend.
			UniversalAdditionalCameraData additionalData = grabber.GetUniversalAdditionalCameraData();
			additionalData.renderPostProcessing = false;
			additionalData.renderShadows = false;
			additionalData.requiresDepthOption = CameraOverrideOption.Off;

			backdropCamera = grabber;
			grabberObject.AddComponent<CloakBackdropCamera>();
			Report($"grabber attached under '{camera.name}'");
		}

		private void LateUpdate()
		{
			// Superseded driver: two rigs can carry grabbers across a world
			// swap (the log once showed two 'grabber attached' lines and two
			// 'parked' sweeps from the same rig). Only the instance whose
			// camera is the current backdropCamera may act; the stale one
			// parks its child camera so the scene is not rendered every
			// frame for a buffer nobody samples.
			Camera ownGrabber = GetComponent<Camera>();
			if (ownGrabber != backdropCamera)
			{
				if (ownGrabber.enabled)
				{
					ownGrabber.enabled = false;
				}
				return;
			}

			bool wants = backdropCamera != null && mainCamera != null && CloakBackdropRequests.Any;
			if (!wants)
			{
				if (backdropCamera != null && backdropCamera.enabled)
				{
					backdropCamera.enabled = false;
					Report("grabber parked (no hull is cloaking)");
				}
				return;
			}

			FormatBuffers(mainCamera.aspect);

			// Ping-pong: this frame's render fills one buffer while the
			// global stays on the other - the PREVIOUS frame's finished
			// copy. The switch happens now, before any camera of this frame
			// has rendered, so both the grab's own hulls and the main
			// camera's hulls sample a stable buffer all frame long. Only
			// the current driver reaches this point (superseded ones bail
			// above), so the flip runs exactly once per frame.
			bufferAIsWrite = !bufferAIsWrite;
			RenderTexture write = bufferAIsWrite ? bufferA : bufferB;
			RenderTexture read = bufferAIsWrite ? bufferB : bufferA;

			if (!backdropCamera.enabled)
			{
				backdropCamera.enabled = true;
				Report("grabbing previous frames while a cloaking hull wants them");
			}
			backdropCamera.targetTexture = write;
			// Zoom may ride on FOV; the copy has to project like the main
			// camera or the bend maps the wrong content into its silhouette.
			if (backdropCamera.fieldOfView != mainCamera.fieldOfView)
			{
				backdropCamera.fieldOfView = mainCamera.fieldOfView;
			}

			Shader.SetGlobalTexture("_CloakBackdropRT", read);
		}

		/// <summary>
		/// Two RTs at the fixed long edge; the short edge follows the main
		/// camera's aspect so the copy projects 1:1 with what the player
		/// sees. Only an aspect drift (foldable rotation) rebuilds them.
		/// </summary>
		private static void FormatBuffers(float aspect)
		{
			float normalizedAspect = Mathf.Abs(aspect) < 1e-3f ? 1f : aspect;
			if (bufferA != null && Mathf.Abs(normalizedAspect - builtAspect) <= 0.01f)
			{
				return;
			}

			if (bufferA != null)
			{
				bufferA.Release();
				bufferB.Release();
				Object.Destroy(bufferA);
				Object.Destroy(bufferB);
				bufferA = null;
				bufferB = null;
			}

			builtAspect = normalizedAspect;
			int wide = normalizedAspect >= 1f
				? SnapshotLongEdge
				: Mathf.Max(16, Mathf.RoundToInt(SnapshotLongEdge * normalizedAspect));
			int tall = normalizedAspect >= 1f
				? Mathf.Max(16, Mathf.RoundToInt(SnapshotLongEdge / normalizedAspect))
				: SnapshotLongEdge;

			bufferA = NewSnapshot(wide, tall);
			bufferB = NewSnapshot(wide, tall);
		}

		private static RenderTexture NewSnapshot(int width, int height)
		{
			// Linear conversion, matching the camera colour buffer the main
			// view renders into: samples read raw and land in the same
			// space the hull's own lit shading was computed in. 24 depth
			// bits because a camera cannot z-test without a depth target.
			RenderTexture snapshot = new RenderTexture(width, height, 24,
				RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			snapshot.name = "CloakBackdropRT";
			snapshot.wrapMode = TextureWrapMode.Clamp; // the warp pulls at screen edges, not from the far side
			snapshot.filterMode = FilterMode.Bilinear;
			snapshot.Create();
			return snapshot;
		}

		// One print per state change: the capture is invisible by nature
		// and a silent grabber was the failure mode that killed v2.2.
		// Compiled out of release builds; only a developer reads it.
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private static void Report(string message)
		{
			Debug.Log("[CloakBackdrop] " + message);
		}
	}
}
