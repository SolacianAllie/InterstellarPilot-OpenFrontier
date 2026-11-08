using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Fleets;
using OpenFrontier.IP.UI.Screens.Hud;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.Test;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI.Screens.Home
{
	public class UIHotKeyController : MonoBehaviour
	{
		public bool AllowNavigationAwayFromScreen(ScreenBase screenBase)
		{
			if (screenBase is HudScreen || screenBase is GodModeScreen)
			{
				return true;
			}
			return false;
		}

		private void Update()
		{
			if (!EngineASX.LoadedAndReady || EngineASX.Instance.LocalPlayer == null || (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null))
			{
				return;
			}
			ScreenBase currentScreen = UIController.Instance.ScreenNavigator.CurrentScreen;
			if (currentScreen != null && currentScreen.FadingState == ScreenFadeState.None)
			{
				if (Input.GetKeyDown(KeyCode.Pause) && currentScreen.ShouldAllowTogglePause())
				{
					EngineASX.Instance.TogglePause();
				}
				NavigationHotKeyUpdate();
			}
		}

		private void NavigationHotKeyUpdate()
		{
			ScreenBase currentScreen = UIController.Instance.ScreenNavigator.CurrentScreen;
			if (AllowNavigationAwayFromScreen(UIController.Instance.ScreenNavigator.CurrentScreen) || UIController.Instance.DockUIHeader.gameObject.activeInHierarchy)
			{
				if (Input.GetKeyDown(KeyCode.P) && currentScreen.RequestNavigationAwayTo<PropertyScreen>())
				{
					UIController.Instance.ScreenNavigator.TogglePropertyScreen();
				}
				if (Input.GetKeyDown(KeyCode.O) && currentScreen.RequestNavigationAwayTo<FleetsScreen>())
				{
					UIController.Instance.ScreenNavigator.ToggleFleetsScreen();
				}
				if (Input.GetKeyDown(KeyCode.L) && currentScreen.RequestNavigationAwayTo<LogScreen>())
				{
					UIController.Instance.ScreenNavigator.ToggleLogScreen();
				}
				if (Input.GetKeyDown(KeyCode.Period) && currentScreen.RequestNavigationAwayTo<SectorMapScreen>())
				{
					UIController.Instance.ScreenNavigator.ToggleSectorMapScreenWhenPilotting();
				}
				if (Input.GetKeyDown(KeyCode.Comma) && currentScreen.RequestNavigationAwayTo<UniverseMapScreen>())
				{
					DockUIHeader.ToggleUniverseMap();
				}
				if (Input.GetKeyDown(KeyCode.M) && currentScreen.RequestNavigationAwayTo<MessagesUI>())
				{
					UIController.Instance.ScreenNavigator.ToggleMessagesScreen();
				}
				if (Input.GetKeyDown(KeyCode.G) && currentScreen.RequestNavigationAwayTo<GodModeScreen>())
				{
					UIController.Instance.ScreenNavigator.ToggleGodModeScreen();
				}
			}
		}
	}
}
