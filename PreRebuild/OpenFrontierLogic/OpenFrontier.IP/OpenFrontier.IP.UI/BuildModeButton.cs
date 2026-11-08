using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class BuildModeButton : MonoBehaviour
	{
		private void Awake()
		{
			GetComponent<Button>().onClick.AddListener(ButtonClick);
		}

		private void ButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowBuildModeScreen();
		}
	}
}
