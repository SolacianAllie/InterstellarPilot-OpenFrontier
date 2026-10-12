using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.HelpSystem
{
	[RequireComponent(typeof(Button))]
	public class TargettedHelpButton : MonoBehaviour
	{
		private Button button;

		public Transform TargetTransform;

		public bool IncludeScreenPrimaryHelp;

		private void Awake()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(() =>
			{
				if (UIController.Instance.ScreenNavigator.CurrentScreen != null)
				{
					UIController.Instance.ScreenNavigator.CurrentScreen.ShowHelp(TargetTransform, IncludeScreenPrimaryHelp);
				}
			});
		}
	}
}
