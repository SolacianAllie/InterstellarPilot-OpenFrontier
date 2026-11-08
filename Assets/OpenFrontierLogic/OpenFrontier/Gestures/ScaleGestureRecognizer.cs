using System;
using System.Collections.Generic;

namespace OpenFrontier.Gestures
{
	/// <summary>
	/// Two-finger pinch/scale recognizer, ported from Fingers
	/// ScaleGestureRecognizer. The pinch-zoom camera controls read
	/// ScaleDistanceDeltaY - the per-step change in the vertical
	/// separation between the two fingers, in screen inches - and the
	/// threshold sequence (Began, then Executing per update) is what the
	/// camera consumes; the hysteresis constants against wobble and
	/// direction flips are ported verbatim because they are what keeps a
	/// resting second finger from pulsing the zoom.
	///
	/// ZoomSpeed defaults to 3 with a mouse present 1 without, matching
	/// Fingers' ctor read of FingersScript.MousePresent. PinchZoomCamera
	/// overwrote it with its serialized value; CameraSpectator did not and
	/// leans on this default, so the desktop vs phone split must stay.
	/// </summary>
	public class ScaleGestureRecognizer : GestureRecognizer
	{
		private const float minimumScaleResolutionSquared = 1.005f;

		private const float stationaryScaleResolutionSquared = 1.05f;

		private const float stationaryTimeSeconds = 0.1f;

		private const float hysteresisScaleResolutionSquared = 1.15f;

		private const int resetDirectionMilliseconds = 300;

		private const float minimumScaleDownPerUpdate = 0.25f;

		private const float maximumScaleUpPerUpdate = 4f;

		// Realtime clock for the stationary timers, standing in for the
		// Stopwatch the decompiled source kept running.
		private double timerStart = UnityEngine.Time.realtimeSinceStartupAsDouble;

		private float distance;

		private float distanceX;

		private float distanceY;

		private float previousDistanceDirection;

		private float previousDistance;

		private float previousDistanceX;

		private float previousDistanceY;

		public float ScaleMultiplier { get; private set; }

		public float ScaleMultiplierX { get; private set; }

		public float ScaleMultiplierY { get; private set; }

		public float ScaleDistance => distance;

		public float ScaleDistanceX => distanceX;

		public float ScaleDistanceY => distanceY;

		public float PreviousScaleDistance => previousDistance;

		public float PreviousScaleDistanceX => previousDistanceX;

		public float PreviousScaleDistanceY => previousDistanceY;

		public float ScaleDistanceDelta => distance - previousDistance;

		public float ScaleDistanceDeltaX => distanceX - previousDistanceX;

		public float ScaleDistanceDeltaY => distanceY - previousDistanceY;

		public float ZoomSpeed { get; set; }

		public float ScaleMultiplierRange
		{
			get
			{
				if (ScaleMultiplier > 1f)
				{
					return ScaleMultiplier * ZoomSpeed;
				}
				if (ScaleMultiplier < 1f)
				{
					return -1f / ScaleMultiplier * ZoomSpeed;
				}
				return 0f;
			}
		}

		public float ThresholdUnits { get; set; }

		public ScaleGestureRecognizer()
		{
			ScaleMultiplier = 1f;
			ScaleMultiplierX = 1f;
			ScaleMultiplierY = 1f;
			ZoomSpeed = (GestureScript.HasMouse ? 3f : 1f);
			ThresholdUnits = 0.15f;
			MinimumNumberOfTouchesToTrack = 2;
			MaximumNumberOfTouchesToTrack = 2;
		}

		private double Now => UnityEngine.Time.realtimeSinceStartupAsDouble;

		private void SetPreviousDistance(float distance, float distanceX, float distanceY)
		{
			previousDistance = distance;
			previousDistanceX = distanceX;
			previousDistanceY = distanceY;
		}

		private float ClampScale(float rawScale)
		{
			if (rawScale > 4f)
			{
				return 4f;
			}
			if (rawScale < 0.25f)
			{
				return 0.25f;
			}
			return rawScale;
		}

		private float GetScale(float rawScale)
		{
			rawScale = ClampScale(rawScale);
			if (ZoomSpeed != 1f)
			{
				if (rawScale < 1f)
				{
					rawScale -= (1f - rawScale) * ZoomSpeed;
				}
				else if (rawScale > 1f)
				{
					rawScale += (rawScale - 1f) * ZoomSpeed;
				}
				rawScale = ((rawScale > 4f) ? 4f : ((rawScale < 0.25f) ? 0.25f : rawScale));
			}
			return rawScale;
		}

		private void ProcessTouches()
		{
			CalculateFocus(CurrentTrackedTouches);
			if (!TrackedTouchCountIsWithinRange)
			{
				distance = 0f;
				distanceX = 0f;
				distanceY = 0f;
				previousDistance = 0f;
				previousDistanceX = 0f;
				previousDistanceY = 0f;
				return;
			}
			GestureTouch first = CurrentTrackedTouches[0];
			GestureTouch second = CurrentTrackedTouches[1];
			distance = DistanceBetweenPoints(first.X, first.Y, second.X, second.Y);
			distanceX = Distance(first.X - second.X);
			distanceY = Distance(first.Y - second.Y);
			if (State == GestureRecognizerState.Possible)
			{
				if (previousDistance == 0f)
				{
					SetPreviousDistance(distance, distanceX, distanceY);
				}
				if (Math.Abs(previousDistance - distance) >= ThresholdUnits)
				{
					SetPreviousDistance(distance, distanceX, distanceY);
					SetState(GestureRecognizerState.Began);
				}
			}
			else if (State == GestureRecognizerState.Executing)
			{
				if (distance == previousDistance)
				{
					return;
				}
				float stationaryThresholdSquared = ((Now - timerStart <= stationaryTimeSeconds) ? minimumScaleResolutionSquared : stationaryScaleResolutionSquared);
				float currentSquared = distance * distance;
				float previousSquared = previousDistance * previousDistance;
				if ((currentSquared - previousSquared) * previousDistanceDirection < 0f)
				{
					stationaryThresholdSquared = Math.Max(stationaryThresholdSquared, hysteresisScaleResolutionSquared);
				}
				if (previousSquared > stationaryThresholdSquared * currentSquared || currentSquared > stationaryThresholdSquared * previousSquared)
				{
					if (previousDistanceDirection == 0f)
					{
						SetPreviousDistance(distance, distanceX, distanceY);
					}
					timerStart = Now;
					float direction = (currentSquared - previousSquared >= 0f) ? 1f : (-1f);
					if (previousDistanceDirection == 0f || direction == previousDistanceDirection)
					{
						ScaleMultiplier = GetScale(distance / previousDistance);
						ScaleMultiplierX = GetScale(distanceX / previousDistanceX);
						ScaleMultiplierY = GetScale(distanceY / previousDistanceY);
						SetState(GestureRecognizerState.Executing);
					}
					else
					{
						ScaleMultiplier = 1f;
						ScaleMultiplierX = 1f;
						ScaleMultiplierY = 1f;
					}
					previousDistanceDirection = direction;
					SetPreviousDistance(distance, distanceX, distanceY);
				}
				else if ((Now - timerStart) * 1000.0 > (double)resetDirectionMilliseconds)
				{
					previousDistanceDirection = 0f;
				}
			}
			else if (State == GestureRecognizerState.Began)
			{
				ScaleMultiplier = 1f;
				ScaleMultiplierX = 1f;
				ScaleMultiplierY = 1f;
				previousDistanceDirection = 0f;
				SetPreviousDistance(distance, distanceX, distanceY);
				SetState(GestureRecognizerState.Executing);
			}
			else
			{
				SetState(GestureRecognizerState.Possible);
			}
		}

		protected override void TouchesBegan(ICollection<GestureTouch> touches)
		{
			previousDistance = 0f;
		}

		protected override void TouchesMoved()
		{
			ProcessTouches();
		}

		protected override void TouchesEnded()
		{
			if (State == GestureRecognizerState.Executing)
			{
				CalculateFocus(CurrentTrackedTouches);
				SetState(GestureRecognizerState.Ended);
			}
			else
			{
				SetState(GestureRecognizerState.Failed);
			}
		}

		public override void Reset()
		{
			base.Reset();
			distance = 0f;
			distanceX = 0f;
			distanceY = 0f;
			previousDistance = 0f;
			previousDistanceX = 0f;
			previousDistanceY = 0f;
			previousDistanceDirection = 0f;
		}
	}
}
