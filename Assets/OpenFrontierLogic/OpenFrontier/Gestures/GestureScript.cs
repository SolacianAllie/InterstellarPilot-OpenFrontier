using System;
using System.Collections.Generic;
using OpenFrontier;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ISTouchPhase = UnityEngine.InputSystem.TouchPhase;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace OpenFrontier.Gestures
{
	/// <summary>
	/// Gesture driver: the runtime half of the Fingers.dll replacement.
	/// Registry + per-frame touch pump in the shape the consumers were
	/// written against (AddGesture / RemoveGesture / ResetState /
	/// HasInstance), fed from the New Input System's EnhancedTouch instead
	/// of the legacy UnityEngine.Input that Fingers.dll polled - which is
	/// the whole reason for the replacement: the legacy input module is
	/// unusable under Active Input Handling "New Only". Phase A of the
	/// input migration; the player-side flip to New Only is Phase B.
	///
	/// Ported behaviour, verified line by line against the decompiled
	/// Fingers source (ilspycmd, 2026-10):
	/// - every touch any phase lands in currentTouchIds; began/moved/
	///   ended lists are rebuilt per frame (Stationary joins moved);
	/// - mouse buttons ride along as three fingers with Fingers' device
	///   ids (0x7FFFFFF5/44/43) and the button index as
	///   PlatformSpecificTouch, which is what makes
	///   GestureRecognizerExtensions.IsRightMouseButton distinguish a
	///   right-mouse tap from a finger tap; a held-but-stationary button
	///   reports Moved every frame, as Fingers did;
	/// - lost touches (tracked last frame, unseen this one) do NOT reach
	///   the gestures as Ended - the owning gesture is silently reset and
	///   its tracked ids cleaned from the pointers table, exactly Fingers'
	///   ProcessLostTouches;
	/// - the raycast cascade resolves ONLY at touch begin, per pointer:
	///   EventSystem.RaycastAll, then Physics2D.OverlapPointNonAlloc hits
	///   deduped by GameObject identity, a blank row when nothing was
	///   hit, then the capture walk (ForcePassThrough seen -> later
	///   ForceDeny skipped; after a pass-through only a raycast object
	///   that is somebody's PlatformSpecificView still collects);
	/// - registered gestures receive filtered began / all moved / all
	///   ended in registration order (all priorities are the default 0
	///   here, and Fingers' insertion sort was stable), and an ended
	///   gesture keeps its claim on the stream until the end of the pump
	///   frame - the one-frame grace in Fingers' active-gesture handling;
	/// - scene unloaded runs the ClearAllGestures reset (the old
	///   serialized LevelUnloadOption=1), as do pause and focus loss.
	///
	/// Not ported because no consumer ever used it: gesture masks /
	/// AddMask, ConsumeTouch, CaptureGestureHandler, the virtual-touch
	/// API, the mouse-wheel-as-two-fingers simulation (the camera screens
	/// run their own wheel code through the LegacyInput shim), ShowTouches
	/// display objects. If a future consumer needs one, port it from the
	/// decompiled Fingers source - the source is portable, memory is not
	/// (the decompile lives its one good day via ilspycmd).
	/// </summary>
	public class GestureScript : MonoBehaviour
	{
		private static GestureScript singleton;

		/// <summary>The old serialized component ran with ClearAllGestures.</summary>
		public GestureLevelUnloadOption LevelUnloadOption = GestureLevelUnloadOption.ClearAllGestures;

		public List<GameObject> PassThroughObjects = new List<GameObject>();

		/// <summary>
		/// Serialized true on the old GameController component; auto-off
		/// when no pointer device exists (Mouse.current == null), the same
		/// edge Fingers' HasMouse() walked.
		/// </summary>
		public bool TreatMousePointerAsFinger = true;

		private readonly List<GestureRecognizer> gestures = new List<GestureRecognizer>();

		private readonly List<GestureRecognizer> gesturesSnapshot = new List<GestureRecognizer>();

		private readonly List<GestureTouch> touchesBegan = new List<GestureTouch>();

		private readonly List<GestureTouch> touchesMoved = new List<GestureTouch>();

		private readonly List<GestureTouch> touchesEnded = new List<GestureTouch>();

		private readonly List<GestureTouch> filteredTouches = new List<GestureTouch>();

		private readonly HashSet<int> currentTouchIds = new HashSet<int>();

		private readonly List<GestureTouch> previousTouches = new List<GestureTouch>();

		private readonly List<GestureTouch> tempTouches = new List<GestureTouch>();

		private readonly Dictionary<int, List<GameObject>> gameObjectsForTouch = new Dictionary<int, List<GameObject>>();

		private readonly Dictionary<int, Vector2> previousTouchPositions = new Dictionary<int, Vector2>();

		private readonly List<RaycastResult> captureRaycastResults = new List<RaycastResult>();

		private readonly Collider2D[] overlapResults = new Collider2D[128];

		private readonly List<Component> components = new List<Component>();

		private readonly HashSet<Type> componentTypesToDenyPassThrough = new HashSet<Type>();

		private readonly HashSet<Type> componentTypesToIgnorePassThrough = new HashSet<Type>();

		private readonly List<GameObject> livePassThroughObjects = new List<GameObject>();

		private int oldScreenX;

		private int oldScreenY;

		public const int MousePointerId0 = 2147483645;

		public const int MousePointerId1 = 2147483644;

		public const int MousePointerId2 = 2147483643;

		public bool MousePresent => HasMouse;

		/// <summary>
		/// The scale recognizer's ZoomSpeed default (3 desktop, 1 phone)
		/// hangs off this - Mouse.current is null on phones without a
		/// pointer, the same signal the LegacyInput shim uses.
		/// </summary>
		public static bool HasMouse => Mouse.current != null;

		public static bool HasInstance => singleton != null;

		public static GestureScript Instance
		{
			get
			{
				if (singleton == null)
				{
					GameObject gameObject = new GameObject("GestureScript");
					gameObject.AddComponent<GestureScript>();
					UnityEngine.Object.DontDestroyOnLoad(gameObject);
				}
				return singleton;
			}
		}

		private void Awake()
		{
			if (singleton != null && singleton != this)
			{
				UnityEngine.Object.Destroy(gameObject);
				return;
			}
			singleton = this;
			ApplyScreenScale();
			SetupDefaultPassThroughComponents();
			SceneManager.sceneUnloaded += SceneManagerSceneUnloaded;
			oldScreenX = Screen.width;
			oldScreenY = Screen.height;
		}

		private void OnDestroy()
		{
			if (singleton == this)
			{
				singleton = null;
			}
		}

		private void OnEnable()
		{
			EnhancedTouchSupport.Enable();
			SceneManager.sceneLoaded += SceneLoaded;
		}

		private void OnDisable()
		{
			SceneManager.sceneLoaded -= SceneLoaded;
			ResetState(false);
		}

		/// <summary>
		/// GestureUnits fed from OpenFrontier.ScreenDpi - the JNI-backed
		/// value with its own fallback. FingersScript.Awake read Screen.dpi
		/// raw (0 on Android foldables and Linux) and needed FingersDpiFix
		/// patched in afterwards; here the correct value is the only value,
		/// and this replaces that component's whole job.
		/// </summary>
		private void ApplyScreenScale()
		{
			GestureUnits.PixelsPerInch = ScreenDpi.Value;
			GestureUnits.UnitMultiplier = GestureUnits.PixelsPerInch;
		}

		private void SceneLoaded(Scene scene, LoadSceneMode mode)
		{
			// The fold swap and resolution changes re-resolve the backing
			// DisplayMetrics - the same re-apply FingersDpiFix performed.
			ScreenDpi.Invalidate();
			ApplyScreenScale();
			oldScreenX = Screen.width;
			oldScreenY = Screen.height;
		}

		private void SceneManagerSceneUnloaded(Scene scene)
		{
			switch (LevelUnloadOption)
			{
			case GestureLevelUnloadOption.ResetGestureState:
				ResetState(false);
				break;
			case GestureLevelUnloadOption.ClearAllGestures:
				ResetState(true);
				break;
			}
		}

		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				ResetState(false);
			}
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (!hasFocus)
			{
				ResetState(false);
			}
		}

		private void Update()
		{
			// Fold/resolution watcher (unscaled sizes; Screen.width/height
			// are already the post-change values when the change happens).
			if (Screen.width != oldScreenX || Screen.height != oldScreenY)
			{
				ScreenDpi.Invalidate();
				ApplyScreenScale();
				oldScreenX = Screen.width;
				oldScreenY = Screen.height;
			}
			InternalUpdate();
		}

		private void SetupDefaultPassThroughComponents()
		{
			// The same deny list FingersScript.SetupDefaultPassThrough
			// installed: gestures are refused over these controls so the
			// camera drag does not fire under a HUD button.
			componentTypesToDenyPassThrough.Add(typeof(Scrollbar));
			componentTypesToDenyPassThrough.Add(typeof(Button));
			componentTypesToDenyPassThrough.Add(typeof(Dropdown));
			componentTypesToDenyPassThrough.Add(typeof(Toggle));
			componentTypesToDenyPassThrough.Add(typeof(Slider));
			componentTypesToDenyPassThrough.Add(typeof(InputField));
			componentTypesToDenyPassThrough.Add(typeof(UnityEngine.UI.ScrollRect));
			componentTypesToIgnorePassThrough.Add(typeof(Text));
		}

		private void CleanupPassThroughObjects()
		{
			// Destroyed entries (Unity-null) leave; scene switch
			// especially empties the list, parity with Fingers'
			// CleanupPassThroughObjects.
			livePassThroughObjects.Clear();
			foreach (GameObject item in PassThroughObjects)
			{
				if (item != null)
				{
					livePassThroughObjects.Add(item);
				}
			}
			if (livePassThroughObjects.Count != PassThroughObjects.Count)
			{
				PassThroughObjects.Clear();
				PassThroughObjects.AddRange(livePassThroughObjects);
			}
		}

		public bool AddGesture(GestureRecognizer gesture)
		{
			if (gesture == null || gestures.Contains(gesture))
			{
				return false;
			}
			gestures.Add(gesture);
			return true;
		}

		public bool RemoveGesture(GestureRecognizer gesture)
		{
			if (gesture != null)
			{
				// Fingers called Dispose() here, which for our port's
				// feature set is exactly "leave the active set" - Reset
				// does that and restores a clean Possible for a future
				// re-add. Every consumer re-creates its recognizers on
				// OnEnable, so the reset can never fight live state.
				gesture.Reset();
				return gestures.Remove(gesture);
			}
			return false;
		}

		/// <summary>
		/// Same contract as FingersScript.ResetState: every registered
		/// recognizer reset; with clearGestures the registry itself is
		/// emptied. ItemAutoMultiSelect's long-press multi-select calls it
		/// mid-frame so a lingering tap cannot fire afterwards.
		/// </summary>
		public void ResetState(bool clearGestures)
		{
			foreach (GestureRecognizer gesture in gestures)
			{
				gesture.Reset();
			}
			if (clearGestures)
			{
				gestures.Clear();
			}
			touchesBegan.Clear();
			touchesMoved.Clear();
			touchesEnded.Clear();
			gameObjectsForTouch.Clear();
			previousTouches.Clear();
			previousTouchPositions.Clear();
		}

		private void InternalUpdate()
		{
			CleanupPassThroughObjects();
			currentTouchIds.Clear();
			touchesBegan.Clear();
			touchesMoved.Clear();
			touchesEnded.Clear();
			CollectTouchTouches();
			CollectMouseTouches();
			ProcessLostTouches();
			foreach (GestureTouch baked in touchesBegan)
			{
				PopulateGameObjectsForTouch(baked.Id, baked.X, baked.Y);
			}
			if (gestures.Count > 0)
			{
				// Snapshot: a consumer callback may touch the registry
				// (ItemAutoMultiSelect's ResetState path runs from Update,
				// so Fingers never hit this; the snapshot makes the pump
				// re-entrant anyway at no measurable cost).
				gesturesSnapshot.Clear();
				gesturesSnapshot.AddRange(gestures);
				foreach (GestureRecognizer gesture in gesturesSnapshot)
				{
					gesture.ProcessTouchesBegan(FilterTouchesBegan(touchesBegan, gesture));
					gesture.ProcessTouchesMoved(touchesMoved);
					gesture.ProcessTouchesEnded(touchesEnded);
				}
				foreach (GestureRecognizer gesture in gesturesSnapshot)
				{
					gesture.OnFrameTick();
				}
				GestureRecognizer.EndOfFrameActiveMaintenance();
			}
			foreach (GestureTouch ended in touchesEnded)
			{
				gameObjectsForTouch.Remove(ended.Id);
			}
		}

		private void CollectTouchTouches()
		{
			ReadOnlyArray<ETouch> activeTouches = ETouch.activeTouches;
			for (int i = 0; i < activeTouches.Count; i++)
			{
				ETouch touch = activeTouches[i];
				ISTouchPhase phase = touch.phase;
				Vector2 position = touch.screenPosition;
				int fingerId = touch.touchId;
				Vector2 previous = position;
				if (previousTouchPositions.TryGetValue(fingerId, out Vector2 stored))
				{
					previous = stored;
				}
				TouchPhase mapped;
				switch (phase)
				{
				case ISTouchPhase.None:
					mapped = TouchPhase.Unknown;
					break;
				case ISTouchPhase.Began:
					mapped = TouchPhase.Began;
					break;
				case ISTouchPhase.Moved:
					mapped = TouchPhase.Moved;
					break;
				case ISTouchPhase.Stationary:
					mapped = TouchPhase.Stationary;
					break;
				case ISTouchPhase.Ended:
					mapped = TouchPhase.Ended;
					break;
				case ISTouchPhase.Canceled:
					// Unity's InputSystem enum spells it with one 'l'.
					mapped = TouchPhase.Cancelled;
					break;
				default:
					mapped = TouchPhase.Unknown;
					break;
				}
				GestureTouch g = new GestureTouch(fingerId, position.x, position.y, previous.x, previous.y, touch.pressure, null, mapped);
				ProcessTouch(ref g);
				previousTouchPositions[fingerId] = position;
			}
		}

		private void CollectMouseTouches()
		{
			if (!HasMouse || !TreatMousePointerAsFinger)
			{
				return;
			}
			Vector2 mousePosition = Mouse.current.position.ReadValue();
			AddMouseTouch(0, MousePointerId0, mousePosition);
			AddMouseTouch(1, MousePointerId1, mousePosition);
			AddMouseTouch(2, MousePointerId2, mousePosition);
		}

		private void AddMouseTouch(int index, int pointerId, Vector2 mousePosition)
		{
			float x = mousePosition.x;
			float y = mousePosition.y;
			var mouse = Mouse.current;
			if (mouse == null)
			{
				return;
			}
			ButtonControl button = index switch
			{
				0 => mouse.leftButton,
				1 => mouse.rightButton,
				2 => mouse.middleButton,
				_ => null,
			};
			if (button == null)
			{
				return;
			}
			TouchPhase touchPhase;
			if (button.wasPressedThisFrame)
			{
				touchPhase = TouchPhase.Began;
			}
			else if (button.wasReleasedThisFrame)
			{
				touchPhase = TouchPhase.Ended;
			}
			else if (button.isPressed)
			{
				// Held but stationary still reports Moved every frame,
				// exactly Fingers' AddMouseTouch (no-delta skips are what
				// its SimulateMouseWithTouches path did instead).
				touchPhase = TouchPhase.Moved;
			}
			else
			{
				return;
			}
			Vector2 previous = mousePosition;
			if (previousTouchPositions.TryGetValue(pointerId, out Vector2 stored))
			{
				previous = stored;
			}
			GestureTouch g = new GestureTouch(pointerId, x, y, previous.x, previous.y, 1f, index, touchPhase);
			ProcessTouch(ref g);
			previousTouchPositions[pointerId] = mousePosition;
		}

		/// <summary>
		/// FingersProcessTouch: route a delivered touch into the frame's
		/// phase lists and the currently-down bookkeeping.
		/// </summary>
		private void ProcessTouch(ref GestureTouch g)
		{
			currentTouchIds.Add(g.Id);
			switch (g.TouchPhase)
			{
			case TouchPhase.Began:
				if (!previousTouches.Contains(g))
				{
					previousTouches.Add(g);
				}
				touchesBegan.Add(g);
				previousTouchPositions[g.Id] = new Vector2(g.X, g.Y);
				break;
			case TouchPhase.Moved:
			case TouchPhase.Stationary:
				// Already in previousTouches from its Began; Fingers'
				// FingersContinueTouch only topped up the position map.
				touchesMoved.Add(g);
				previousTouchPositions[g.Id] = new Vector2(g.X, g.Y);
				break;
			default:
				touchesEnded.Add(g);
				previousTouchPositions.Remove(g.Id);
				previousTouches.Remove(g);
				break;
			}
		}

		/// <summary>
		/// Tracked-last-frame, unseen-this-frame pointers die quietly: the
		/// owning gesture is reset (no synthetic Ended is delivered), and
		/// the pointer id leaves both position and down tables. Fingers'
		/// ProcessLostTouches verbatim, minus the duplicate temp-entry it
		/// collected (harmless there, pointless here).
		/// </summary>
		private void ProcessLostTouches()
		{
			if (previousTouches.Count == 0)
			{
				return;
			}
			foreach (GestureTouch previousTouch in previousTouches)
			{
				if (!currentTouchIds.Contains(previousTouch.Id))
				{
					tempTouches.Add(previousTouch);
				}
			}
			foreach (GestureRecognizer gesture in gestures)
			{
				bool stale = false;
				foreach (GestureTouch tracked in gesture.CurrentTrackedTouches)
				{
					if (!currentTouchIds.Contains(tracked.Id))
					{
						tempTouches.Add(tracked);
						stale = true;
					}
				}
				if (stale)
				{
					gesture.Reset();
				}
			}
			foreach (GestureTouch temp in tempTouches)
			{
				previousTouchPositions.Remove(temp.Id);
				previousTouches.Remove(temp);
			}
			tempTouches.Clear();
		}

		/// <summary>
		/// Resolve the object column under a began touch, Fingers'
		/// PopulateGameObjectsForTouch: EventSystem raycasters first, then
		/// every 2D collider under the point (deduped by GameObject), a
		/// blank row when nothing was hit, then the capture walk builds
		/// the pointer's accepted list. Resolved ONLY at touch begin - the
		/// ended table offload below balances the cache.
		/// </summary>
		private void PopulateGameObjectsForTouch(int pointerId, float x, float y)
		{
			if (EventSystem.current == null)
			{
				return;
			}
			if (!gameObjectsForTouch.TryGetValue(pointerId, out List<GameObject> accepted))
			{
				accepted = new List<GameObject>();
				gameObjectsForTouch[pointerId] = accepted;
			}
			accepted.Clear();
			captureRaycastResults.Clear();
			PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
			pointerEventData.Reset();
			pointerEventData.position = new Vector2(x, y);
			pointerEventData.clickCount = 1;
			EventSystem.current.RaycastAll(pointerEventData, captureRaycastResults);
			int overlapCount = Physics2D.OverlapPointNonAlloc(pointerEventData.position, overlapResults);
			RaycastResult from2D = default(RaycastResult);
			for (int i = 0; i < overlapCount; i++)
			{
				if (overlapResults[i] == null)
				{
					continue;
				}
				Collider2D collider2D = overlapResults[i];
				GameObject gameObject = collider2D.gameObject;
				bool duplicate = false;
				foreach (RaycastResult existing in captureRaycastResults)
				{
					if (existing.gameObject == gameObject)
					{
						duplicate = true;
						break;
					}
				}
				if (!duplicate)
				{
					from2D.gameObject = gameObject;
					captureRaycastResults.Add(from2D);
				}
			}
			Array.Clear(overlapResults, 0, overlapCount);
			if (captureRaycastResults.Count == 0)
			{
				captureRaycastResults.Add(default(RaycastResult));
			}
			bool passThroughSeen = false;
			foreach (RaycastResult captureRaycastResult in captureRaycastResults)
			{
				GameObject current = captureRaycastResult.gameObject;
				bool platformSpecificViewHit;
				switch (ShouldCaptureGesture(current, out platformSpecificViewHit))
				{
				case CaptureResult.ForcePassThrough:
					passThroughSeen = true;
					break;
				case CaptureResult.ForceDenyPassThrough:
					if (!passThroughSeen)
					{
						accepted.Add(current);
						return;
					}
					break;
				case CaptureResult.Default:
					if (passThroughSeen)
					{
						// After a pass-through, only a raycast object that
						// is somebody's PlatformSpecificView still
						// collects - the mechanism behind "restricted
						// gestures still work above UI".
						foreach (GestureRecognizer gesture in gestures)
						{
							if (gesture.PlatformSpecificView == current)
							{
								platformSpecificViewHit = true;
								accepted.Add(current);
								break;
							}
						}
					}
					else
					{
						accepted.Add(current);
					}
					break;
				}
				if (platformSpecificViewHit)
				{
					break;
				}
			}
		}

		private CaptureResult ShouldCaptureGesture(GameObject obj, out bool platformSpecificView)
		{
			platformSpecificView = false;
			if (obj == null)
			{
				return CaptureResult.Default;
			}
			if (PassThroughObjects.Contains(obj))
			{
				return CaptureResult.ForcePassThrough;
			}
			foreach (GestureRecognizer gesture in gestures)
			{
				if (gesture.PlatformSpecificView == obj)
				{
					platformSpecificView = true;
					return CaptureResult.Default;
				}
			}
			obj.GetComponents(components);
			try
			{
				foreach (Component component in components)
				{
					if (component != null)
					{
						Type type = component.GetType();
						if (componentTypesToDenyPassThrough.Contains(type))
						{
							return CaptureResult.ForceDenyPassThrough;
						}
						if (componentTypesToIgnorePassThrough.Contains(type))
						{
							return CaptureResult.Ignore;
						}
					}
				}
			}
			finally
			{
				components.Clear();
			}
			return CaptureResult.Default;
		}

		private bool GameObjectMatchesPlatformSpecificView(List<GameObject> list, GestureRecognizer recognizer)
		{
			GameObject view = recognizer.PlatformSpecificView;
			if (!view && EventSystem.current == null)
			{
				return true;
			}
			if (view && view.GetComponent<Canvas>() != null)
			{
				// Fingers let a canvas_root view accept touches anywhere;
				// the gate only meant something for collider-backed views.
				return true;
			}
			if (list.Count == 0)
			{
				return view == null;
			}
			foreach (GameObject item in list)
			{
				if (item == view)
				{
					return true;
				}
				if (item != null && (item.GetComponent<Collider2D>() != null || item.GetComponent<Collider>() != null) && view == null)
				{
					return true;
				}
			}
			return false;
		}

		private ICollection<GestureTouch> FilterTouchesBegan(List<GestureTouch> touches, GestureRecognizer gesture)
		{
			filteredTouches.Clear();
			foreach (GestureTouch touch in touches)
			{
				if (gameObjectsForTouch.TryGetValue(touch.Id, out List<GameObject> accepted) && !GameObjectMatchesPlatformSpecificView(accepted, gesture))
				{
					continue;
				}
				filteredTouches.Add(touch);
			}
			return filteredTouches;
		}

		private enum CaptureResult
		{
			ForcePassThrough,

			ForceDenyPassThrough,

			Default,

			Ignore
		}

		public enum GestureLevelUnloadOption
		{
			Nothing,

			ResetGestureState,

			ClearAllGestures
		}
	}
}
