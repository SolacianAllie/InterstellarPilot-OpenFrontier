using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class BackButtonController : MonoBehaviour
	{
		public Button Button;

		private void Awake()
		{
			if (Button == null)
			{
				Button = GetComponent<Button>();
				if (Button == null)
				{
					Debug.LogError("Expecting back button");
				}
			}
			if (Button != null)
			{
				Button.onClick.AddListener(ButtonClick);
			}
		}

		private void ButtonClick()
		{
			UIController.Instance.ScreenNavigator.TryNavigateBack();
		}
	}
}
