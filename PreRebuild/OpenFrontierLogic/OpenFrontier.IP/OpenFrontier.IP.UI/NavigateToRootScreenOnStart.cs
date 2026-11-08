using OpenFrontier.IP.UI.Screens;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class NavigateToRootScreenOnStart : MonoBehaviour
	{
		private ScreenBase screen;

		private void Awake()
		{
			screen = GetComponent<ScreenBase>();
		}

		private void Update()
		{
			if (screen != null && ScreenNavigator.Instance != null && ScreenNavigator.Instance.RootScreen != screen)
			{
				ScreenNavigator.Instance.NavigateToRootScreen(screen);
				Object.Destroy(this);
			}
		}
	}
}
