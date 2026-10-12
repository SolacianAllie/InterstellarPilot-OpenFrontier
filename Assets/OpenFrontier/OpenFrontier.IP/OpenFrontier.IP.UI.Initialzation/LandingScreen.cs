using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.IP.UI.Initialzation
{
	public class LandingScreen : MonoBehaviour
	{
		private void Update()
		{
			if (GameController.Instance != null && UIController.Instance != null && UIController.Instance.HasLoaded)
			{
				if (ShouldShowScaleScreen())
				{
					SceneManager.LoadScene(ScreenNames.UIScaleScreenName);
				}
				else
				{
					UI.QuitToMainMenu();
				}
				Object.Destroy(this);
			}
		}

		public bool ShouldShowScaleScreen()
		{
			return false;
		}
	}
}
