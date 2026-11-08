using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class HudComponentManager : MonoBehaviour
	{
		public CanvasGroup CurrentSpeedCanvasGroup;

		public CanvasGroup CommsCanvasGroup;

		public CanvasGroup CompassCanvasGroup;

		public CanvasGroup CapacitorCanvasGroup;

		public CanvasGroup ComponentsCanvasGroup;

		public CanvasGroup TargetPanelCanvasGroup;

		public CanvasGroup ThrottleCanvasGroup;

		public CanvasGroup DockOptionsCanvasGroup;

		public CanvasGroup TractorOptionsCanvasGroup;

		public CanvasGroup TurnOptionsCanvasGroup;

		public CanvasGroup TargetsCanvasGroup;

		public CanvasGroup DialogCanvasGroup;

		public CanvasGroup MissileLocksCanvasGroup;

		public CanvasGroup WorldSpaceShieldsCanvasGroup;

		public CanvasGroup WorldSpaceHullCanvasGroup;

		public CanvasGroup ScreenSpaceShieldsCanvasGroup;

		public CanvasGroup ScreenSpaceHullCanvasGroup;

		public CanvasGroup AutoPilotCanvasGroup;

		public CanvasGroup CameraControlsCanvasGroup;

		public CanvasGroup GetHudComponent(HudComponent componentType)
		{
			switch (componentType)
			{
			case HudComponent.ControlsBg:
				return null;
			case HudComponent.PlayerSpdLabel:
				return CurrentSpeedCanvasGroup;
			case HudComponent.CommsButton:
				return CommsCanvasGroup;
			case HudComponent.Compass:
				return CompassCanvasGroup;
			case HudComponent.PlayerCapacitor:
				return CapacitorCanvasGroup;
			case HudComponent.TurretComponents:
				return ComponentsCanvasGroup;
			case HudComponent.PlayerHull:
				if (GameController.Instance.PlayerOptions.UI_ScreenSpaceShields)
				{
					return ScreenSpaceHullCanvasGroup;
				}
				return WorldSpaceHullCanvasGroup;
			case HudComponent.PlayerShields:
				if (GameController.Instance.PlayerOptions.UI_ScreenSpaceShields)
				{
					return ScreenSpaceShieldsCanvasGroup;
				}
				return WorldSpaceShieldsCanvasGroup;
			case HudComponent.TargetPanel:
				return TargetPanelCanvasGroup;
			case HudComponent.TargetSprites:
				return TargetsCanvasGroup;
			case HudComponent.Throttle:
				return ThrottleCanvasGroup;
			case HudComponent.DockButton:
				return DockOptionsCanvasGroup;
			case HudComponent.TractorLoot:
				return TractorOptionsCanvasGroup;
			case HudComponent.TurnButtons:
				return TurnOptionsCanvasGroup;
			case HudComponent.MissileLocks:
				return MissileLocksCanvasGroup;
			case HudComponent.Dialog:
				return DialogCanvasGroup;
			case HudComponent.AutoPilot:
				return AutoPilotCanvasGroup;
			case HudComponent.CameraControls:
				return CameraControlsCanvasGroup;
			default:
				if (LogWrapper.LogMsgs)
				{
					Debug.LogWarning($"The HudComponent of type {componentType} is not known", this);
				}
				return null;
			}
		}

		public void SetHudComponentVisible(HudComponent componentType, bool visible)
		{
			CanvasGroup hudComponent = GetHudComponent(componentType);
			if (hudComponent != null)
			{
				hudComponent.enabled = visible;
			}
		}

		public bool GetHudComponentActive(HudComponent componentType)
		{
			CanvasGroup hudComponent = GetHudComponent(componentType);
			if (hudComponent != null)
			{
				return hudComponent.gameObject.activeInHierarchy;
			}
			return false;
		}

		public void SetHudComponentActive(HudComponent componentType, bool active)
		{
			CanvasGroup hudComponent = GetHudComponent(componentType);
			if (hudComponent != null)
			{
				hudComponent.gameObject.SetActive(active);
			}
		}

		public void SetHudComponentInteractable(HudComponent componentType, bool interactable)
		{
			CanvasGroup hudComponent = GetHudComponent(componentType);
			if (hudComponent != null)
			{
				hudComponent.interactable = interactable;
			}
		}
	}
}
