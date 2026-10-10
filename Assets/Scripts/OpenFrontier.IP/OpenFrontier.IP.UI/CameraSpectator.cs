using System.Collections.Generic;
using DigitalRubyShared;
using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.EventSystems;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI
{
	public class CameraSpectator : MonoBehaviour
	{
		public enum SpectateState
		{
			None,
			Viewpoint,
			Orbit,
			Flyby,
			ManualOrbit
		}

		public float OrbitDistanceMultiplier = 1f;

		public float OrbitPanGestureMinRotationSpeed = 1f;

		public float OrbitPanGestureMaxRotationSpeed = 2f;

		public float OrbitPanGestureMultiplier = 10f;

		public float OrbitScaleGestureRate = 4f;

		public float OrbitScaleMouseWheelRate = 4f;

		[Tooltip("The threshold in units before zooming begins to happen. Start distance must change this much in order to start the gesture.")]
		[Range(0f, 3f)]
		public float OrbitScaleGestureZoomThresholdUnits = 0.15f;

		public GameObject OrbitScaleGesturePlatformSpecificView;

		// Auto-orbit pitch band: spawns AND drift stay inside it. The
		// drift re-rolls used to wander far outside the spawn range over
		// a long watch (slowly going top-down or bottom-up).
		public float MinOrbitAngleX = -30f;

		public float MaxOrbitAngleX = 30f;

		public float MaxManualOrbitAngleX = 75f;

		public float OrbitRotationRateMultiplierX = 0.4f;

		public float OrbitStationDistanceMultiplier = 2f;

		public bool AllowFlyby = true;

		public bool AllowOrbit = true;

		public bool AllowViewport = true;

		private bool alternateState = true;

		public bool UseRealTime = true;

		private List<SpectateState> availableStates = new List<SpectateState>();

		private Vector3 cameraAngle = Vector3.zero;

		private Vector3 currentRotRate = Vector3.zero;

		// The re-rolled drift rate the current one eases toward - the
		// state timer re-rolls every few seconds and assigning the new
		// rate instantly made the orbit suddenly reverse direction.
		private Vector3 targetRotRate = Vector3.zero;

		private SpectateState currentState;

		private int currentViewportIndex;

		public float FlyByMaxDist = 300f;

		public float FlyByMinDist;

		public float FlyByMinSpdAllowed = 4f;

		public float FlyByRadiusMultiplier = 1f;

		public float FlyBySpdMultiplier = 3f;

		public float MaxFlyByAngle = 20f;

		[SerializeField]
		private float minOrbitDistance = 40f;

		private double nextStateChangeTime;

		public float OrbitRadiusDistanceFudge = 2f;

		[SerializeField]
		private float minOrbitRadiusMultiplier = 1.5f;

		[SerializeField]
		private float maxOrbitRadiusMultiplier = 2.4f;

		[SerializeField]
		private float minManualOrbitRadiusMultiplier = 1f;

		[SerializeField]
		private float maxManualOrbitRadiusMultiplier = 3f;

		[SerializeField]
		public float OrbitSpeed = 4f;

		public float StateDuration = 5f;

		[SerializeField]
		private GameObject target;

		private Unit targetUnit;

		private Unit lastTargetRootUnit;

		private double lastStateChangeTime;

		private Vector3 lastTargetPosition = Vector3.zero;

		public float CurrentOrbitDistance;

		public ScaleGestureRecognizer OrbitScaleGesture { get; private set; }

		public PanGestureRecognizer OrbitPanGesture { get; private set; }

		public double LastStateChangeTime => lastStateChangeTime;

		public SpectateState CurrentState
		{
			get
			{
				return currentState;
			}
			private set
			{
				SpectateState previousState = currentState;
				if (currentState != value)
				{
					lastStateChangeTime = CurrentGameTime();
				}
				currentState = value;
				if (target != null)
				{
					switch (currentState)
					{
					case SpectateState.Flyby:
					{
						Vector3 euler = new Vector3((0f - MaxFlyByAngle) / 2f + Random.value * MaxFlyByAngle, (0f - MaxFlyByAngle) / 2f + Random.value * MaxFlyByAngle, 0f);
						float num = Mathf.Clamp(TargetSpeed * FlyBySpdMultiplier, FlyByMinDist, FlyByMaxDist);
						Vector3 vector = Quaternion.Euler(GetOrbitCameraAngle()) * Vector3.forward * FlyByRadiusMultiplier * TargetRadius;
						transform.position = target.transform.TransformPoint(vector + Quaternion.Euler(euler) * Vector3.forward * num);
						break;
					}
					case SpectateState.Orbit:
						// Orbit->Orbit re-entry must NOT re-frame: the
						// state timer re-rolls every few seconds, and
						// re-randomizing angle/distance on the SAME ship
						// was a visible perspective snap. Only frame up
						// when arriving from a different state.
						if (previousState != SpectateState.Orbit)
						{
							cameraAngle = GetOrbitCameraAngle();
							SetRandomOrbitDistance();
						}
						targetRotRate = new Vector3(0f - OrbitSpeed + Random.value * OrbitSpeed * 2f * OrbitRotationRateMultiplierX, 0f - OrbitSpeed + Random.value * OrbitSpeed * 2f, 0f);
						if (previousState != SpectateState.Orbit)
						{
							currentRotRate = targetRotRate;
						}
						break;
					}
				}
				ApplyState();
			}
		}

		public float TargetSpeed => Vector3.Distance(lastTargetPosition, target.transform.position);

		public float TargetRadius
		{
			get
			{
				if (targetUnit != null)
				{
					return targetUnit.GetRootUnit().UnitClass.ShieldRingRadius;
				}
				return 0f;
			}
		}

		public bool AlternateViewState
		{
			get
			{
				return alternateState;
			}
			set
			{
				alternateState = value;
			}
		}

		public int CurrentViewportIndex
		{
			get
			{
				return currentViewportIndex;
			}
			set
			{
				currentViewportIndex = value;
			}
		}

		public float RotRate
		{
			get
			{
				return OrbitSpeed;
			}
			set
			{
				OrbitSpeed = value;
			}
		}

		public Unit TargetRootUnit
		{
			get
			{
				if (targetUnit != null)
				{
					return targetUnit.GetRootUnit();
				}
				return null;
			}
		}

		public GameObject Target
		{
			get
			{
				return target;
			}
			set
			{
				if (target != value)
				{
					target = value;
					if (target != null)
					{
						targetUnit = target.GetComponent<Unit>();
						ChooseState();
						ApplyState();
						CameraMoved();
						UpdateLastTargetPosition();
					}
					else
					{
						targetUnit = null;
						CurrentState = SpectateState.None;
					}
				}
			}
		}

		public float MinOrbitDistance
		{
			get
			{
				return minOrbitDistance;
			}
			set
			{
				minOrbitDistance = value;
			}
		}

		public float MaxOrbitRadiusMultiplier
		{
			get
			{
				return maxOrbitRadiusMultiplier;
			}
			set
			{
				maxOrbitRadiusMultiplier = value;
			}
		}

		public float MinOrbitRadiusMultiplier
		{
			get
			{
				return minOrbitRadiusMultiplier;
			}
			set
			{
				minOrbitRadiusMultiplier = value;
			}
		}

		public EngineASX Engine => EngineASX.Instance;

		public void SetRandomOrbitDistance()
		{
			OrbitDistanceMultiplier = Random.Range(minOrbitRadiusMultiplier, maxOrbitRadiusMultiplier);
			SetOrbitDistance();
		}

		private void SetOrbitDistance()
		{
			CurrentOrbitDistance = CalculateOrbitDistance();
		}

		private double CurrentGameTime()
		{
			if (!UseRealTime)
			{
				return Time.timeAsDouble;
			}
			return Time.realtimeSinceStartupAsDouble;
		}

		private void CameraMoved()
		{
			Engine.OnCameraMoved();
		}

		private void OnEnable()
		{
			OrbitScaleGesture = new ScaleGestureRecognizer();
			OrbitScaleGesture.StateUpdated += ScaleGesture_Updated;
			OrbitScaleGesture.ThresholdUnits = OrbitScaleGestureZoomThresholdUnits;
			OrbitScaleGesture.PlatformSpecificView = OrbitScaleGesturePlatformSpecificView;
			FingersScript.Instance.AddGesture(OrbitScaleGesture);
			OrbitPanGesture = new PanGestureRecognizer();
			OrbitPanGesture.StateUpdated += PanGestureUpdated;
			OrbitPanGesture.PlatformSpecificView = OrbitScaleGesturePlatformSpecificView;
			FingersScript.Instance.AddGesture(OrbitPanGesture);
		}

		private void OnDisable()
		{
			if (FingersScript.HasInstance)
			{
				FingersScript.Instance.RemoveGesture(OrbitPanGesture);
				FingersScript.Instance.RemoveGesture(OrbitScaleGesture);
			}
		}

		private void PanGestureUpdated(GestureRecognizer r)
		{
			if (r.State == GestureRecognizerState.Began)
			{
				CurrentState = SpectateState.ManualOrbit;
			}
			if (r.State == GestureRecognizerState.Executing)
			{
				float num = Mathf.Lerp(OrbitPanGestureMinRotationSpeed, OrbitPanGestureMaxRotationSpeed, GameController.Instance.CameraDragRotateSensitivity) * OrbitPanGestureMultiplier;
				num *= GameController.Instance.GetCameraDragRotateSensitivityMultiplier();
				float velocityX = r.VelocityX;
				float velocityY = r.VelocityY;
				velocityX *= RealTime.deltaTime;
				velocityY *= RealTime.deltaTime;
				cameraAngle.y += DeviceInfo.PixelsToUnits(velocityX) * num;
				cameraAngle.x = Mathf.Clamp(cameraAngle.x + DeviceInfo.PixelsToUnits(velocityY) * num, 0f - MaxManualOrbitAngleX, MaxManualOrbitAngleX);
			}
		}

		private void ScaleGesture_Updated(GestureRecognizer gesture)
		{
			if (gesture.State == GestureRecognizerState.Began)
			{
				CurrentState = SpectateState.ManualOrbit;
			}
			if (gesture.State == GestureRecognizerState.Executing)
			{
				OrbitDistanceMultiplier = Mathf.Clamp(OrbitDistanceMultiplier - OrbitScaleGesture.ScaleDistanceDeltaY * OrbitScaleGestureRate, minManualOrbitRadiusMultiplier, maxManualOrbitRadiusMultiplier);
				SetOrbitDistance();
			}
		}

		private void Update()
		{
			if (!(target != null))
			{
				return;
			}
			if (currentState != SpectateState.ManualOrbit)
			{
				if (CurrentGameTime() > nextStateChangeTime)
				{
					ChooseState();
				}
				else if (targetUnit != null && TargetRootUnit != lastTargetRootUnit)
				{
					SetRandomOrbitDistance();
				}
			}
			else
			{
				MouseWheelScale();
				if (targetUnit != null && TargetRootUnit != lastTargetRootUnit)
				{
					SetOrbitDistance();
				}
			}
			ApplyState();
			UpdateLastTargetPosition();
			lastTargetRootUnit = TargetRootUnit;
		}

		private void MouseWheelScale()
		{
			if (Input.mouseScrollDelta.y != 0f && (!(EventSystem.current != null) || !EventSystem.current.IsPointerOverGameObject()))
			{
				OrbitDistanceMultiplier = Mathf.Clamp(OrbitDistanceMultiplier - Input.mouseScrollDelta.y * OrbitScaleMouseWheelRate, minManualOrbitRadiusMultiplier, maxManualOrbitRadiusMultiplier);
				SetOrbitDistance();
			}
		}

		private void UpdateLastTargetPosition()
		{
			lastTargetPosition = target.transform.position;
		}

		private void ApplyState()
		{
			switch (currentState)
			{
			case SpectateState.ManualOrbit:
				ApplyOrbit();
				break;
			case SpectateState.Orbit:
				ApplyOrbit();
				// Ease into re-rolled drift rates (~2s settle) instead
				// of snapping the orbit's direction/speed instantly.
				currentRotRate = Vector3.Lerp(currentRotRate, targetRotRate, 1f - Mathf.Exp(-1.5f * (float)GameController.Instance.RealDeltaTime));
				cameraAngle += currentRotRate * (float)GameController.Instance.RealDeltaTime;
				cameraAngle.x = Mathf.Clamp(cameraAngle.x, MinOrbitAngleX, MaxOrbitAngleX);
				break;
			case SpectateState.Flyby:
				if (TargetSpeed == 0f)
				{
					CurrentState = SpectateState.Orbit;
					CameraMoved();
				}
				else
				{
					gameObject.transform.LookAt(target.transform);
				}
				break;
			}
		}

		private void ApplyOrbit()
		{
			Quaternion quaternion = Quaternion.Euler(cameraAngle);
			Vector3 vector = Vector3.forward * CurrentOrbitDistance;
			transform.position = GetTargetRootPosition() + quaternion * vector;
			gameObject.transform.LookAt(GetTargetRootPosition());
		}

		private Vector3 GetTargetRootPosition()
		{
			// Every step is checked, because a unit can outlive the GameObject it was
			// destroyed with for a frame or two around a scene change, and
			// Unity's own null is the only check that catches an object
			// already torn down. Staying where it is for a frame is
			// invisible; throwing every frame while the world is being
			// swapped out is not.
			if (TargetRootUnit != null)
			{
				Transform rootTransform = TargetRootUnit.transform;

				if (rootTransform != null)
				{
					return rootTransform.position;
				}
			}

			if (target != null && target.transform != null)
			{
				return target.transform.position;
			}

			return transform.position;
		}

		private float CalculateOrbitDistance()
		{
			float num = OrbitRadiusDistanceFudge + TargetRadius * OrbitDistanceMultiplier;
			if (targetUnit != null)
			{
				Unit rootUnit = targetUnit.GetRootUnit();
				if (rootUnit.UnitType == UnitType.Station)
				{
					num *= OrbitStationDistanceMultiplier;
				}
				if (rootUnit.UnitClass.DisplayData != null)
				{
					num *= rootUnit.UnitClass.DisplayData.CameraOrbitDistanceMultipler;
				}
			}
			return Mathf.Max(minOrbitDistance, num);
		}

		public void ChooseState()
		{
			PopulateAvailableStates();
			if (availableStates.Count > 0)
			{
				CurrentState = availableStates[Random.Range(0, availableStates.Count)];
			}
			SetNextStateChangeTime();
		}

		private void SetNextStateChangeTime()
		{
			nextStateChangeTime = CurrentGameTime() + (double)StateDuration;
		}

		private Vector3 GetOrbitCameraAngle()
		{
			return new Vector3(MinOrbitAngleX + Random.value * (MaxOrbitAngleX - MinOrbitAngleX), Random.value * 360f, 0f);
		}

		private void PopulateAvailableStates()
		{
			availableStates.Clear();
			if (target != null)
			{
				if (AllowFlyby && TargetSpeed > FlyByMinSpdAllowed)
				{
					availableStates.Add(SpectateState.Flyby);
				}
				if (AllowViewport && targetUnit != null)
				{
					availableStates.Add(SpectateState.Viewpoint);
				}
				if (AllowOrbit)
				{
					availableStates.Add(SpectateState.Orbit);
				}
			}
		}
	}
}
