using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine.SceneManagement;

namespace OpenFrontier.IP.UI
{
	public class UIWinScreen : ScreenBase
	{
		public override bool ForceShowBackButton()
		{
			return true;
		}

		protected override bool onNavigatingBack()
		{
			GameController.Instance.LaunchOrigin = GameController.EngineLaunchSource.Unspecified;
			SceneManager.LoadScene(ScreenNames.MenuSceneName, LoadSceneMode.Single);
			return false;
		}
	}
}
