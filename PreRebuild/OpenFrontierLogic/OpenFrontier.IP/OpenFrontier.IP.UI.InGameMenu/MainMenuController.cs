using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Hypersleep;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.InGameMenu
{
	public class MainMenuController : MonoBehaviour
	{
		public Button HypersleepButton;

		public Button ScannerButton;

		public Button ViewOptionsButton;

		private void Awake()
		{
			HypersleepButton.gameObject.SetActive(HypersleepHelper.ShouldShowHypersleepOption());
			ScannerButton.onClick.AddListener(ScannerButtonClick);
			ViewOptionsButton.onClick.AddListener(ViewOptionsButtonClick);
		}

		private void Refresh()
		{
			ViewOptionsButton.interactable = EngineASX.Instance.World.Permissions.AllowHudViewOptions;
		}

		private void ScannerButtonClick()
		{
			EngineASX.Instance.Hud.HUDScannerDisplayListController.Show();
		}

		private void ViewOptionsButtonClick()
		{
			if (!EngineASX.Instance.World.Permissions.AllowHudViewOptions)
			{
				UIController.Instance.OnDeniedUIOption();
			}
			else
			{
				EngineASX.Instance.Hud.ViewOptions.ModalWindow.ToggleActive();
			}
		}
	}
}
