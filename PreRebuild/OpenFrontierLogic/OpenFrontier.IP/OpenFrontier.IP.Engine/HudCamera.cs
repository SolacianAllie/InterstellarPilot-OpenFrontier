using System;
using System.Collections;
using System.Collections.Generic;
using OpenFrontier.IP.UI.Engine;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.EventSystems;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class HudCamera : MonoBehaviour
	{
		public delegate void LostTargetHandler(HudCamera sender, Unit lostTarget);

		private WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

		private Vector3 camDesiredPosition = Vector3.zero;

		private Vector3 camDesiredRotation = Vector3.zero;

		[SerializeField]
		private float cameraElevation;

		[SerializeField]
		private float camUnitDistance;

		public Unit SpectateTarget;

		private EngineASX engine;

		private HudTarget? currentHudTarget;

		private Vector3 lastKnownUnitWorldPosition = Vector3.zero;

		public float LastTimeChangedTarget;

		public float CamUnitDistance
		{
			get
			{
				return camUnitDistance;
			}
			set
			{
				camUnitDistance = Mathf.Clamp01(value);
			}
		}

		public bool HasChangeTargetCooldownExpired => RealTime.time > LastTimeChangedTarget + EngineASX.Instance.GameSettings.HudCameraSettings.CooldownTime;

		public HudTarget? CurrentHudTarget => currentHudTarget;

		public bool HasTarget => currentHudTarget.HasValue;

		public float CameraElevation
		{
			get
			{
				return cameraElevation;
			}
			set
			{
				cameraElevation = Mathf.Clamp01(value);
			}
		}

		public Vector3 DesiredRotation
		{
			get
			{
				return camDesiredRotation;
			}
			set
			{
				camDesiredRotation = value;
			}
		}

		public event LostTargetHandler LostTarget;

		public void StartTargetTimeout(float duration)
		{
			currentHudTarget = HudTarget.Position(lastKnownUnitWorldPosition, Time.time + duration);
		}

		public void MoveCameraToDesired(bool instant)
		{
			if (instant)
			{
				engine.MainCamera.SetWorldPosition(camDesiredPosition);
				engine.MainCamera.transform.localRotation = Quaternion.Euler(camDesiredRotation);
				engine.OnCameraMoved();
			}
			else
			{
				if (!(SpectateTarget != null))
				{
					return;
				}
				Vector3 currentVelocity = Vector3.zero;
				if (!engine.IsPaused && SpectateTarget.RBody != null)
				{
					currentVelocity = SpectateTarget.RBody.linearVelocity;
				}
				Vector3 vector = Vector3.SmoothDamp(engine.MainCamera.transform.position, camDesiredPosition, ref currentVelocity, EngineASX.Instance.GameSettings.HudCameraSettings.SmoothDampTime, float.MaxValue, GetDeltaTime());
				if (EngineASX.Instance.GameSettings.HudCameraSettings.AllowCameraThroughObjects)
				{
					engine.MainCamera.SetWorldPosition(vector);
				}
				else
				{
					Vector3 value = vector - SpectateTarget.SectorPosition;
					Vector3 vector2 = Vector3.Normalize(value);
					if (Physics.Raycast(new Ray(SpectateTarget.SectorPosition, vector2), out var hitInfo, value.magnitude, GameController.Instance.SelectableMask, QueryTriggerInteraction.Ignore))
					{
						engine.MainCamera.SetSectorPosition(SpectateTarget.SectorPosition + vector2 * hitInfo.distance);
					}
					else
					{
						engine.MainCamera.SetSectorPosition(vector);
					}
				}
				engine.MainCamera.transform.rotation = Quaternion.Slerp(engine.MainCamera.transform.localRotation, Quaternion.Euler(camDesiredRotation), EngineASX.Instance.GameSettings.HudCameraSettings.CamRotationLerpRate * GetDeltaTime());
			}
		}

		public void UpdateDesiredCameraOrientation()
		{
			if (!(SpectateTarget != null) || !(SpectateTarget.ActiveUnit != null))
			{
				return;
			}
			switch (engine.Hud.CameraMode)
			{
			case HudCameraMode.FixedForward:
				UpdateDesiredCameraOrientation_FixedForward();
				break;
			case HudCameraMode.Free:
				UpdateDesiredCameraPosition_Free(SpectateTarget.ActiveUnit);
				break;
			case HudCameraMode.LockTarget:
				if (HasTarget)
				{
					UpdateDesiredCameraOrientation_LockTarget();
				}
				else
				{
					UpdateDesiredCameraOrientation_FixedForward();
				}
				break;
			}
		}

		private void UpdateDesiredCameraOrientation_LockTarget()
		{
			float yBearing = Unit.GetYBearing(SpectateTarget.SectorPosition, currentHudTarget.Value.GetSectorPosition());
			SetCameraDesiredRotationY(yBearing);
			UpdateDesiredCameraPosition(SpectateTarget.ActiveUnit);
		}

		private void UpdateDesiredCameraOrientation_FixedForward()
		{
			float yBearing = Unit.GetYBearing(SpectateTarget.transform.forward);
			SetCameraDesiredRotationY(yBearing);
			UpdateDesiredCameraPosition(SpectateTarget.ActiveUnit);
		}

		public void UpdateDesiredCameraOrientation(float desiredYAngle)
		{
			SetCameraDesiredRotationY(desiredYAngle);
			UpdateDesiredCameraPosition(SpectateTarget.ActiveUnit);
		}

		public void UpdateDesiredCameraPosition(ActiveUnit activeUnit)
		{
			if (!(activeUnit == null))
			{
				float minCameraDistance = activeUnit.ActiveUnitClass.MinCameraDistance;
				float maxCameraDistance = activeUnit.ActiveUnitClass.MaxCameraDistance;
				float num = Mathf.Lerp(minCameraDistance, maxCameraDistance, CamUnitDistance);
				float minCameraHeight = activeUnit.ActiveUnitClass.MinCameraHeight;
				float maxCameraHeight = activeUnit.ActiveUnitClass.MaxCameraHeight;
				float num2 = Mathf.Lerp(minCameraHeight, maxCameraHeight, cameraElevation);
				Vector3 vector = Quaternion.Euler(0f, camDesiredRotation.y, 0f) * (Vector3.back * num + Vector3.up * num2);
				camDesiredPosition = activeUnit.Unit.transform.position + vector;
			}
		}

		// Open Frontier: Free mode mirrors CameraSpectator's manual orbit -
		// the camera sits on the orbit sphere around the unit at the full
		// pitch/yaw angle, and the matching rotation looks back at it
		// (position = target + Q*(back*d), rotation = Q, is exactly LookAt).
		private void UpdateDesiredCameraPosition_Free(ActiveUnit activeUnit)
		{
			if (!(activeUnit == null))
			{
				float minCameraDistance = activeUnit.ActiveUnitClass.MinCameraDistance;
				float maxCameraDistance = activeUnit.ActiveUnitClass.MaxCameraDistance;
				float num = Mathf.Lerp(minCameraDistance, maxCameraDistance, CamUnitDistance);
				camDesiredPosition = activeUnit.Unit.transform.position + Quaternion.Euler(camDesiredRotation) * (Vector3.back * num);
			}
		}

		public void SetCameraDesiredRotationY(float desiredYAngle)
		{
			camDesiredRotation = new Vector3(EngineASX.Instance.GameSettings.HudCameraSettings.CamRelativeXAngle, desiredYAngle, 0f);
		}

		// Open Frontier: free-mode rotation with user pitch (the locked modes
		// keep the forced CamRelativeXAngle pitch via SetCameraDesiredRotationY).
		public void SetCameraDesiredRotation(float desiredXAngle, float desiredYAngle)
		{
			camDesiredRotation = new Vector3(desiredXAngle, desiredYAngle, 0f);
		}

		public bool HasHudTargetExpired()
		{
			return Time.time > currentHudTarget.Value.ExpiryTime;
		}

		public void SetHudTarget(HudTarget target)
		{
			LastTimeChangedTarget = -1f;
			currentHudTarget = target;
		}

		public void RemoveTarget()
		{
			if (HasTarget)
			{
				currentHudTarget = null;
				LastTimeChangedTarget = RealTime.time;
			}
		}

		public void RemoveTargetWithcooldown()
		{
			if (HasTarget)
			{
				currentHudTarget = HudTarget.Position(lastKnownUnitWorldPosition, Time.time + EngineASX.Instance.GameSettings.HudCameraSettings.CooldownTime);
				LastTimeChangedTarget = RealTime.time;
			}
		}

		private float GetDeltaTime()
		{
			if (!engine.IsPaused)
			{
				return Time.deltaTime;
			}
			return RealTime.deltaTime;
		}

		private float GetTime()
		{
			if (!engine.IsPaused)
			{
				return Time.time;
			}
			return RealTime.time;
		}

		private void Awake()
		{
		}

		private IEnumerator Start()
		{
			engine = EngineASX.Instance;
			while (true)
			{
				if (enabled)
				{
					Reposition();
				}
				yield return _waitForFixedUpdate;
			}
		}

		public void Init()
		{
			ResetZoomAndElevation();
		}

		private void ResetZoomAndElevation()
		{
			CameraElevation = GameController.Instance.GameSettings.HudCameraSettings.DefaultElevation;
			CamUnitDistance = GameController.Instance.GameSettings.HudCameraSettings.DefaultZoom;
		}

		private void Update()
		{
			if (!(SpectateTarget != null))
			{
				return;
			}
			MouseWheelScale();
			if (HasTarget)
			{
				HudTarget value = currentHudTarget.Value;
				if (value.IsUnit)
				{
					if (value.Unit == null || !value.Unit.IsValid)
					{
						RemoveTargetWithcooldown();
					}
					else if (value.Unit.IsDestroyed)
					{
						RemoveTargetWithcooldown();
					}
					else if (value.Unit.Sector != SpectateTarget.Sector)
					{
						RemoveTargetWithcooldown();
					}
					else
					{
						lastKnownUnitWorldPosition = value.Unit.transform.position;
					}
				}
				if (HasTarget && HasHudTargetExpired())
				{
					Unit unit = value.Unit;
					RemoveTarget();
					if (LostTarget != null)
					{
						LostTarget(this, unit);
					}
				}
			}
			if (engine.IsPaused && EngineASX.Instance.Hud != null && EngineASX.Instance.Hud.IsCurrentScreen)
			{
				Reposition();
			}
		}

		private void Reposition()
		{
			if (EngineASX.LoadedAndReady && EngineASX.Instance.ActiveSector != null && engine.MainCamera != null)
			{
				if (SpectateTarget != null && SpectateTarget.ActiveUnit != null)
				{
					UpdateDesiredCameraOrientation();
				}
				MoveCameraToDesired();
			}
		}

		private void MoveCameraToDesired()
		{
			MoveCameraToDesired(!EngineASX.Instance.GameSettings.HudCameraSettings.LerpCameraToTarget);
		}

		public void ResetOrientation()
		{
			if (GameController.Instance.PreferCameraLock)
			{
				EngineASX.Instance.Hud.CameraMode = HudCameraMode.LockTarget;
			}
			else
			{
				EngineASX.Instance.Hud.CameraMode = HudCameraMode.FixedForward;
			}
			if (SpectateTarget != null)
			{
				float yBearing = Unit.GetYBearing(SpectateTarget.transform.forward);
				SetCameraDesiredRotationY(yBearing);
				UpdateDesiredCameraOrientation();
				MoveCameraToDesired(instant: true);
			}
		}

		public void ResetAll()
		{
			ResetZoomAndElevation();
			ResetOrientation();
		}

		private void MouseWheelScale()
		{
			if (Input.mouseScrollDelta.y != 0f && IsMouseOverTouchTarget())
			{
				CamUnitDistance -= Input.mouseScrollDelta.y * GameController.Instance.GameSettings.HudCameraSettings.MouseWheelZoomRate;
			}
		}

		public bool IsMouseOverTouchTarget()
		{
			if (EventSystem.current == null)
			{
				return false;
			}
			PointerEventData eventData = new PointerEventData(EventSystem.current)
			{
				pointerId = -1,
				position = Input.mousePosition
			};
			List<RaycastResult> list = new List<RaycastResult>();
			EventSystem.current.RaycastAll(eventData, list);
			if (list.Count > 0)
			{
				return list[0].gameObject == EngineASX.Instance.Hud.TouchTargetButton;
			}
			return false;
		}
	}
}
