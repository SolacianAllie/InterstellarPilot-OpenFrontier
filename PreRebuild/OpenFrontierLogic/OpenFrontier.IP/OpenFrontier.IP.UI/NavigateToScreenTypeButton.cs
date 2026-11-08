using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class NavigateToScreenTypeButton : MonoBehaviour
	{
		private Button hudButton;

		public GameObject ScreenContext;

		public UIScreenType ScreenType;

		private void Awake()
		{
			hudButton = GetComponent<Button>();
			hudButton.onClick.AddListener(hudButton_Activated);
		}

		private void hudButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowScreenType(ScreenType, ScreenContext);
		}
	}
}
