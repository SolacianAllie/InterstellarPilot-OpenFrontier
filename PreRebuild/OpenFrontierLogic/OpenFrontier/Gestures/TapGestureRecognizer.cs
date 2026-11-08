using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenFrontier.Gestures
{
	/// <summary>
	/// Tap recognizer, ported from Fingers TapGestureRecognizer with its
	/// defaults and flow intact: a press-release pair counts while the
	/// whole gesture stays under ThresholdSeconds (0.4) and every tracked
	/// touch stays within ThresholdUnits (0.3 inches) of where it began.
	/// Both HudTapInputController and CinematicScreen create it with
	/// MaximumNumberOfTouchesToTrack = 1 and act on the Ended state.
	///
	/// NumberOfTapsRequired &gt; 1 (double tap) is ported via the sequential
	/// tap threshold: the recognizer reports to the pump (OnFrameTick) so
	/// it can fail itself when the next tap does not arrive in time; no
	/// Fingers coroutine is involved.
	/// </summary>
	public class TapGestureRecognizer : GestureRecognizer
	{
		private int tapCount;

		private double tapStartTime;

		private bool timerRunning;

		private readonly List<GestureTouch> tapTouches = new List<GestureTouch>();

		public int NumberOfTapsRequired { get; set; }

		public float ThresholdSeconds { get; set; }

		public float SequentialTapThresholdSeconds { get; set; }

		public float ThresholdUnits { get; set; }

		public bool SendBeginState { get; set; }

		public ReadOnlyCollection<GestureTouch> TapTouches { get; private set; }

		public TapGestureRecognizer()
		{
			NumberOfTapsRequired = 1;
			ThresholdSeconds = 0.4f;
			SequentialTapThresholdSeconds = 0.4f;
			ThresholdUnits = 0.3f;
			TapTouches = tapTouches.AsReadOnly();
		}

		/// <summary>
		/// Fingers measured wasRemoved was tracked by a Stopwatch and by
		/// RunActionAfterDelay for the sequential-tap delay; both fold into
		/// realtime deadlines polled by the pump (unscaled clock, so the
		/// 0.4s windows behave the same while the game world is paused or
		/// heavily time-scaled - the stopwatch that Fingers read took no
		/// notice of Time.timeScale either).
		/// </summary>
		private double Now => UnityEngine.Time.realtimeSinceStartupAsDouble;

		protected override void TouchesBegan(ICollection<GestureTouch> touches)
		{
			foreach (GestureTouch touch in touches)
			{
				if (!IgnoreTouch(touch.Id))
				{
					SetState(GestureRecognizerState.Failed);
					return;
				}
			}
			// No resetFocus argument: Fingers relied on the base lazy
			// first-calculation so a multi-tap sequence keeps one continuous
			// StartFocus; the frame where Ended cleared the tracking state
			// guarantees a clean first calculation at the next begin.
			CalculateFocus(CurrentTrackedTouches);
			tapStartTime = Now;
			timerRunning = true;
			if (SendBeginState && TrackedTouchCountIsWithinRange)
			{
				SetState(GestureRecognizerState.Began);
			}
			else
			{
				SetState(GestureRecognizerState.Possible);
			}
			if (tapCount == 0)
			{
				TrackTouchesStartLocations();
			}
			foreach (GestureTouch touch in touches)
			{
				tapTouches.Add(touch);
			}
		}

		protected override void TouchesMoved()
		{
			CalculateFocus(CurrentTrackedTouches);
			if (timerRunning && Now - tapStartTime >= (double)ThresholdSeconds)
			{
				SetState(GestureRecognizerState.Failed);
			}
		}

		protected override void TouchesEnded()
		{
			// Fingers ignored SequentialTapThresholdSeconds for the FIRST
			// tap (its timer only ran between taps); with a single tap
			// required, the press-release pair was unbounded above by
			// ThresholdSeconds only through TouchesMoved, and here it used
			// the timer elapsed &lt;= SequentialTapThresholdSeconds check.
			// Same arithmetic, kept as were it: press-release must complete
			// within the sequential window.
			if (!timerRunning || Now - tapStartTime > (double)SequentialTapThresholdSeconds)
			{
				SetState(GestureRecognizerState.Failed);
				return;
			}
			CalculateFocus(CurrentTrackedTouches, resetFocus: false);
			if (AreTrackedTouchesWithinDistance(ThresholdUnits))
			{
				tapCount++;
				if (tapCount == NumberOfTapsRequired)
				{
					SetState(GestureRecognizerState.Ended);
					return;
				}
				tapStartTime = Now;
				timerRunning = true;
			}
			else
			{
				SetState(GestureRecognizerState.Failed);
			}
		}

		internal override void OnFrameTick()
		{
			// A partial multi-tap sequence fails once the next tap fails to
			// start within the sequential window. With the game's single
			// taps this is dormant: EndOrFailed resets the count without
			// waiting for the deadline.
			if (tapCount > 0 && timerRunning && State == GestureRecognizerState.Possible && Now - tapStartTime >= (double)SequentialTapThresholdSeconds)
			{
				SetState(GestureRecognizerState.Failed);
			}
		}

		protected override void StateChanged()
		{
			if (State == GestureRecognizerState.Failed || State == GestureRecognizerState.Ended)
			{
				tapCount = 0;
				timerRunning = false;
				tapTouches.Clear();
			}
		}
	}
}
