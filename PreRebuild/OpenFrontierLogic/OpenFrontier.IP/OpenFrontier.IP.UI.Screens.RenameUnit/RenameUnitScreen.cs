using System.Text.RegularExpressions;
using OpenFrontier.IP.UI.Screens.MessageBox;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.RenameUnit
{
	public class RenameUnitScreen : ScreenBase
	{
		public delegate void RenameConfirmedHandler(RenameUnitScreen handler, bool rename, string newName);

		public TextMeshProUGUI TitleLabel;

		public InputField InputField;

		public Button OkButton;

		public Button CancelButton;

		public Text CurrentNameText;

		private string newName;

		public Text MaxCharactersText;

		public int MinCharacters;

		public bool RenameOnConfirm;

		public string RegexPatternValidation { get; set; }

		public string RegexPatternValidationMessage { get; set; }

		public string TitleText
		{
			get
			{
				return TitleLabel.text;
			}
			set
			{
				TitleLabel.text = value;
			}
		}

		public int MaxCharacters
		{
			get
			{
				return InputField.characterLimit;
			}
			set
			{
				InputField.characterLimit = value;
			}
		}

		public string NewName
		{
			get
			{
				return newName;
			}
			set
			{
				newName = value;
			}
		}

		public string CurrentName
		{
			get
			{
				return CurrentNameText.text;
			}
			set
			{
				CurrentNameText.text = value;
			}
		}

		public event RenameConfirmedHandler RenameConfirmed;

		protected override void awake()
		{
			base.awake();
			OkButton.onClick.AddListener(OkButtonClick);
			CancelButton.onClick.AddListener(CancelButtonClick);
		}

		protected override void refresh()
		{
			base.refresh();
			InputField.text = newName;
			MaxCharactersText.text = $"({MinCharacters}-{MaxCharacters} chars)";
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (navigatedForward)
			{
				EventSystem.current.SetSelectedGameObject(InputField.gameObject);
			}
		}

		protected override void update()
		{
			base.update();
			OkButton.interactable = InputField.text.Length >= MinCharacters;
		}

		private void OkButtonClick()
		{
			if (!string.IsNullOrWhiteSpace(RegexPatternValidation) && !Regex.IsMatch(InputField.text, RegexPatternValidation))
			{
				string message = ((!string.IsNullOrWhiteSpace(RegexPatternValidationMessage)) ? RegexPatternValidationMessage : "The input text is invalid");
				UIController.Instance.ShowMessageBox(message, MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			if (RenameConfirmed != null)
			{
				RenameConfirmed(this, rename: true, InputField.text);
			}
			NavigateBack();
		}

		private void CancelButtonClick()
		{
			if (RenameConfirmed != null)
			{
				RenameConfirmed(this, rename: false, InputField.text);
			}
			NavigateBack();
		}
	}
}
