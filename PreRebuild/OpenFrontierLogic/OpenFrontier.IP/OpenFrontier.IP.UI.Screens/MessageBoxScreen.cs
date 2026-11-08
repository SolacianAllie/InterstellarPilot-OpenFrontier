using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.MessageBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class MessageBoxScreen : ScreenBase
	{
		public delegate void MessageBoxDismissedHandler(MessageBoxScreen sender, MessageBoxResult result);

		public class ShowMessageArgs
		{
			public MessageBoxIcon Icon;

			public MessageBoxButtons Buttons = MessageBoxButtons.Ok;

			public string Text;

			public string Title;

			public TextAlignmentOptions TextAlignment = TextAlignmentOptions.Center;
		}

		public class PanelDeactivateArgs
		{
			public ScreenBase Screen;

			public static PanelDeactivateArgs Create(ScreenBase screen)
			{
				PanelDeactivateArgs panelDeactivateArgs = new PanelDeactivateArgs();
				panelDeactivateArgs.Screen = screen;
				panelDeactivateArgs.DisableCanvasGroupsInput();
				panelDeactivateArgs.FadeCanvasGroups();
				return panelDeactivateArgs;
			}

			public void Restore()
			{
				SetCanvasGroupsInteractable(interactable: true);
				RestoreCanvasGroupsAlpha();
			}

			private void FadeCanvasGroups()
			{
				SetCanvasGroupsAlpha(GameController.Instance.FadedScreenAlpha);
			}

			private void RestoreCanvasGroupsAlpha()
			{
			}

			private void SetCanvasGroupsAlpha(float alpha)
			{
				Screen.CanvasGroupAlpha = alpha;
			}

			private void DisableCanvasGroupsInput()
			{
				SetCanvasGroupsInteractable(interactable: false);
			}

			private void SetCanvasGroupsInteractable(bool interactable)
			{
				foreach (CanvasGroup canvasGroup in Screen.CanvasGroups)
				{
					if (canvasGroup != null)
					{
						canvasGroup.interactable = interactable;
					}
				}
			}
		}

		public ScrollRect TextScrollRect;

		public RectTransform AutoSizeTarget;

		public float MinHeight = 250f;

		public float MaxHeight = 600f;

		public float MinWidth = 635f;

		public float MaxWidth = 735f;

		public int MinSizeCharacters = 30;

		public int MaxSizeCharacters = 200;

		public MessageBoxIconSettings IconSettings;

		public Image IconImage;

		public GameObject IconRoot;

		private MessageBoxIcon icon;

		private MessageBoxButtons buttons = MessageBoxButtons.Ok;

		public Button CancelButton;

		private MessageBoxResult currentResult;

		private PanelDeactivateArgs deactivateArgs;

		public Button OkButton;

		public TextMeshProUGUI TextLabel;

		public Text TitleLabel;

		private bool cancelDismiss;

		public string Text
		{
			get
			{
				return TextLabel.text;
			}
			set
			{
				TextLabel.text = value;
			}
		}

		public MessageBoxIcon Icon
		{
			get
			{
				return icon;
			}
			set
			{
				if (icon != value)
				{
					icon = value;
					ApplyIcon();
				}
			}
		}

		public MessageBoxButtons Buttons
		{
			get
			{
				return buttons;
			}
			set
			{
				if (buttons != value)
				{
					buttons = value;
					ApplyButtons();
				}
			}
		}

		public bool CancelDismiss
		{
			get
			{
				return cancelDismiss;
			}
			set
			{
				cancelDismiss = value;
			}
		}

		public MessageBoxResult Result => currentResult;

		public event MessageBoxDismissedHandler Dismissed;

		private void ApplyIcon()
		{
			IconRoot.SetActive(icon != MessageBoxIcon.None);
			switch (icon)
			{
			case MessageBoxIcon.Ok:
				IconImage.sprite = IconSettings.OkSprite;
				break;
			case MessageBoxIcon.Warning:
				IconImage.sprite = IconSettings.WarningSprite;
				break;
			case MessageBoxIcon.Error:
				IconImage.sprite = IconSettings.ErrorSprite;
				break;
			case MessageBoxIcon.Question:
				IconImage.sprite = IconSettings.QuestionSprite;
				break;
			case MessageBoxIcon.Info:
				IconImage.sprite = IconSettings.InfoSprite;
				break;
			}
		}

		public void Cancel()
		{
			Finish(MessageBoxResult.Cancel);
		}

		public void Ok()
		{
			Finish(MessageBoxResult.Ok);
		}

		public void Show(ShowMessageArgs args, MessageBoxDismissedHandler callback = null)
		{
			cancelDismiss = true;
			currentResult = MessageBoxResult.None;
			AutoSize(args.Text);
			if (args != null)
			{
				UIController.Instance.ScreenNavigator.NavigateToScreen(this);
				Dismissed = callback;
				if (TextLabel != null)
				{
					TextLabel.text = args.Text;
					TextLabel.alignment = args.TextAlignment;
				}
				if (TitleLabel != null)
				{
					TitleLabel.gameObject.SetActive(!string.IsNullOrEmpty(args.Title));
					TitleLabel.text = args.Title;
				}
				TextScrollRect.ResetScrollPosition();
				buttons = args.Buttons;
				icon = args.Icon;
				ApplyIcon();
				ApplyButtons();
			}
			else
			{
				Debug.LogWarning("Message box shown with no result");
			}
		}

		private void AutoSize(string text)
		{
			Vector2 sizeDelta = AutoSizeTarget.sizeDelta;
			float t = Mathf.Clamp01((float)(text.Length - MinSizeCharacters) / (float)(MaxSizeCharacters - MinSizeCharacters));
			sizeDelta.x = Mathf.Lerp(MinWidth, MaxWidth, t);
			sizeDelta.y = Mathf.Lerp(MinHeight, MaxHeight, t);
			AutoSizeTarget.sizeDelta = sizeDelta;
		}

		protected override void awake()
		{
			base.awake();
			OkButton.onClick.AddListener(Ok);
			CancelButton.onClick.AddListener(Cancel);
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			ScreenBase backTarget = GetBackTarget();
			if (backTarget != null)
			{
				deactivateArgs = PanelDeactivateArgs.Create(backTarget);
			}
		}

		protected override void onNotCurrentPanel()
		{
			base.onNotCurrentPanel();
			if (deactivateArgs != null)
			{
				deactivateArgs.Restore();
				deactivateArgs = null;
			}
		}

		protected override bool AllowNavigateBack()
		{
			return buttons == MessageBoxButtons.Ok;
		}

		private void Finish(MessageBoxResult result)
		{
			cancelDismiss = false;
			currentResult = result;
			if (Dismissed != null)
			{
				Dismissed(this, result);
			}
			if (!cancelDismiss && IsCurrentScreen)
			{
				NavigateBack();
			}
		}

		private void ApplyButtons()
		{
			switch (buttons)
			{
			case MessageBoxButtons.Ok:
				OkButton.gameObject.SetActive(value: true);
				CancelButton.gameObject.SetActive(value: false);
				break;
			case MessageBoxButtons.OkCancel:
				CancelButton.gameObject.SetActive(value: true);
				OkButton.gameObject.SetActive(value: true);
				break;
			case MessageBoxButtons.None:
				CancelButton.gameObject.SetActive(value: false);
				OkButton.gameObject.SetActive(value: false);
				break;
			case MessageBoxButtons.Cancel:
				CancelButton.gameObject.SetActive(value: true);
				OkButton.gameObject.SetActive(value: false);
				break;
			}
		}
	}
}
