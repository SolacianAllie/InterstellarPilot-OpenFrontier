using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class HudViewOptions : MonoBehaviour
	{
		public ModalWindow ModalWindow;

		public Toggle ShowStatusBarToggle;

		public Toggle ShowComponentsToggle;

		public Toggle ShowTargetPanelToggle;

		public Toggle ShowTargetsToggle;

		public Toggle ShowControlsToggle;

		public Toggle ShowDialogToggle;

		public Toggle ShowIndicatorsToggle;

		public Toggle ShowTargetIndicatorsToggle;

		private void Awake()
		{
			ShowStatusBarToggle.onValueChanged.AddListener(ShowStatusBarValueChanged);
			ShowComponentsToggle.onValueChanged.AddListener(ShowComponentsToggleValueChanged);
			ShowTargetPanelToggle.onValueChanged.AddListener(ShowTargetPanelValueChanged);
			ShowTargetsToggle.onValueChanged.AddListener(ShowTargetsToggleValueChanged);
			ShowControlsToggle.onValueChanged.AddListener(ShowControlsToggleValueChanged);
			ShowDialogToggle.onValueChanged.AddListener(ShowDialogToggleValueChanged);
			ShowIndicatorsToggle.onValueChanged.AddListener(ShowIndicatorsToggleValueChanged);
			ShowTargetIndicatorsToggle.onValueChanged.AddListener(ShowTargetIndicatorsToggleValueChanged);
			ModalWindow.Opening += ModalWindow_Opening;
		}

		private bool ModalWindow_Opening(ModalWindow sender)
		{
			RefreshControls();
			return true;
		}

		private void RefreshControls()
		{
			ShowStatusBarToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudHeader);
			ShowComponentsToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudComponents);
			ShowTargetPanelToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudTargetPanel);
			ShowTargetsToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudTargets);
			ShowControlsToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudControls);
			ShowDialogToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudDialog);
			ShowIndicatorsToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudIndicators);
			ShowTargetIndicatorsToggle.SetIsOnWithoutNotify(GameController.Instance.PlayerOptions.UI_ShowHudTargetIndicators);
		}

		private void ShowStatusBarValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudHeader = value;
			ApplyShowHeaderSetting();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudHeaderKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudHeader));
			PlayerPrefs.Save();
		}

		private void ShowComponentsToggleValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudComponents = value;
			ApplyHudComponentsSetting();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudComponentsKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudComponents));
			PlayerPrefs.Save();
		}

		private void ShowTargetPanelValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudTargetPanel = value;
			ApplyTargetPanelSetting();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudTargetPanelKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudTargetPanel));
			PlayerPrefs.Save();
		}

		private void ShowTargetsToggleValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudTargets = value;
			ApplyTargetsSetting();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudTargetsKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudTargets));
			PlayerPrefs.Save();
		}

		private void ShowControlsToggleValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudControls = value;
			ApplyControlsSetting();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudControlsKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudControls));
			PlayerPrefs.Save();
		}

		private void ShowDialogToggleValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudDialog = value;
			ApplyDialogSetting();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudDialogKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudDialog));
			PlayerPrefs.Save();
		}

		private void ShowIndicatorsToggleValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudIndicators = value;
			ApplyIndicatorsSetting();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudIndicatorsKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudIndicators));
			PlayerPrefs.Save();
		}

		private void ShowTargetIndicatorsToggleValueChanged(bool value)
		{
			GameController.Instance.PlayerOptions.UI_ShowHudTargetIndicators = value;
			ApplyTargetIndicatorsSettings();
			PlayerPrefs.SetInt(GameController.Instance.PlayerOptionConstants.UI_ShowHudTargetIndicatorsKey, Helper.BoolToInt(GameController.Instance.PlayerOptions.UI_ShowHudTargetIndicators));
			PlayerPrefs.Save();
		}

		public void ApplyPlayerPrefs()
		{
			RefreshControls();
			ApplyShowHeaderSetting();
			ApplyTargetsSetting();
			ApplyHudComponentsSetting();
			ApplyTargetPanelSetting();
			ApplyControlsSetting();
			ApplyDialogSetting();
			ApplyIndicatorsSetting();
			ApplyTargetIndicatorsSettings();
		}

		private static void ApplyShowHeaderSetting()
		{
			EngineASX.Instance.Hud.RefreshHudPadder();
		}

		private static void ApplyHudComponentsSetting()
		{
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.TurretComponents, GameController.Instance.PlayerOptions.UI_ShowHudComponents);
		}

		private static void ApplyTargetPanelSetting()
		{
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.TargetPanel, GameController.Instance.PlayerOptions.UI_ShowHudTargetPanel);
		}

		private static void ApplyTargetsSetting()
		{
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.TargetSprites, GameController.Instance.PlayerOptions.UI_ShowHudTargets);
		}

		private static void ApplyControlsSetting()
		{
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.TurnButtons, GameController.Instance.PlayerOptions.UI_ShowHudControls);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.Throttle, GameController.Instance.PlayerOptions.UI_ShowHudControls);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.DockButton, GameController.Instance.PlayerOptions.UI_ShowHudControls);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.CommsButton, GameController.Instance.PlayerOptions.UI_ShowHudControls);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.TractorLoot, GameController.Instance.PlayerOptions.UI_ShowHudControls);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.CameraControls, GameController.Instance.PlayerOptions.UI_ShowHudControls);
		}

		private static void ApplyDialogSetting()
		{
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.Dialog, GameController.Instance.PlayerOptions.UI_ShowHudDialog);
		}

		private static void ApplyIndicatorsSetting()
		{
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.PlayerCapacitor, GameController.Instance.PlayerOptions.UI_ShowHudIndicators);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.PlayerShields, GameController.Instance.PlayerOptions.UI_ShowHudIndicators);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.PlayerHull, GameController.Instance.PlayerOptions.UI_ShowHudIndicators);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.MissileLocks, GameController.Instance.PlayerOptions.UI_ShowHudIndicators);
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.AutoPilot, GameController.Instance.PlayerOptions.UI_ShowHudIndicators);
		}

		private static void ApplyTargetIndicatorsSettings()
		{
			EngineASX.Instance.Hud.ComponentManager.SetHudComponentActive(HudComponent.Compass, GameController.Instance.PlayerOptions.UI_ShowHudTargetIndicators);
		}
	}
}
