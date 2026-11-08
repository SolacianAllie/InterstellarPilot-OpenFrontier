using System.Collections.Generic;

namespace OpenFrontier.Gestures
{
	/// <summary>
	/// Pan (one-finger drag) recognizer, ported from Fingers
	/// PanGestureRecognizer. Defaults carry over exactly: ThresholdUnits
	/// 0.2 (inches of focus travel before Began), min/max touches 1 (the
	/// pinch pair must not hijack the orbit drag - Fingers' pan tracked a
	/// single finger even with two down, and the camera feel depends on
	/// that), per-step velocity from GestureVelocityTracker.
	///
	/// Consumers read VelocityX/VelocityY while Executing and react to
	/// Began; the speed-restart logic (pause re-arms the threshold) is the
	/// pause-restarts only piece that matters for a long camera drag and
	/// is ported rather than approximated.
	/// </summary>
	public class PanGestureRecognizer : GestureRecognizer
	{
		private bool needsDistanceThreshold;

		private float startX;

		private float startY;

		private double slowSince;

		private bool slowTimerRunning;

		public float ThresholdUnits { get; set; }

		public float SpeedUnitsToRestartThresholdUnits { get; set; }

		public float TimeToRestartThresholdUnits { get; set; }

		public PanGestureRecognizer()
		{
			ThresholdUnits = 0.2f;
			SpeedUnitsToRestartThresholdUnits = 0f;
			TimeToRestartThresholdUnits = 0f;
		}

		private double Now => UnityEngine.Time.realtimeSinceStartupAsDouble;

		private void ProcessTouches(bool resetFocus)
		{
			if (CalculateFocus(CurrentTrackedTouches, resetFocus))
			{
				slowTimerRunning = false;
				if (ThresholdUnits <= 0f)
				{
					needsDistanceThreshold = false;
					SetState(GestureRecognizerState.Began);
				}
				else
				{
					needsDistanceThreshold = true;
					SetState(GestureRecognizerState.Possible);
				}
				startX = FocusX;
				startY = FocusY;
			}
			else if (!needsDistanceThreshold && (State == GestureRecognizerState.Began || State == GestureRecognizerState.Executing))
			{
				// Fingers' SpeedUnitsToRestartThresholdUnits path: after the
				// pan slows to a crawl for long enough, a fresh threshold is
				// armed so a deliberate further drag re-begins. Default
				// speed zero keeps the if from ever running but the shape
				// is kept for parity.
				if (SpeedUnitsToRestartThresholdUnits > 0f && Distance(VelocityX, VelocityY) < SpeedUnitsToRestartThresholdUnits && slowTimerRunning && Now - slowSince >= (double)TimeToRestartThresholdUnits)
				{
					needsDistanceThreshold = true;
					startX = FocusX;
					startY = FocusY;
				}
				else
				{
					slowSince = Now;
					slowTimerRunning = true;
					SetState(GestureRecognizerState.Executing);
				}
			}
			else
			{
				if (!TrackedTouchCountIsWithinRange)
				{
					return;
				}
				if (needsDistanceThreshold)
				{
					if (Distance(FocusX - startX, FocusY - startY) >= ThresholdUnits)
					{
						needsDistanceThreshold = false;
						SetState(GestureRecognizerState.Began);
					}
					else if (State != GestureRecognizerState.Executing)
					{
						SetState(GestureRecognizerState.Possible);
					}
				}
				else
				{
					SetState(GestureRecognizerState.Possible);
				}
			}
		}

		protected override void TouchesBegan(ICollection<GestureTouch> touches)
		{
			ProcessTouches(true);
		}

		protected override void TouchesMoved()
		{
			ProcessTouches(false);
		}

		protected override void TouchesEnded()
		{
			ProcessTouches(false);
			if (State == GestureRecognizerState.Possible)
			{
				SetState(GestureRecognizerState.Failed);
			}
			else
			{
				SetState(GestureRecognizerState.Ended);
			}
		}
	}
}
