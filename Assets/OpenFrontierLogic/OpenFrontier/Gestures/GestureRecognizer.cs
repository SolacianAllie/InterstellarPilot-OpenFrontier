using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OpenFrontier.Gestures
{
	/// <summary>
	/// Family of state names the recognizers move through. Subset of the
	/// Fingers GestureRecognizerState as actually used by this game's
	/// consumers: HudTapInputController, CinematicScreen, PanCamera,
	/// PinchZoomCamera and CameraSpectator test Possible/Began/Executing/
	/// Ended/Failed and nothing else. EndPending never arises here because
	/// RequireGestureRecognizerToFail is never configured.
	/// </summary>
	public enum GestureRecognizerState
	{
		Possible,

		Began,

		Executing,

		Ended,

		Failed
	}

	public enum TouchPhase
	{
		Unknown,

		Began,

		Moved,

		Stationary,

		Ended,

		Cancelled
	}

	/// <summary>
	/// One tracked pointer this frame, ported from Fingers GestureTouch
	/// field for field. Coordinates are Unity screen pixels (origin bottom
	/// left), the space every Fingers consumer's math already assumed.
	///
	/// PlatformSpecificTouch is the piece IsRightMouseButton hangs off:
	/// Fingers filled it with the mouse BUTTON index (0 left, 1 right,
	/// 2 middle) for mouse-driven touches, and with the Unity Touch struct
	/// for real fingers, so the extension's `is int` pattern distinguishes
	/// a right-mouse tap from a finger tap. int? null for fingers matches
	/// that pattern exactly.
	///
	/// Classic constructor on purpose: ilspycmd emitted a C# 12 primary
	/// constructor, which the editor's C# 9 compiler refuses.
	/// </summary>
	public struct GestureTouch : IComparable<GestureTouch>
	{
		private readonly int id;

		private readonly float previousX;

		private readonly float previousY;

		private readonly float pressure;

		private readonly float screenX;

		private readonly float screenY;

		private readonly int? platformSpecificTouch;

		private readonly TouchPhase touchPhase;

		public GestureTouch(int platformSpecificId, float screenX, float screenY, float previousX, float previousY, float pressure = 1f, int? platformSpecificTouch = null, TouchPhase touchPhase = TouchPhase.Unknown)
		{
			id = platformSpecificId;
			this.screenX = screenX;
			this.screenY = screenY;
			this.previousX = previousX;
			this.previousY = previousY;
			this.pressure = pressure;
			this.platformSpecificTouch = platformSpecificTouch;
			this.touchPhase = touchPhase;
		}

		public int Id => id;

		public float ScreenX => screenX;

		public float X => screenX;

		public float ScreenY => screenY;

		public float Y => screenY;

		public float PreviousX => previousX;

		public float PreviousY => previousY;

		public float Pressure => pressure;

		public float DeltaX => screenX - previousX;

		public float DeltaY => screenY - previousY;

		public int? PlatformSpecificTouch => platformSpecificTouch;

		public TouchPhase TouchPhase => touchPhase;

		public int CompareTo(GestureTouch other)
		{
			return id.CompareTo(other.id);
		}

		public override int GetHashCode()
		{
			return id;
		}

		public override bool Equals(object obj)
		{
			if (obj is GestureTouch gestureTouch)
			{
				return gestureTouch.id == id;
			}
			return false;
		}

		public override string ToString()
		{
			return $"Id: {Id}, X: {ScreenX} ({Y}), Y: {PreviousX} ({PreviousY}), phase: {touchPhase}";
		}
	}

	/// <summary>
	/// Time-weighted velocity over the last 8 samples, ported from Fingers
	/// GestureVelocityTracker. PanCamera and CameraSpectator drive the
	/// orbit camera with VelocityX/Y after converting through
	/// GestureUnits, so the averaging behaviour (and therefore the drag
	/// feel) must not drift: instantaneous per-frame velocity reads
	/// distinctly twitchy next to the smoothed original.
	/// </summary>
	public class GestureVelocityTracker
	{
		private struct VelocityHistory
		{
			public float VelocityX;

			public float VelocityY;

			public float Seconds;
		}

		private const int maxHistory = 8;

		private readonly Queue<VelocityHistory> history = new Queue<VelocityHistory>();

		private double lastUpdateTime;

		private float previousX;

		private float previousY;

		private bool hasPrevious;

		public float ElapsedSeconds => (float)(Time.realtimeSinceStartupAsDouble - lastUpdateTime);

		public float VelocityX { get; private set; }

		public float VelocityY { get; private set; }

		public float Speed => (float)Math.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);

		private void AddItem(float velocityX, float velocityY, float elapsed)
		{
			history.Enqueue(new VelocityHistory
			{
				VelocityX = velocityX,
				VelocityY = velocityY,
				Seconds = elapsed
			});
			if (history.Count > maxHistory)
			{
				history.Dequeue();
			}
			float total = 0f;
			VelocityX = 0f;
			VelocityY = 0f;
			foreach (VelocityHistory item in history)
			{
				total += item.Seconds;
			}
			foreach (VelocityHistory item2 in history)
			{
				float weight = item2.Seconds / total;
				VelocityX += item2.VelocityX * weight;
				VelocityY += item2.VelocityY * weight;
			}
			lastUpdateTime = Time.realtimeSinceStartupAsDouble;
		}

		public void Reset()
		{
			lastUpdateTime = Time.realtimeSinceStartupAsDouble;
			VelocityX = 0f;
			VelocityY = 0f;
			history.Clear();
		}

		public void Restart()
		{
			Restart(float.MinValue, float.MinValue);
		}

		public void Restart(float previousX, float previousY)
		{
			this.previousX = previousX;
			this.previousY = previousY;
			hasPrevious = previousX != float.MinValue;
			Reset();
		}

		public void Update(float x, float y)
		{
			float elapsedSeconds = ElapsedSeconds;
			if (hasPrevious && elapsedSeconds > 0f)
			{
				float velocityX = (x - previousX) / elapsedSeconds;
				float velocityY = (y - previousY) / elapsedSeconds;
				AddItem(velocityX, velocityY, elapsedSeconds);
			}
			previousX = x;
			previousY = y;
			hasPrevious = true;
		}
	}

	/// <summary>
	/// Base recognizer, ported from Fingers GestureRecognizer with the
	/// mechanics this game's consumers rely on intact: touch tracking with
	/// a min/max span, screen-units distance math, focus averaging,
	/// velocity, and the same-view exclusivity rule that keeps orbit-pan
	/// and pinch-scale fighting over the same pair of fingers from
	/// double-driving the camera. The subtle load-bearing shapes, all
	/// verified in the decompile, are:
	/// - touches track ONLY while the gesture is Possible, up to the
	///   maximum; a late finger joining during Began/Executing is ignored;
	/// - a touch END anywhere fails any gesture that is not holding
	///   TrackedTouchCountIsWithinRange (so a rested tap pre-fails on other
	///   pointers' releases) - and BOTH end and fail re-arm through
	///   ResetInternal, which fires yet another Possible transition, so
	///   failed recognizers can happily re-begin (a tap can and must fire
	///   more than once per lifetime);
	/// - TouchesEnded consumers read focus/tracked state BEFORE the ended
	///   touches are untracked - the tap's FocusX/FocusY in the Ended
	///   callback is the release position;
	/// - the same-view claim persists until the end of the pump frame
	///   after Ended (Fingers' RunActionAfterDelay(0) removal), which is
	///   what makes a same-frame sibling gesture fail rather than begin.
	/// Everything Fingers exposed that no consumer touches (masks,
	/// RequireGestureRecognizerToFail, consumed touches, priority sorting,
	/// the BeginGestureRestart machinery FingersScript never calls) is
	/// left out rather than ported dead.
	/// </summary>
	public abstract class GestureRecognizer
	{
		/// <summary>
		/// Gestures currently claiming execution, HashSet so the Add on
		/// both the Began and Executing transitions is idempotent exactly
		/// as Fingers' ActiveGestures was.
		/// </summary>
		private static readonly HashSet<GestureRecognizer> activeGestures = new HashSet<GestureRecognizer>();

		private GestureRecognizerState state = GestureRecognizerState.Possible;

		private readonly List<GestureTouch> currentTrackedTouches = new List<GestureTouch>();

		private readonly ReadOnlyCollection<GestureTouch> trackedReadOnly;

		private readonly GestureVelocityTracker velocityTracker = new GestureVelocityTracker();

		private readonly List<KeyValuePair<float, float>> touchStartLocations = new List<KeyValuePair<float, float>>();

		private readonly List<int> ignoreTouchIds = new List<int>();

		private bool inReset;

		private int minimumNumberOfTouchesToTrack = 1;

		private int maximumNumberOfTouchesToTrack = 1;

		public bool Enabled = true;

		public int MinimumNumberOfTouchesToTrack
		{
			get
			{
				return minimumNumberOfTouchesToTrack;
			}
			set
			{
				minimumNumberOfTouchesToTrack = ((value < 1) ? 1 : value);
				if (minimumNumberOfTouchesToTrack > maximumNumberOfTouchesToTrack)
				{
					maximumNumberOfTouchesToTrack = minimumNumberOfTouchesToTrack;
				}
			}
		}

		public int MaximumNumberOfTouchesToTrack
		{
			get
			{
				return maximumNumberOfTouchesToTrack;
			}
			set
			{
				maximumNumberOfTouchesToTrack = ((value < 1) ? 1 : value);
				if (maximumNumberOfTouchesToTrack < minimumNumberOfTouchesToTrack)
				{
					minimumNumberOfTouchesToTrack = maximumNumberOfTouchesToTrack;
				}
			}
		}

		public bool TrackedTouchCountIsWithinRange
		{
			get
			{
				if (currentTrackedTouches.Count >= minimumNumberOfTouchesToTrack)
				{
					return currentTrackedTouches.Count <= maximumNumberOfTouchesToTrack;
				}
				return false;
			}
		}

		/// <summary>
		/// The view a gesture is limited to, in the Fingers sense: the
		/// touch must begin over an object the EventSystem raycast resolves
		/// to this GameObject for the gesture to start (see GestureScript).
		/// Null recognises anywhere.
		/// </summary>
		public GameObject PlatformSpecificView { get; set; }

		/// <summary>
		/// Fingers default; the same-view rule reads it exactly as there.
		/// </summary>
		public bool AllowSimultaneousExecutionIfPlatformSpecificViewsAreDifferent { get; set; }

		public GestureRecognizerState State => state;

		public IList<GestureTouch> CurrentTrackedTouches => trackedReadOnly;

		public float FocusX { get; protected set; }

		public float FocusY { get; protected set; }

		public float StartFocusX { get; protected set; }

		public float StartFocusY { get; protected set; }

		public float DeltaX { get; protected set; }

		public float DeltaY { get; protected set; }

		public float DistanceX { get; protected set; }

		public float DistanceY { get; protected set; }

		public float VelocityX => velocityTracker.VelocityX;

		public float VelocityY => velocityTracker.VelocityY;

		/// <summary>
		/// State transitions report here - the same delegate the Fingers
		/// consumers subscribed to. Fired by every transition, including
		/// the Possible re-arm that follows ends and fails.
		/// </summary>
		public event Action<GestureRecognizer> StateUpdated;

		public GestureRecognizer()
		{
			trackedReadOnly = currentTrackedTouches.AsReadOnly();
			AllowSimultaneousExecutionIfPlatformSpecificViewsAreDifferent = true;
			StartFocusX = float.MinValue;
			StartFocusY = float.MinValue;
		}

		/// <summary>
		/// Base event then family hook, mirroring Fingers' StateChanged
		/// contract (the decompiled overrides all called base FIRST, so
		/// subscribers always observe pre-reset state).
		/// </summary>
		private void FireStateUpdated()
		{
			StateUpdated?.Invoke(this);
			StateChanged();
		}

		protected virtual void StateChanged()
		{
		}

		internal virtual void OnFrameTick()
		{
			// Pump tick for recognizers owing a delayed decision (the
			// tap's sequential-tap window). Stands in for Fingers'
			// RunActionAfterDelay callbacks; never fires events.
		}

		/// <returns>true when the id was NOT already ignored and has now
		/// been added - Fingers' IgnoreTouch contract.</returns>
		protected bool IgnoreTouch(int id)
		{
			if (ignoreTouchIds.Contains(id))
			{
				return false;
			}
			ignoreTouchIds.Add(id);
			return true;
		}

		protected void TrackTouchesStartLocations()
		{
			foreach (GestureTouch tracked in currentTrackedTouches)
			{
				touchStartLocations.Add(new KeyValuePair<float, float>(tracked.X, tracked.Y));
			}
		}

		protected bool CalculateFocus(ICollection<GestureTouch> touches, bool resetFocus = false)
		{
			bool firstCalculation = resetFocus || StartFocusX == float.MinValue || StartFocusY == float.MinValue;
			FocusX = 0f;
			FocusY = 0f;
			foreach (GestureTouch touch in touches)
			{
				FocusX += touch.X;
				FocusY += touch.Y;
			}
			float count = touches.Count;
			if (count > 0f)
			{
				FocusX /= count;
				FocusY /= count;
			}
			if (firstCalculation)
			{
				StartFocusX = FocusX;
				StartFocusY = FocusY;
				DeltaX = 0f;
				DeltaY = 0f;
				velocityTracker.Restart();
			}
			else
			{
				// Per-step delta comes from the previous call, the running
				// distance from the gesture start - Fingers kept both in
				// one CalculateFocus and the two being distinct is
				// load-bearing for the tap distance test.
				DeltaX = FocusX - prevFocusX;
				DeltaY = FocusY - prevFocusY;
			}
			velocityTracker.Update(FocusX, FocusY);
			DistanceX = FocusX - StartFocusX;
			DistanceY = FocusY - StartFocusY;
			prevFocusX = FocusX;
			prevFocusY = FocusY;
			return firstCalculation;
		}

		private float prevFocusX;

		private float prevFocusY;

		/// <summary>
		/// Distance in screen units (inches). Consumers' thresholds live
		/// in these inches.
		/// </summary>
		public float DistanceBetweenPoints(float x1, float y1, float x2, float y2)
		{
			float dx = x2 - x1;
			float dy = y2 - y1;
			return GestureUnits.PixelsToUnits((float)Math.Sqrt(dx * dx + dy * dy));
		}

		public float Distance(float vectorX, float vectorY)
		{
			return GestureUnits.PixelsToUnits((float)Math.Sqrt(vectorX * vectorX + vectorY * vectorY));
		}

		public float Distance(float length)
		{
			return GestureUnits.PixelsToUnits(Math.Abs(length));
		}

		public bool AreTrackedTouchesWithinDistance(float thresholdUnits)
		{
			if (currentTrackedTouches.Count == 0 || touchStartLocations.Count == 0)
			{
				return false;
			}
			foreach (GestureTouch tracked in currentTrackedTouches)
			{
				bool matched = false;
				for (int i = touchStartLocations.Count - 1; i >= 0; i--)
				{
					if (DistanceBetweenPoints(tracked.X, tracked.Y, touchStartLocations[i].Key, touchStartLocations[i].Value) <= thresholdUnits)
					{
						matched = true;
						break;
					}
				}
				if (!matched)
				{
					return false;
				}
			}
			return true;
		}

		internal static void EndOfFrameActiveMaintenance()
		{
			// Releases the one-pump-frame exclusivity claim of gestures
			// that reported Ended this frame (Fingers' delayed
			// RemoveFromActiveGestures), before the next frame's stream
			// starts. Gestures that re-began clear the flag at their
			// transition, so an active Began/Executing never loses its
			// claim here.
			maintenanceBuffer.Clear();
			foreach (GestureRecognizer gesture in activeGestures)
			{
				if (gesture.pendingActiveRemoval)
				{
					maintenanceBuffer.Add(gesture);
				}
			}
			foreach (GestureRecognizer gesture in maintenanceBuffer)
			{
				gesture.pendingActiveRemoval = false;
				activeGestures.Remove(gesture);
			}
		}

		private static readonly List<GestureRecognizer> maintenanceBuffer = new List<GestureRecognizer>();

		private bool pendingActiveRemoval;

		/// <summary>
		/// The Fingers same-view exclusivity rule: a gesture trying to
		/// BEGIN while another gesture is already executing (on the same
		/// view, or anywhere if the multi-view allowance is off) fails
		/// itself; the first gesture to reach Began claims the stream.
		/// </summary>
		private bool CanExecute(GestureRecognizerState value)
		{
			if (activeGestures.Count == 0 || (value != GestureRecognizerState.Began && value != GestureRecognizerState.Executing && value != GestureRecognizerState.Ended))
			{
				return true;
			}
			if (state == GestureRecognizerState.Began || state == GestureRecognizerState.Executing)
			{
				return true;
			}
			foreach (GestureRecognizer active in activeGestures)
			{
				if (active == this)
				{
					continue;
				}
				bool sameView = active.PlatformSpecificView == PlatformSpecificView;
				if (AllowSimultaneousExecutionIfPlatformSpecificViewsAreDifferent && !sameView)
				{
					continue;
				}
				return false;
			}
			return true;
		}

		private void FailGestureNow()
		{
			state = GestureRecognizerState.Failed;
			activeGestures.Remove(this);
			FireStateUpdated();
			// Fingers' FailGestureNow tails into ResetInternal(ClearTrac
			// kedTouchesOnEndOrFail=false): tracked touches STAY (the
			// caller's StopTrackingTouches finally cleans the delivered
			// ids), and the Possible re-arm below fires once more.
			ResetInternal(false);
		}

		private void ResetInternal(bool clearCurrentTrackedTouches)
		{
			if (inReset)
			{
				return;
			}
			inReset = true;
			try
			{
				if (clearCurrentTrackedTouches)
				{
					currentTrackedTouches.Clear();
				}
				touchStartLocations.Clear();
				StartFocusX = float.MinValue;
				StartFocusY = float.MinValue;
				prevFocusX = float.MinValue;
				prevFocusY = float.MinValue;
				DistanceX = 0f;
				DistanceY = 0f;
				DeltaX = 0f;
				DeltaY = 0f;
				FocusX = 0f;
				FocusY = 0f;
				velocityTracker.Reset();
				activeGestures.Remove(this);
				SetState(GestureRecognizerState.Possible);
			}
			finally
			{
				inReset = false;
			}
		}

		public virtual void Reset()
		{
			ResetInternal(true);
		}

		public bool HasTouch(int id)
		{
			foreach (GestureTouch tracked in currentTrackedTouches)
			{
				if (tracked.Id == id)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Frame entry points owned by GestureScript - the dispatch shapes
		/// follow Fingers' ProcessTouches{Began,Moved,Ended} exactly, order-
		/// sensitive notes in each method.
		/// </summary>
		public void ProcessTouchesBegan(ICollection<GestureTouch> touches)
		{
			if (!Enabled || touches == null || touches.Count == 0 || (state != GestureRecognizerState.Possible && state != GestureRecognizerState.Began && state != GestureRecognizerState.Executing))
			{
				return;
			}
			int added = TrackTouches(touches);
			if (added > 0)
			{
				// Fingers' overflow check (max only lowerable mid-life, so
				// unreachable through TrackTouches' own cap) kept for
				// parity.
				if (currentTrackedTouches.Count > MaximumNumberOfTouchesToTrack)
				{
					SetState(GestureRecognizerState.Failed);
				}
				else
				{
					TouchesBegan(touches);
				}
			}
		}

		private int TrackTouches(ICollection<GestureTouch> touches)
		{
			int num = 0;
			foreach (GestureTouch touch in touches)
			{
				// Fingers tracks ONLY in Possible: a finger joining a
				// gesture mid-flight does not extend its tracking.
				if (state == GestureRecognizerState.Possible && currentTrackedTouches.Count < MaximumNumberOfTouchesToTrack && !currentTrackedTouches.Contains(touch))
				{
					currentTrackedTouches.Add(touch);
					num++;
				}
			}
			if (currentTrackedTouches.Count > 1)
			{
				currentTrackedTouches.Sort();
			}
			return num;
		}

		public void ProcessTouchesMoved(ICollection<GestureTouch> touches)
		{
			if (!Enabled || touches == null || touches.Count == 0)
			{
				return;
			}
			// Intersect first: a moved event for foreign pointers must not
			// disturb a gesture that fell out of its valid state - only a
			// gesture actually being touched can fail here.
			if (!TouchesIntersect(touches, currentTrackedTouches))
			{
				return;
			}
			if (currentTrackedTouches.Count > MaximumNumberOfTouchesToTrack || (state != GestureRecognizerState.Possible && state != GestureRecognizerState.Began && state != GestureRecognizerState.Executing))
			{
				SetState(GestureRecognizerState.Failed);
				return;
			}
			UpdateTrackedTouches(touches);
			TouchesMoved();
		}

		public void ProcessTouchesEnded(ICollection<GestureTouch> touches)
		{
			if (!Enabled || touches == null || touches.Count == 0)
			{
				return;
			}
			try
			{
				// Delivered end ids leave the ignore list first - a touch
				// must be able to begin a fresh session next time.
				foreach (GestureTouch touch in touches)
				{
					ignoreTouchIds.Remove(touch.Id);
				}
				if (!TrackedTouchCountIsWithinRange || (state != GestureRecognizerState.Possible && state != GestureRecognizerState.Began && state != GestureRecognizerState.Executing))
				{
					FailGestureNow();
				}
				else if (TouchesIntersect(touches, currentTrackedTouches))
				{
					UpdateTrackedTouches(touches);
					TouchesEnded();
				}
			}
			finally
			{
				// Tracking is dropped only afterwards (Fingers' finally
				// StopTrackingTouches) - the tap consumer reads
				// FocusX/FocusY from touches still tracked.
				StopTrackingTouches(touches);
			}
		}

		private bool TouchesIntersect(ICollection<GestureTouch> touches, List<GestureTouch> tracked)
		{
			foreach (GestureTouch touch in touches)
			{
				for (int i = 0; i < tracked.Count; i++)
				{
					if (tracked[i].Id == touch.Id)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void UpdateTrackedTouches(ICollection<GestureTouch> touches)
		{
			int num = 0;
			foreach (GestureTouch touch in touches)
			{
				for (int i = 0; i < currentTrackedTouches.Count; i++)
				{
					if (currentTrackedTouches[i].Id == touch.Id)
					{
						currentTrackedTouches[i] = touch;
						num++;
						break;
					}
				}
			}
			if (num != 0)
			{
				currentTrackedTouches.Sort();
			}
		}

		private void StopTrackingTouches(ICollection<GestureTouch> touches)
		{
			foreach (GestureTouch touch in touches)
			{
				for (int i = currentTrackedTouches.Count - 1; i >= 0; i--)
				{
					if (currentTrackedTouches[i].Id == touch.Id)
					{
						currentTrackedTouches.RemoveAt(i);
						break;
					}
				}
			}
		}

		protected void SetState(GestureRecognizerState value)
		{
			if (value == GestureRecognizerState.Failed)
			{
				FailGestureNow();
				return;
			}
			if (!CanExecute(value))
			{
				FailGestureNow();
				return;
			}
			switch (value)
			{
			case GestureRecognizerState.Began:
			case GestureRecognizerState.Executing:
				state = value;
				// Re-begun after a same-frame end: the ended claim must not
				// lag behind and strip this fresh one at pump maintenance.
				pendingActiveRemoval = false;
				activeGestures.Add(this);
				FireStateUpdated();
				break;
			case GestureRecognizerState.Ended:
				// Fingers SetState(Ended): EndGesture Fires the Ended
				// event, re-arms Possible through ResetInternal and leaves
				// the active set; the Ended case then re-claims the
				// exclusivity until the pump frame ends (the delayed
				// RemoveFromActiveGestures there).
				EndGesture();
				activeGestures.Add(this);
				pendingActiveRemoval = true;
				break;
			default:
				state = value;
				FireStateUpdated();
				break;
			}
		}

		private void EndGesture()
		{
			state = GestureRecognizerState.Ended;
			FireStateUpdated();
			// ResetOnEnd is true family-wide (virtual default in Fingers,
			// no override anywhere) - ends always re-arm.
			ResetInternal(false);
		}

		protected abstract void TouchesBegan(ICollection<GestureTouch> touches);

		protected abstract void TouchesMoved();

		protected abstract void TouchesEnded();
	}
}
