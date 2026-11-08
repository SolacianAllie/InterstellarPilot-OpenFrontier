using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class GodModeIntelOptions : MonoBehaviour
	{
		public Button DiscoverEverythingButton;

		public Button ClearIntelButton;

		private void Awake()
		{
			DiscoverEverythingButton.onClick.AddListener(DiscoverEverythingButtonClick);
			ClearIntelButton.onClick.AddListener(ClearIntelButtonClick);
		}

		private void DiscoverEverythingButtonClick()
		{
			if (EngineASX.Instance != null && EngineASX.Instance.LocalFaction != null)
			{
				EngineASX.Instance.LocalFaction.Intel.DiscoverEverything();
				EngineASX.Instance.LocalFaction.InvalidateTraderTargets();
				UIController.Instance.ShowMessageBox("All objects added to intel", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
			}
		}

		private void ClearIntelButtonClick()
		{
			EngineASX.Instance.LocalFaction.Intel.Clear();
			UIController.Instance.ShowMessageBox("All player intel removed", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
		}
	}
}
