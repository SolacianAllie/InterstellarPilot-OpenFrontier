using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class SectorAppearanceOptions : MonoBehaviour
	{
		public TextMeshProUGUI SectorNameLabel;

		public Button ChangeSectorAppearanceButton;

		public Button RenameSectorButton;

		private void Awake()
		{
			ChangeSectorAppearanceButton.onClick.AddListener(ChangeSectorAppearanceButtonClicked);
			RefreshSectorNameLabel();
			RenameSectorButton.onClick.AddListener(RenameSectorButtonClick);
		}

		private void RefreshSectorNameLabel()
		{
			if (EngineASX.Instance.ActiveSector != null)
			{
				SectorNameLabel.text = EngineASX.Instance.ActiveSector.Name;
			}
			else
			{
				SectorNameLabel.text = "-";
			}
		}

		private void RenameSectorButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameSectorScreen(EngineASX.Instance.ActiveSector, RenameSectorButtonClickCallback);
		}

		private void RenameSectorButtonClickCallback(RenameUnitScreen handler, bool rename, string newName)
		{
			if (rename)
			{
				EngineASX.Instance.ActiveSector.Name = newName;
				RefreshSectorNameLabel();
			}
		}

		private void ChangeSectorAppearanceButtonClicked()
		{
			UIController.Instance.ScreenNavigator.ShowChangeSectorAppearanceScreen();
		}
	}
}
