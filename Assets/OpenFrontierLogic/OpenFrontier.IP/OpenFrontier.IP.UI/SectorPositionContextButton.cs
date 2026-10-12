using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SectorPositionContextButton : MonoBehaviour
	{
		public bool HideWhenNoTarget = true;

		public Button Button;

		public SectorTarget SectorTarget;

		public bool Interactable = true;

		public TextMeshProUGUI Label;

		public bool ShortName = true;

		public bool HandleClick = true;

		private void Awake()
		{
			Button.onClick.AddListener(ButtonClick);
			if (HideWhenNoTarget)
			{
				Button.gameObject.SetActive(value: false);
			}
		}

		public void RefreshIsInteractable()
		{
			if (Button.gameObject.activeSelf)
			{
				Button.interactable = Interactable && SectorTarget != null;
			}
		}

		public void Refresh()
		{
			if (Button == null)
			{
				Debug.LogError("Missing button", this);
				return;
			}
			if (SectorTarget != null)
			{
				Button.gameObject.SetActive(value: true);
				if (Label != null)
				{
					Label.text = TextFormattingHelper.FormatSectorPosition(SectorTarget.SectorPosition);
				}
			}
			else if (HideWhenNoTarget)
			{
				Button.gameObject.SetActive(value: false);
			}
			RefreshIsInteractable();
		}

		public void SetTarget(SectorTarget sectorTarget)
		{
			SectorTarget = sectorTarget;
			Refresh();
		}

		private void ButtonClick()
		{
			if (HandleClick)
			{
				UIController.Instance.ScreenNavigator.ShowSectorPositionContextMenuScreen(SectorTarget);
			}
		}
	}
}
