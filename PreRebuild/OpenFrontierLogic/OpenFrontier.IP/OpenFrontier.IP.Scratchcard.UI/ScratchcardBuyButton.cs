using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.Scratchcard.UI
{
	public class ScratchcardBuyButton : MonoBehaviour
	{
		private Button button;

		private Text buttonLabel;

		public ScratchcardScreen Screen;

		private void Start()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(ButtonClick);
			buttonLabel = GetComponentInChildren<Text>();
			buttonLabel.text = $"Buy ({Screen.ScratchcardSetup.CostPerCard:N0} Cr)";
		}

		private void Update()
		{
			button.interactable = Screen.Faction != null && Screen.Faction.Credits >= Screen.ScratchcardSetup.CostPerCard;
		}

		private void ButtonClick()
		{
			Screen.Play();
		}
	}
}
