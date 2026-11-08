using System;
using System.Collections;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class UIController : MonoBehaviour
	{
		public Canvas HomeCanvas;

		public TextMeshProUGUI VersionWatermarkLabel;

		public DockUIHeader DockUIHeader;

		public SavingGameIcon SavingGameIcon;

		public AudioClip NavigateForwardAudio;

		public AudioClip NavigateBackAudio;

		private MessageBoxScreen messageBoxScreen;

		public ScreenNavigator ScreenNavigator;

		public HudQuickMsg QuickMsg;

		private static UIController instance;

		private bool hasLoaded;

		public bool HasLoaded => hasLoaded;

		public static UIController Instance => instance;

		public MessageBoxScreen MessageBoxScreen
		{
			get
			{
				return messageBoxScreen;
			}
			set
			{
				messageBoxScreen = value;
			}
		}

		private void Awake()
		{
			instance = this;
			DockUIHeader.gameObject.SetActive(value: false);
			// Open Frontier: the alpha watermark is retired - the main menu
			// version text carries the (project-driven) version and alpha tag.
			if (VersionWatermarkLabel != null)
			{
				VersionWatermarkLabel.enabled = false;
			}
		}

		public void OnDeniedUIOption()
		{
		}

		private void Start()
		{
			if (ScreenNavigator == null)
			{
				throw new Exception("Screen navigator is missing");
			}
			GameController.Instance.UIController = this;
			transform.SetParent(GameController.Instance.transform);
			transform.localPosition = Vector3.zero;
			StartCoroutine(LoadMessageBoxSceneCoroutine());
			StartCoroutine(LoadPausedSceneCoroutine());
			ScreenNavigator.ScreenNavigated += ScreenNavigator_ScreenNavigated;
			ScreenNavigator.NavigatedBack += ScreenNavigator_NavigatedBack;
			GameController.Instance.ScreenScaler.ScreenNavigator = ScreenNavigator;
			GameController.Instance.ScreenScaler.ScaleScreen(gameObject);
		}

		private void ScreenNavigator_NavigatedBack(ScreenNavigator sender, ScreenBase newScreen)
		{
			if (GameController.Instance.PlayButtonSounds && NavigateBackAudio != null)
			{
				AudioHelper.PlaySound(NavigateBackAudio);
			}
		}

		private void ScreenNavigator_ScreenNavigated(ScreenNavigator sender, ScreenBase screen)
		{
			if (ScreenNavigator.NavigationStack.Count > 1 && GameController.Instance.PlayButtonSounds && NavigateForwardAudio != null)
			{
				AudioHelper.PlaySound(NavigateForwardAudio);
			}
		}

		private IEnumerator LoadPausedSceneCoroutine()
		{
			ScreenNavigationRequest<PauseMenuUI> request = new ScreenNavigationRequest<PauseMenuUI>
			{
				LoadOnly = true
			};
			ScreenNavigator.LoadAndNavigateToScreen(request);
			yield return 0;
			request.ScreenResult.SetActive(enabled: false);
			hasLoaded = true;
		}

		private IEnumerator LoadMessageBoxSceneCoroutine()
		{
			ScreenNavigationRequest<MessageBoxScreen> request = new ScreenNavigationRequest<MessageBoxScreen>
			{
				LoadOnly = true
			};
			ScreenNavigator.LoadAndNavigateToScreen(request);
			yield return 0;
			MessageBoxScreen = request.ResultConcrete;
			if (MessageBoxScreen != null)
			{
				messageBoxScreen.SetActive(enabled: false);
			}
			else
			{
				LogMessageBoxScreenLoadFailure();
			}
		}

		private void LogMessageBoxScreenLoadFailure()
		{
			Debug.LogError("Failed to load message box screen", this);
		}

		public void ShowInsufficientCreditsMessageBox()
		{
			ShowMessageBox("Insufficient Credits", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
		}

		public void ShowMessageBox(string message)
		{
			ShowMessageBox(message, MessageBoxButtons.Ok);
		}

		public void MessageBoxDismiss()
		{
			messageBoxScreen.NavigateBack();
		}

		public void ShowError(string message)
		{
			ShowMessageBox(message, MessageBoxButtons.Ok, null, MessageBoxIcon.Error);
		}

		public void ShowMessageBox(string message, MessageBoxButtons buttons, MessageBoxScreen.MessageBoxDismissedHandler callback = null, MessageBoxIcon icon = MessageBoxIcon.Info, string title = null, TextAlignmentOptions textAlignmentOptions = TextAlignmentOptions.Center)
		{
			MessageBoxScreen.ShowMessageArgs args = new MessageBoxScreen.ShowMessageArgs
			{
				Text = message,
				Buttons = buttons,
				Title = title,
				Icon = icon,
				TextAlignment = textAlignmentOptions
			};
			if (messageBoxScreen != null)
			{
				messageBoxScreen.Show(args, callback);
				return;
			}
			ScreenNavigationRequest<MessageBoxScreen> screenNavigationRequest = new ScreenNavigationRequest<MessageBoxScreen>
			{
				LoadOnly = true
			};
			ScreenNavigator.LoadAndNavigateToScreen(screenNavigationRequest);
			MessageBoxScreen = screenNavigationRequest.ResultConcrete;
			if (MessageBoxScreen != null)
			{
				MessageBoxScreen.Show(args, callback);
			}
			else
			{
				LogMessageBoxScreenLoadFailure();
			}
		}

		private void Update()
		{
			if (EngineASX.LoadedAndReady)
			{
				if (ScreenNavigator.RealTimeSinceLastScreenChange > 0.05f)
				{
					DockUIHeader.gameObject.SetActive(ScreenNavigator.CurrentScreen is EngineScreen engineScreen && engineScreen.ShouldShowDockHeader());
				}
			}
			else
			{
				DockUIHeader.gameObject.SetActive(value: false);
			}
		}
	}
}
