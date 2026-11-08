using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Skirmish;
using OpenFrontier.IP.UI.Screens.UniverseSandboxSettings;
using OpenFrontier.IP.UI.Screens.UniverseSelect;

namespace OpenFrontier.IP.UI.Screens.GameMode
{
	public class GameModeScreen : ScreenBase
	{
		public void NavigateToUniverse()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<UniverseSelectScreen>();
		}

		public void NavigateToUniverseSandbox()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<UniverseSandboxSettingsScreen>();
		}

		public void NavigateToScenarios()
		{
			UIController.Instance.ScreenNavigator.ShowScenariosScreen(GameController.EngineLaunchSource.ScenariosUI, showTutorials: false, showMissions: true);
		}

		public void NavigateToSkirmish()
		{
			UIController.Instance.ScreenNavigator.NavigateToScreen<SkirmishSetupScreen>();
		}

		public void NavigateToTutorials()
		{
			UIController.Instance.ScreenNavigator.ShowTutorialsScreen();
		}

		public void NavigateToBattles()
		{
			UIController.Instance.ScreenNavigator.ShowBattlesScreen();
		}
	}
}
