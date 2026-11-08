using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

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
	/// What the copy composes (v3.2): the player's view is not one
	/// camera. It is an ordered pack, the BiRP depth+clear-flag
	/// composition this project inherited from the original game. The
	/// copy enumerates the real pack once per rig: every enabled camera
	/// between a deep floor and the world rig's depth - this game's
	/// space camera sits at depth -2 with a SolidColor clear, the
	/// planets/sun and background-sprite cameras draw over it, and the
	/// world rig (the camera the fold childrens under, v3.0) renders
	/// last. Composed alone, a child of the world rig (v3.0) copied
	/// only the pack's LAST member and the fold bent holes.
	///
	/// Who owns the copy's background: the pack member NEAREST the
	/// world rig whose clear paints one (Skybox or SolidColor). Members
	/// deeper than that clear are wiped under it in the real view the
	/// same way, so they never enter the copy. Every remaining member
	/// becomes an overlay replica stacked under the base, and the
	/// grabber itself is re-typed as the stack's FINAL overlay - the
	/// world pass renders last, exactly like the real rig. All replica
	/// masks have the UI layers stripped: the interface camera may own
	/// the background clear, but the interface never paints into the
	/// copy. Every replica mirrors its real twin (pose, FOV, clip
	/// planes, mask, depth) each frame in LateUpdate, so whatever the
	/// engine drives for the real backdrop - sector rotation, zoom,
	/// rig motion - stays live with no duplicate of that logic here.
	///
	/// Feedback: none. While the copy's world pass renders the grabber
	/// raises the _CloakCopyPass global and every fold-material hull
	/// clips itself out of the copy (ship cloak shader, v3.1): the
	/// sampled pixels are the view the player sees minus the hulls
	/// that fold - always true background, no painting feeding
	/// forward. The blog's copy is a blit of the finished frame and so
	/// feeds its own previous painting back into the next; with a
	/// camera we do not have to, which is why a parked ship stays bent
	/// space instead of diffusing toward a readable hull. The trade:
	/// anything standing directly behind a cloaking hull is swallowed
	/// by the copy the same way - the GRFS read hid things behind the
	/// actor too.
	///
	/// Cost: while any hull asks (CloakBackdropRequests), every frame
	/// pays one extra composed scene render (culling + draws at ~256px
	/// fill). While none do, the rig is disabled and nothing runs.
	/// Post-processing, shadow re-renders and the depth prepass are off
	/// for the copy: the sample is consumed as a bent smear through
	/// which a post-pass difference is not readable, and skipping the
	/// atlas keeps the cloak frame cheap.
	///
	/// Lifecycle: ProvideCamera (engine, at CreateCamera) wires it; the
	/// rig dies with its parent rig, a scene change rebuilds it. The
	/// RTs persist for the session (fixed dims, static refs) and are
	/// re-created if a domain reload dropped them.
	/// </summary>
	[DefaultExecutionOrder(1000)]
	public sealed class CloakBackdropCamera : MonoBehaviour
	{
		// The player-facing size of the grab. Fixed regardless of screen
		// resolution, so a 4K display and a foldable bend from the same
		// map and the cost does not grow with the device's pixels.
		private const int SnapshotLongEdge = 256;

		// Raised while the copy's world pass renders; every fold-material
		// hull clips itself out of the copy under it (ship cloak shader).
		// A global, not a material property: render-phase state, not art
		// state.
		private const string FoldCopyPassGlobal = "_CloakCopyPass";

		/// <summary>The engine's render camera as handed over; Unity-null when its rig is destroyed.</summary>
		private static Camera mainCamera;

		/// <summary>
		/// The camera that owns the ping-pong target - the copy's base. In
		/// a fallback rig (no background-owning camera found between the
		/// depth floor and the world rig) this is the grabber itself; with
		/// a composed stack it is the background replica. Unity-null after
		/// a scene teardown.
		/// </summary>
		private static Camera backdropCamera;

		/// <summary>The world-pass camera, the copy stack's final overlay; Unity-null in a fallback rig.</summary>
		private static Camera worldGrabber;

		/// <summary>The real backdrop cameras, in the order the view composites them.</summary>
		private static readonly List<Camera> realBackdropCameras = new();

		/// <summary>The live mirrors of the same cameras, in matching order; entry 0 is the copy base.</summary>
		private static readonly List<Camera> backdropReplicaCameras = new();

		/// <summary>Which buffer is this frame's write target; the other one holds last frame's finished copy.</summary>
		private static bool bufferAIsWrite;

		/// <summary>Aspect the RT pair was sized for; a drift beyond 1% (foldable rotation) rebuilds them.</summary>
		private static float builtAspect = -1f;

		private static RenderTexture bufferA;
		private static RenderTexture bufferB;

		// The rig scan needs the world's backdrop cameras to EXIST. Ensure
		// runs inside GameController.Awake, while the game may still be
		// mid-instantiation: the world's backdrop pack and the additive
		// scenes it rides on can arrive after, and a whole session once
		// stood on the scan of a not-yet-live rig. The fallback stays
		// live meanwhile; whenever it has not found the rig yet, the
		// scan retries on scene loads and on a slow timer, and upgrades
		// to the composed stack the moment the rig exists.
		private static bool backdropPlanPending;

		/// <summary>Frame-of-last-scan stamp; the retry is deliberately slow, world load takes under a second.</summary>
		private static int nextPlanRetryFrame;

		/// <summary>Scan attempts against the current rig; scene loads that re-open the scan restart the count.</summary>
		private static int planRetryCount;

		private const int PlanRetryInterval = 60;
		private const int PlanRetryLimit = 30;

		// A pack camera can sit well below the world rig and still own
		// the view's background: the played EngineScene drives its space
		// camera at depth -2 (the SolidColor clear + starfield pass).
		// The floor admits it; the menu itself is unreachable inside a
		// world (scenario loads are LoadSceneMode.Single).
		private const float PlanDepthFloor = -10f;

		// Every copy-rig object carries this prefix (the grabber, the
		// copy base, the replicas); the pack scan uses it as a guard so
		// a still-parked old rig can never enumerate into a new plan.
		private const string CopyRigPrefix = "CloakBackdrop";

		/// <summary>UI layers stripped from every replica mask (cached once, lazily - never in a field initializer).</summary>
		private static int uiStripMaskValue;

		private static bool uiStripMaskReady;

#if UNITY_EDITOR
		/// <summary>
		/// Rig state the dump table last printed for; a plan that keeps
		/// failing prints its camera table once per distinct state
		/// (dev builds only) instead of dying silently - the lesson that
		/// killed v2.2: a copy rig that composes nothing looks like a
		/// ghost.
		/// </summary>
		private static string lastRigDumpForDiagnostics;
#endif

		/// <summary>Mask of the UI layers to strip from copy-rig cameras; 0 when no UI layer exists.</summary>
		private static int GetUiStripMask()
		{
			if (!uiStripMaskReady)
			{
				int uiLayer = LayerMask.NameToLayer("UI");
				uiStripMaskValue = uiLayer >= 0 ? 1 << uiLayer : 0;
				uiStripMaskReady = true;
			}
			return uiStripMaskValue;
		}

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
				// A stack is not rendered when its base is disabled, so
				// disabling the base parks the whole copy rig; the world
				// pass is parked alongside it. Belt and braces for the
				// frame between teardown and rebuild - the objects die
				// with their rig anyway.
				if (backdropCamera != null)
				{
					backdropCamera.enabled = false;
				}
				if (worldGrabber != null && worldGrabber.enabled)
				{
					worldGrabber.enabled = false;
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
			// The driver component is not decoration - it carries every
			// runtime behaviour of this system: the wants-loop that enables
			// and aims the camera each frame, the ping-pong bookkeeping,
			// the _CloakCopyPass render hooks, the superseded-rig parking
			// and the deferred rig scan. A camera without it is inertia:
			// enabled nowhere, aims nowhere, and every folding hull samples
			// an empty buffer (v3.1 lost this attach in a rewrite and the
			// result shipped as "the grey ship again"). Never drop it.
			grabberObject.AddComponent<CloakBackdropCamera>();
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
			// The main camera's own scene, minus HUD canvases: the world
			// pass must frame the world the hull stands in front of, not
			// the interface painted over it. Everything else travels -
			// ships, nebulae, the sun quad of the world pass, particles.
			int uiLayer = LayerMask.NameToLayer("UI");
			grabber.cullingMask = uiLayer >= 0 ? (camera.cullingMask & ~(1 << uiLayer)) : camera.cullingMask;
			// Early in the frame loop, before its parent renders (depth
			// ascending). Only the fallback keeps this camera as its own
			// base; with a stack the depth is re-set to the world rig's own
			// so any internal ordering agrees with the stack order.
			grabber.depth = camera.depth - 10f;
			grabber.fieldOfView = camera.fieldOfView;
			grabber.nearClipPlane = camera.nearClipPlane;
			grabber.farClipPlane = camera.farClipPlane;
			grabber.enabled = false; // LateUpdate enables the rig while any hull wants the copy

			UniversalAdditionalCameraData grabberData = grabber.GetUniversalAdditionalCameraData();
			grabberData.renderPostProcessing = false;
			grabberData.renderShadows = false;
			grabberData.requiresDepthOption = CameraOverrideOption.Off;

			// v3.1: compose the copy the way the view composites. ONE scan,
			// once per rig creation - never on the frame path.
			if (BuildBackdropStack(grabber, grabberObject))
			{
				ApplyStackedPlan(grabber, grabberData);
			}
			else
			{
				// No background-owning camera between the floor and the
				// world rig (an empty or odd world): the single-camera copy
				// of v3.0, with one correction - the buffer is sky-cleared.
				// Without that the copy kept unwritten pixels between the
				// nebula quads and the fold bent stale color.
				grabber.clearFlags = CameraClearFlags.Skybox;
				backdropCamera = grabber;
				worldGrabber = null;
				realBackdropCameras.Clear();
				backdropReplicaCameras.Clear();
				// Almost always this scan simply ran before the world's
				// backdrop cameras were instantiated. Stay live as the
				// fallback and re-scan until the rig exists.
				backdropPlanPending = true;
				nextPlanRetryFrame = 0;
				planRetryCount = 0;
				Report("no background camera found - copy base is the grabber itself (covers no pack members); scan deferred");
			}

			Report($"grabber attached under '{camera.name}'");
		}

		/// <summary>
		/// Mirror the real view's backdrop pack into a copy stack. The
		/// pack: every enabled camera between a deep floor and the world
		/// rig's depth, sorted ascending. The copy's base is the member
		/// NEAREST the world rig whose clear owns a background (Skybox or
		/// SolidColor); members deeper than that clear are wiped under it
		/// in the real view and never enter the copy. Every remaining
		/// member becomes an overlay replica stacked under the base, and
		/// the grabber itself is re-typed as the stack's final overlay
		/// (the world pass renders last, like the real rig). All replica
		/// masks have the UI layers stripped so the interface can never
		/// paint into the copy. Returns false - the grabber stays a
		/// plain single-camera copy, the caller in fallback mode - when
		/// no background-owning camera exists.
		/// </summary>
		private static bool BuildBackdropStack(Camera grabber, GameObject grabberObject)
		{
			// A prior compose may ride on this grabber (scene loads
			// re-open the plan): tear its replicas and stack list down
			// before rebuilding, so no mirror leaks and the world pass is
			// only ever on one stack.
			if (backdropReplicaCameras.Count > 0)
			{
				Camera oldBase = backdropReplicaCameras[0];
				if (oldBase != null)
				{
					oldBase.GetUniversalAdditionalCameraData().cameraStack.Clear();
				}
				foreach (Camera replica in backdropReplicaCameras)
				{
					if (replica != null)
					{
						Object.Destroy(replica.gameObject);
					}
				}
				backdropReplicaCameras.Clear();
			}
			realBackdropCameras.Clear();

			// One enumeration at rig creation or plan retry - never the
			// frame path. Inactive is INCLUDED on purpose: the pack filter
			// tests liveness itself, and the diagnostics table is allowed
			// to name cameras that are switched off.
			List<Camera> sceneCameras = new List<Camera>(
				Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));

#if UNITY_EDITOR
			// The rig table prints once per distinct camera state at the
			// retry cadence (2 Hz worst case) - never per frame.
			string rigDump = DumpRigForDiagnostics(sceneCameras);
			if (rigDump != lastRigDumpForDiagnostics)
			{
				lastRigDumpForDiagnostics = rigDump;
				Report(rigDump);
			}
#endif

			foreach (Camera candidate in sceneCameras)
			{
				if (candidate == null || candidate == grabber
					|| !candidate.enabled || !candidate.gameObject.activeInHierarchy
					|| candidate.name.StartsWith(CopyRigPrefix)
					|| candidate.depth < PlanDepthFloor
					|| candidate.depth >= mainCamera.depth)
				{
					continue;
				}
				realBackdropCameras.Add(candidate);
			}
			if (realBackdropCameras.Count == 0)
			{
				Report("no enabled camera between the depth floor and the world rig - copy stays single-camera fallback");
				return false;
			}
			// The view composites far to near by depth - the deepest camera first.
			realBackdropCameras.Sort((a, b) => a.depth.CompareTo(b.depth));

			// The copy base owns the buffer's background: the pack member
			// NEAREST the world rig whose clear paints one (Skybox or
			// SolidColor). Deeper members die under that clear in the real
			// view the same way, so they are excluded from the copy.
			int baseIndex = -1;
			for (int i = realBackdropCameras.Count - 1; i >= 0; i--)
			{
				CameraClearFlags flags = realBackdropCameras[i].clearFlags;
				if (flags == CameraClearFlags.Skybox || flags == CameraClearFlags.SolidColor)
				{
					baseIndex = i;
					break;
				}
			}
			if (baseIndex < 0)
			{
				// No member paints a background: with nothing owning the
				// buffer, a stack would only ever show stale pixels.
				Report("no background-owning camera found (none clears Skybox or SolidColor) - copy stays single-camera fallback");
				realBackdropCameras.Clear();
				return false;
			}
			if (baseIndex > 0)
			{
				realBackdropCameras.RemoveRange(0, baseIndex);
			}

			// Everything is parented under the grabber object, so the copy
			// rig dies with the engine's rig and parks with a superseded
			// grab. Pose and settings are mirrored per frame afterwards.
			for (int i = 0; i < realBackdropCameras.Count; i++)
			{
				Camera real = realBackdropCameras[i];
				bool isBase = i == baseIndex;
				GameObject replicaObject = new GameObject(
					isBase ? "CloakBackdropCopyBase" : "CloakBackdrop_" + real.name);
				replicaObject.transform.SetParent(grabberObject.transform, worldPositionStays: false);
				Camera replica = replicaObject.AddComponent<Camera>();
				// Parked until a hull wants the copy, like the grabber
				// itself: a newborn enabled camera copies the whole scene
				// to the screen for a frame and can be swept into the
				// real view's stack by a scene load landing same frame.
				replica.enabled = false;
				MirrorBackdropCamera(real, replica);
				replica.clearFlags = isBase ? real.clearFlags : CameraClearFlags.Nothing;
				replica.backgroundColor = real.backgroundColor;

				UniversalAdditionalCameraData replicaData = replica.GetUniversalAdditionalCameraData();
				replicaData.renderType = isBase ? CameraRenderType.Base : CameraRenderType.Overlay;
				// The same cheap-copy rules as the grabber: no post, no
				// shadow re-render, no depth prepass. The sample is a
				// bent one-frame-old smear; none of those layers survive
				// to it.
				replicaData.renderPostProcessing = false;
				replicaData.renderShadows = false;
				replicaData.requiresDepthOption = CameraOverrideOption.Off;

				backdropReplicaCameras.Add(replica);
			}

			// Stack order is the render order: base background first, then
			// the rest of the backdrop pack ascending, then the world pass
			// last. The grabber must be OVERLAY-TYPED BEFORE it is added -
			// URP rejects a Base camera being added to a stack, and the
			// world pass would silently never render into the copy.
			UniversalAdditionalCameraData grabberData = grabber.GetUniversalAdditionalCameraData();
			grabberData.renderType = CameraRenderType.Overlay;
			UniversalAdditionalCameraData baseData = backdropReplicaCameras[0].GetUniversalAdditionalCameraData();
			for (int i = 1; i < backdropReplicaCameras.Count; i++)
			{
				baseData.cameraStack.Add(backdropReplicaCameras[i]);
			}
			baseData.cameraStack.Add(grabber);
			return true;
		}

		/// <summary>Copy every render-relevant setting of a real backdrop camera onto its replica, once, at build.</summary>
		private static void MirrorBackdropCamera(Camera real, Camera replica)
		{
			replica.transform.SetPositionAndRotation(real.transform.position, real.transform.rotation);
			replica.orthographic = real.orthographic;
			replica.fieldOfView = real.fieldOfView;
			replica.nearClipPlane = real.nearClipPlane;
			replica.farClipPlane = real.farClipPlane;
			// The UI layers are stripped here exactly like the per-frame
			// mirror: a replica never paints the interface into the copy,
			// even when the interface camera is itself a pack member - a
			// background clear is inherited, interface content is not.
			replica.cullingMask = real.cullingMask & ~GetUiStripMask();
			replica.depth = real.depth;
		}

		/// <summary>
		/// Turn the grabber into the composed stack's final world overlay.
		/// A stack member's own depth is not the render order (the stack
		/// list is), but mirroring the real rig's keeps any internal sort
		/// agreeing: base (sky) first, replicas ascending, the world pass
		/// last. Used by Ensure at build time and by the deferred scan.
		/// </summary>
		private static void ApplyStackedPlan(Camera grabber, UniversalAdditionalCameraData grabberData)
		{
			grabber.depth = mainCamera.depth;
			// Overlays never clear; the base's background clear owns the
			// buffer, exactly like the real view.
			grabber.clearFlags = CameraClearFlags.Nothing;
			grabberData.renderType = CameraRenderType.Overlay;

			backdropCamera = backdropReplicaCameras[0];
			worldGrabber = grabber;
			Report("copy stack composed: " + backdropReplicaCameras.Count + " cameras mirror the real backdrop rig");
		}

		/// <summary>
		/// Retry the backdrop-rig scan once the world's cameras exist.
		/// Called at a slow interval from LateUpdate; scene loads retry
		/// immediately. On success the grabber upgrades from the fallback
		/// to the composed stack; a world with no background-owning
		/// camera settles the fallback for good after a bounded number
		/// of attempts.
		/// </summary>
		private void TryRebuildBackdropPlan()
		{
			if (Time.frameCount < nextPlanRetryFrame)
			{
				return;
			}
			nextPlanRetryFrame = Time.frameCount + PlanRetryInterval;

			// The grabber is always the object this component lives on,
			// and the plan is rebuilt against the same rig it rides.
			Camera grabber = GetComponent<Camera>();
			if (grabber == null || mainCamera == null)
			{
				return;
			}

			if (!BuildBackdropStack(grabber, grabber.gameObject))
			{
				if (++planRetryCount >= PlanRetryLimit)
				{
					// Concluded: this world composes no background-owning
					// camera. The skycleared fallback stays; scene loads
					// re-open the scan.
					backdropPlanPending = false;
					Report("backdrop scan gave up - staying single-camera fallback");
				}
				return;
			}

			planRetryCount = 0;
			backdropPlanPending = false;
			// The fallback held the target on the grabber itself; the
			// stacked plan hands it to the base replica and re-types the
			// grabber an overlay. Clear the stale reference so no camera
			// carries a build-time target into the stack.
			grabber.targetTexture = null;
			ApplyStackedPlan(grabber, grabber.GetUniversalAdditionalCameraData());
			Report("copy stack composed after deferred scan");
		}

		// Scene loads are the earliest the world's replica cameras can be
		// present, and they are an event, not the frame path.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void RegisterSceneLoadRetry()
		{
			SceneManager.sceneLoaded -= OnSceneLoadedForRetry;
			SceneManager.sceneLoaded += OnSceneLoadedForRetry;
		}

		private static void OnSceneLoadedForRetry(Scene scene, LoadSceneMode mode)
		{
			// Every scene load re-opens the scan: the world's backdrop rig
			// can arrive, the menu can leave, and even a COMPOSED plan is
			// stale the moment the pack changes (a UI scene adding a
			// depth-0 background-clearing camera, for instance).
			// Rebuilds are safe - BuildBackdropStack tears the previous
			// replicas and stack list down first.
			nextPlanRetryFrame = 0;
			planRetryCount = 0;
			backdropPlanPending = mainCamera != null;
		}

		private void LateUpdate()
		{
			// Superseded driver: two rigs can carry grabbers across a world
			// swap (the log once showed two 'grabber attached' lines and two
			// 'parked' sweeps from the same rig). Only the instance whose
			// camera is the current world pass may act; the stale one parks
			// its whole copy rig (children included) so nothing renders
			// every frame for a buffer nobody samples.
			Camera ownGrabber = GetComponent<Camera>();
			// Not the current world pass, and not the fallback's own base:
			// a superseded rig from a previous Ensure.
			if (ownGrabber == null || (ownGrabber != worldGrabber && ownGrabber != backdropCamera))
			{
				if (ownGrabber != null)
				{
					if (ownGrabber.enabled)
					{
						ownGrabber.enabled = false;
					}
					// The copy base and replicas ride under this object.
					foreach (Camera childCamera in ownGrabber.GetComponentsInChildren<Camera>())
					{
						if (childCamera != ownGrabber && childCamera.enabled)
						{
							childCamera.enabled = false;
						}
					}
				}
				return;
			}

			bool wants = backdropCamera != null && mainCamera != null && CloakBackdropRequests.Any;

			// A rig whose scan ran too early (the world's backdrop cameras
			// are instantiated after the camera rig) re-scans here: slow,
			// event-gated, and only while the fallback is standing in.
			if (backdropPlanPending)
			{
				TryRebuildBackdropPlan();
			}

			if (!wants)
			{
				if (backdropCamera != null && backdropCamera.enabled)
				{
					backdropCamera.enabled = false;
					Report("grabber parked (no hull is cloaking)");
				}
				return;
			}

			// Mirror the rig every active frame BEFORE rendering (which
			// happens after all LateUpdates): the engine poses the real
			// backdrop cameras at order 0, we run at 1000, so the copy
			// renders through this frame's freshest geometry.
			MirrorBackdropRig();

			FormatBuffers(mainCamera.aspect);

			// Ping-pong: this frame's render fills one buffer while the
			// global stays on the other - the PREVIOUS frame's finished
			// copy. The switch happens now, before any camera of this frame
			// has rendered, so all the copy's passes and the main camera's
			// hulls sample a stable buffer all frame long.
			bufferAIsWrite = !bufferAIsWrite;
			RenderTexture write = bufferAIsWrite ? bufferA : bufferB;
			RenderTexture read = bufferAIsWrite ? bufferB : bufferA;

			if (!backdropCamera.enabled)
			{
				backdropCamera.enabled = true;
				if (worldGrabber != null && !worldGrabber.enabled)
				{
					worldGrabber.enabled = true;
				}
				// Stack members ride with the base: raise them all at once,
				// a disabled overlay is skipped by the stack.
				foreach (Camera replica in backdropReplicaCameras)
				{
					if (replica != null && !replica.enabled)
					{
						replica.enabled = true;
					}
				}
				Report("grabbing previous frames while a cloaking hull wants them");
			}
			// Only the base holds the target; the overlays of a stack
			// render into the base's buffers by construction. Parking and
			// enabling move the whole rig at once (base + members).
			backdropCamera.targetTexture = write;

			Shader.SetGlobalTexture("_CloakBackdropRT", read);
		}

		/// <summary>
		/// Per-frame pose and settings mirror. The engine's own
		/// LateUpdates (order 0) have already posed the real backdrop
		/// cameras when ours runs (execution order 1000); rendering
		/// happens after every script, so the copy always renders through
		/// the frame's freshest rig - sector rotation, zoom and camera
		/// motion included, with no duplication of that logic here.
		/// </summary>
		private static void MirrorBackdropRig()
		{
			// The world pass mirrors its own parent (zoom rides on FOV in
			// this game); pose is free through the parent-child transform.
			if (worldGrabber != null && mainCamera != null
				&& worldGrabber.fieldOfView != mainCamera.fieldOfView)
			{
				worldGrabber.fieldOfView = mainCamera.fieldOfView;
			}

			int count = realBackdropCameras.Count;
			for (int i = 0; i < count; i++)
			{
				Camera real = realBackdropCameras[i];
				Camera replica = backdropReplicaCameras[i];
				// A real camera can die mid-session (scene teardown); the
				// replica freezes on its last pose rather than touching a
				// destroyed transform.
				if (real == null || replica == null)
				{
					continue;
				}
				replica.transform.SetPositionAndRotation(real.transform.position, real.transform.rotation);
				if (replica.fieldOfView != real.fieldOfView)
				{
					replica.fieldOfView = real.fieldOfView;
				}
				if (replica.nearClipPlane != real.nearClipPlane)
				{
					replica.nearClipPlane = real.nearClipPlane;
				}
				if (replica.farClipPlane != real.farClipPlane)
				{
					replica.farClipPlane = real.farClipPlane;
				}
				// The UI layers never reach the copy through a re-mirrored
				// mask either: strip at the same moment the mask moves.
				int strippedMask = real.cullingMask & ~GetUiStripMask();
				if (replica.cullingMask != strippedMask)
				{
					replica.cullingMask = strippedMask;
				}
				if (replica.depth != real.depth)
				{
					replica.depth = real.depth;
				}
			}
		}

		private void OnEnable()
		{
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
		}

		private void OnDisable()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
			// The rig can die between the raise and the release (a teardown
			// mid-frame); the hulls must never clip outside the copy.
			Shader.SetGlobalFloat(FoldCopyPassGlobal, 0f);
		}

		// The copy must not contain the hulls that fold (the fold would
		// sample its own previous painting, and with a visibility floor
		// > 0 that recursion converges to hull). Raised exactly around
		// the copy's world pass: begin raises, end releases, and nothing
		// else - the sky and backdrop passes have no fold material, and
		// probes and previews fire for their own cameras and see the
		// global at 0.
		private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (IsCopyWorldPass(camera))
			{
				Shader.SetGlobalFloat(FoldCopyPassGlobal, 1f);
			}
		}

		private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (IsCopyWorldPass(camera))
			{
				Shader.SetGlobalFloat(FoldCopyPassGlobal, 0f);
			}
		}

		// Unity-null comparison (== null is the destroyed-tested overload);
		// the fallback's single camera is its own world pass.
		private static bool IsCopyWorldPass(Camera camera)
		{
			return camera != null
				&& (camera == worldGrabber || (worldGrabber == null && camera == backdropCamera));
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

#if UNITY_EDITOR
		// The camera table a failing scan prints, one line per camera.
		// Built only here, at retry cadence, dev builds only - the same
		// "no state change without a print" discipline that beat v2.2's
		// silent capture.
		private static string DumpRigForDiagnostics(List<Camera> sceneCameras)
		{
			string text = "camera rig scan (" + sceneCameras.Count + " cameras, floor " + PlanDepthFloor
				+ ", world rig at "
				+ (mainCamera != null ? mainCamera.depth.ToString("0.##") : "null") + ")";
			foreach (Camera candidate in sceneCameras)
			{
				if (candidate == null)
				{
					text += "\n  <destroyed camera>";
					continue;
				}
				text += "\n  '" + candidate.name + "'"
					+ " depth=" + candidate.depth.ToString("0.##")
					+ " clear=" + candidate.clearFlags
					+ " mask=" + candidate.cullingMask
					+ " activeInHierarchy=" + candidate.gameObject.activeInHierarchy
					+ " enabled=" + candidate.enabled
					+ " scene='" + candidate.gameObject.scene.name + "'";
			}
			return text;
		}
#endif

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
