using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.FactionSettings
{
	public class FactionSettingsButton : MonoBehaviour
	{
		private Button button;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(OnClick);
		}

		private void OnClick()
		{
			UIController.Instance.ScreenNavigator.ShowFactionSettingsScreen(EngineASX.Instance.LocalFaction);
		}
	}
}
