using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.EnterNumber
{
	public class EnterNumberScreen : ScreenBase
	{
		public delegate void EnterNumberConfirmedHandler(EnterNumberScreen handler, bool enteredValue, int? newValue);

		public InputField InputField;

		public Button OkButton;

		public Button CancelButton;

		public int Min = int.MinValue;

		public int Max = int.MaxValue;

		public Text PromptText;

		public event EnterNumberConfirmedHandler EnterNumberConfirmed;

		protected override void awake()
		{
			base.awake();
			OkButton.onClick.AddListener(OkButtonClick);
			CancelButton.onClick.AddListener(CancelButtonClick);
		}

		protected override void refresh()
		{
			base.refresh();
		}

		protected override void update()
		{
			base.update();
			OkButton.interactable = ValidateInput();
		}

		private bool ValidateInput()
		{
			if (int.TryParse(InputField.text, out var result))
			{
				if (result < Min)
				{
					return false;
				}
				if (result > Max)
				{
					return false;
				}
				return true;
			}
			return false;
		}

		private void OkButtonClick()
		{
			if (EnterNumberConfirmed != null)
			{
				EnterNumberConfirmed(this, enteredValue: true, int.Parse(InputField.text));
			}
			NavigateBack();
		}

		private void CancelButtonClick()
		{
			if (EnterNumberConfirmed != null)
			{
				EnterNumberConfirmed(this, enteredValue: false, null);
			}
			NavigateBack();
		}
	}
}
