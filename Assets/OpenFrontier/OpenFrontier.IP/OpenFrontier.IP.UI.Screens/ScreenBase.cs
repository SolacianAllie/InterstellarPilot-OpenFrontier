using System.Collections.Generic;
using System.Text;
using OpenFrontier.IP.UI.Screens.HelpSystem;
using OpenFrontier.IP.UI.Screens.MessageBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI.Screens
{
	public class ScreenBase : MonoBehaviour
	{
		public delegate void NavigatingBackHandler(ScreenBase sender, ref bool cancel);

		[SerializeField]
		private bool restrictNavigationAway;

		public Button HelpButton;

		public Transform CustomHelpTransform;

		public ScreenDeactivateType DeactivateType;

		public bool IgnoreCanvasGroupAlpha;

		public float FadeRateMultiplier = 1f;

		public bool ShowDefaultBackButton = true;

		public bool KeepInNavigationStack = true;

		public bool FadeIn = true;

		public bool FadeOut = true;

		public List<CanvasGroup> CanvasGroups;

		private float canvasGroupAlpha = 1f;

		private float? fadeStartTime = 0f;

		private ScreenFadeState fadingState;

		public bool KeepInMemory;

		[SerializeField]
		private bool keepOtherScreensActive;

		public bool RefreshOnEnable = true;

		public bool RefreshOnStart = true;

		private float timeLastEnabled;

		public List<Canvas> Canvases = new List<Canvas>();

		private bool isActive;

		public bool UpdateWhenNotCurrentScreen;

		private int navigationStackIndex = -1;

		public bool PauseWhenShown;

		internal float? previousTimeScale;

		internal float timeScaleWhenMadeCurrent = -1f;

		public bool AllowTogglePause;

		private List<CanvasScaler> canvasScalers;

		private bool hasInit;

		public Button AcceptButton;

		public int NavigationStackIndex
		{
			get
			{
				return navigationStackIndex;
			}
			internal set
			{
				navigationStackIndex = value;
			}
		}

		public bool RestrictNavigationAwayEnabled
		{
			get
			{
				return restrictNavigationAway;
			}
			set
			{
				restrictNavigationAway = value;
			}
		}

		public float CanvasGroupAlpha
		{
			get
			{
				return canvasGroupAlpha;
			}
			set
			{
				float num = Mathf.Clamp01(value);
				if (num != canvasGroupAlpha)
				{
					canvasGroupAlpha = num;
					SetCanvasGroupsAlpha(canvasGroupAlpha);
				}
			}
		}

		public float TimeSinceLastEnabled => Time.time - timeLastEnabled;

		public bool IsCurrentScreen
		{
			get
			{
				if (ScreenNavigator.Instance != null)
				{
					return ScreenNavigator.Instance.CurrentScreen == this;
				}
				return false;
			}
		}

		public ScreenFadeState FadingState
		{
			get
			{
				return fadingState;
			}
			private set
			{
				if (fadingState != value)
				{
					fadingState = value;
					switch (fadingState)
					{
					case ScreenFadeState.FadeIn:
						ResetFadeStartTime();
						break;
					case ScreenFadeState.FadeOut:
						ResetFadeStartTime();
						break;
					}
				}
			}
		}

		public bool KeepOtherScreensActive
		{
			get
			{
				return keepOtherScreensActive;
			}
			set
			{
				keepOtherScreensActive = value;
			}
		}

		public List<CanvasScaler> CanvasScalers => canvasScalers;

		public event NavigatingBackHandler NavigatingBack;

		public ScreenBase NextInStack()
		{
			if (navigationStackIndex < ScreenNavigator.Instance.NavigationStack.Count - 1)
			{
				return ScreenNavigator.Instance.NavigationStack[navigationStackIndex + 1];
			}
			return null;
		}

		private void RestoreAlpha()
		{
			CanvasGroupAlpha = 1f;
		}

		private void ResetFadeStartTime()
		{
			fadeStartTime = null;
		}

		public virtual ScreenBase GetBackTarget()
		{
			return ScreenNavigator.Instance.GetBackTargetScreen(this);
		}

		public void Awake()
		{
			if (hasInit)
			{
				return;
			}
			hasInit = true;
			if (Canvases != null && Canvases.Count > 0)
			{
				canvasScalers = new List<CanvasScaler>(4);
				foreach (Canvas canvase in Canvases)
				{
					CanvasScaler component = canvase.GetComponent<CanvasScaler>();
					if (component != null)
					{
						canvasScalers.Add(component);
					}
				}
			}
			else
			{
				Debug.LogError(gameObject.name + ": For performance it is recommended to assign the Canvases collection to avoid having to scan for these", this);
			}
			if (HelpButton != null)
			{
				HelpButton.onClick.AddListener(HelpButtonClick);
			}
			GetCanvasGroups();
			canvasGroupAlpha = 0f;
			SetCanvasGroupsAlpha(canvasGroupAlpha);
			awake();
			if (ScreenNavigator.Instance != null)
			{
				ScreenNavigator.Instance.RegisterScreen(this);
			}
		}

		private void HelpButtonClick()
		{
			ShowHelp();
		}

		public void ShowHelp(Transform targetTransform = null, bool showScreenPrimaryHelp = true)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (targetTransform == null)
			{
				targetTransform = transform;
			}
			string title = null;
			HelpResourceItem helpResourceItem = null;
			if (showScreenPrimaryHelp)
			{
				helpResourceItem = GetComponent<HelpResourceItem>();
				if (helpResourceItem != null && helpResourceItem.HelpResource != null)
				{
					title = helpResourceItem.HelpResource.Title;
					stringBuilder.AppendLine(helpResourceItem.HelpResource.Description);
					stringBuilder.AppendLine();
				}
			}
			List<HelpResourceItem> items = new List<HelpResourceItem>(20);
			CompileHelpResources(targetTransform, stringBuilder, helpResourceItem, items);
			if (CustomHelpTransform != null)
			{
				CompileHelpResources(CustomHelpTransform, stringBuilder, helpResourceItem, items);
			}
			if (stringBuilder.Length > 0)
			{
				UIController.Instance.ShowMessageBox(stringBuilder.ToString(), MessageBoxButtons.Ok, null, MessageBoxIcon.Info, title, TextAlignmentOptions.Left);
			}
		}

		private static void CompileHelpResources(Transform targetTransform, StringBuilder stringBuilder, HelpResourceItem primaryHelpResourceItem, List<HelpResourceItem> items)
		{
			HelpResourceItem[] componentsInChildren = targetTransform.GetComponentsInChildren<HelpResourceItem>(includeInactive: true);
			foreach (HelpResourceItem helpResourceItem in componentsInChildren)
			{
				HelpResource helpResource = helpResourceItem.HelpResource;
				if (helpResource == null || helpResourceItem == primaryHelpResourceItem || items.Contains(helpResourceItem))
				{
					continue;
				}
				items.Add(helpResourceItem);
				if (!string.IsNullOrWhiteSpace(helpResource.Description))
				{
					if (!string.IsNullOrWhiteSpace(helpResource.Title))
					{
						stringBuilder.Append("<b>");
						stringBuilder.Append(helpResource.Title);
						stringBuilder.Append("</b>");
					}
					stringBuilder.AppendLine();
					stringBuilder.AppendLine(helpResource.Description);
					stringBuilder.AppendLine();
				}
			}
		}

		public void Start()
		{
			start();
			if (ScreenNavigator.Instance != null)
			{
				ScreenNavigator.Instance.RegisterScreen(this);
				if (ScreenNavigator.Instance.CurrentScreen == null)
				{
					ScreenNavigator.Instance.NavigateToRootScreen(this);
				}
			}
			else
			{
				Debug.LogWarning("Missing UIControllerMain instance", this);
			}
			if (RefreshOnStart)
			{
				Refresh();
			}
		}

		public void Update()
		{
			if (!(ScreenNavigator.Instance != null))
			{
				return;
			}
			if (IsInNavigationStack())
			{
				if ((IsCurrentScreen || UpdateWhenNotCurrentScreen) && fadingState != ScreenFadeState.FadeOut)
				{
					update();
					if (fadingState == ScreenFadeState.None)
					{
						NavigateBackWithEscapeKey();
					}
				}
				if (IsCurrentScreen && AcceptButton != null && Input.GetKeyDown(KeyCode.Return))
				{
					AcceptButton.onClick.Invoke();
				}
			}
			UpdateFading();
		}

		public void LateUpdate()
		{
			if (ShouldUpdate())
			{
				lateUpdate();
			}
		}

		public bool ShouldUpdate()
		{
			if (ScreenNavigator.Instance != null && IsInNavigationStack() && (IsCurrentScreen || UpdateWhenNotCurrentScreen) && fadingState != ScreenFadeState.FadeOut)
			{
				return true;
			}
			return false;
		}

		protected virtual void lateUpdate()
		{
		}

		[ContextMenu("Refresh")]
		public void Refresh()
		{
			refresh();
		}

		[ContextMenu("Navigate Back")]
		public void NavigateBack()
		{
			if (IsCurrentScreen)
			{
				ScreenNavigator.Instance.TryNavigateBack();
				return;
			}
			Debug.LogErrorFormat(this, "{0}: Only current panel {1} can navigate back", this, ScreenNavigator.Instance.CurrentScreen);
		}

		public bool OnNavigatingBack()
		{
			return onNavigatingBack();
		}

		public void OnMadeCurrentScreen(bool navigatedForward)
		{
			if (FadeIn)
			{
				FadingState = ScreenFadeState.FadeIn;
			}
			else
			{
				RestoreAlpha();
			}
			SetActive(enabled: true);
			onMadeCurrentPanel(navigatedForward);
		}

		public void SetActive(bool enabled)
		{
			if (isActive != enabled)
			{
				isActive = enabled;
				if (!isActive)
				{
					onDisable();
				}
				else
				{
					OnScreenEnabled();
				}
			}
			switch (DeactivateType)
			{
			case ScreenDeactivateType.Disable:
			{
				if (Canvases.Count > 0)
				{
					foreach (Canvas canvase in Canvases)
					{
						canvase.enabled = enabled;
					}
					break;
				}
				Canvas[] componentsInChildren = GetComponentsInChildren<Canvas>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = enabled;
				}
				break;
			}
			case ScreenDeactivateType.SetActive:
				gameObject.SetActive(enabled);
				break;
			}
		}

		public void OnRemovedFromNavigationStack()
		{
			TryStartFadeOut();
		}

		private void TryStartFadeOut()
		{
			if (FadeOut)
			{
				FadingState = ScreenFadeState.FadeOut;
			}
			else
			{
				OnFadedOut();
			}
		}

		public void OnNotCurrentScreen()
		{
			onNotCurrentPanel();
			if (IsInNavigationStack() && !KeepInNavigationStack)
			{
				ScreenNavigator.Instance.RemoveFromNavigationStack(this);
			}
			if (ScreenNavigator.Instance.CurrentScreen != null && !ScreenNavigator.Instance.CurrentScreen.KeepOtherScreensActive)
			{
				TryStartFadeOut();
			}
		}

		private void OnFadedOut()
		{
			DestroyOrDeactivate();
		}

		internal void DestroyOrDeactivate()
		{
			if (!IsInNavigationStack() && !KeepInMemory)
			{
				SetActive(enabled: false);
				SafeDestroy();
			}
			else if (!IsCurrentScreen)
			{
				SetActive(enabled: false);
			}
		}

		public virtual bool ForceShowBackButton()
		{
			return false;
		}

		public bool IsInNavigationStack()
		{
			return navigationStackIndex > -1;
		}

		protected virtual void onDestroy()
		{
		}

		protected virtual void update()
		{
		}

		protected virtual void onDisable()
		{
			if (FadeIn)
			{
				CanvasGroupAlpha = 0f;
			}
		}

		protected virtual void onEnable()
		{
		}

		protected virtual void awake()
		{
		}

		protected virtual void start()
		{
		}

		protected virtual void refresh()
		{
		}

		protected virtual bool onNavigatingBack()
		{
			bool cancel = false;
			if (NavigatingBack != null)
			{
				NavigatingBack(this, ref cancel);
			}
			return !cancel;
		}

		protected virtual void onNotCurrentPanel()
		{
		}

		protected virtual void onMadeCurrentPanel(bool navigatedForward)
		{
		}

		protected virtual bool AllowNavigateBack()
		{
			return true;
		}

		protected virtual bool ShouldFadeIn()
		{
			return true;
		}

		private void GetCanvasGroups()
		{
			if (CanvasGroups.Count != 0)
			{
				return;
			}
			foreach (Canvas canvase in Canvases)
			{
				CanvasGroup component = canvase.GetComponent<CanvasGroup>();
				if (component != null)
				{
					CanvasGroups.Add(component);
				}
			}
		}

		private void OnScreenEnabled()
		{
			if (FadeIn)
			{
				FadingState = ScreenFadeState.FadeIn;
			}
			else
			{
				FadingState = ScreenFadeState.None;
			}
			timeLastEnabled = Time.time;
			onEnable();
			if (RefreshOnEnable)
			{
				Refresh();
			}
		}

		public void SafeDestroy()
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Screen {this} SafeDestroy", this, 3);
			}
			if (ScreenNavigator.Instance != null)
			{
				ScreenNavigator.Instance.DeregisterScreen(this);
			}
			navigationStackIndex = -1;
			Object.Destroy(gameObject);
		}

		private void OnDestroy()
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Screen {this} OnDestroy", this, 3);
			}
			if (ScreenNavigator.Instance != null)
			{
				ScreenNavigator.Instance.DeregisterScreen(this);
			}
			navigationStackIndex = -1;
			onDestroy();
		}

		private void SetCanvasGroupsAlpha(float alpha)
		{
			if (IgnoreCanvasGroupAlpha)
			{
				return;
			}
			foreach (CanvasGroup canvasGroup in CanvasGroups)
			{
				canvasGroup.alpha = alpha;
			}
		}

		private void UpdateFading()
		{
			if (fadingState != ScreenFadeState.None)
			{
				ApplyFadingState();
			}
		}

		protected virtual void ApplyFadingState()
		{
			if (fadeStartTime.HasValue)
			{
				if (!(RealTime.time > fadeStartTime.Value))
				{
					return;
				}
				switch (fadingState)
				{
				case ScreenFadeState.FadeIn:
					CanvasGroupAlpha += GetFadeChangeValue();
					if (canvasGroupAlpha >= 1f)
					{
						FadingState = ScreenFadeState.None;
					}
					break;
				case ScreenFadeState.FadeOut:
					CanvasGroupAlpha -= GetFadeChangeValue();
					if (canvasGroupAlpha <= 0f)
					{
						FadingState = ScreenFadeState.None;
						OnFadedOut();
					}
					break;
				}
			}
			else
			{
				fadeStartTime = RealTime.time;
			}
		}

		private float GetFadeChangeValue()
		{
			return ScreenNavigator.Instance.ScreenAlphaChangeRate * FadeRateMultiplier * RealTime.deltaTime;
		}

		private void NavigateBackWithEscapeKey()
		{
			if (IsCurrentScreen && fadingState == ScreenFadeState.None && AllowNavigateBack() && Input.GetKeyDown(KeyCode.Escape))
			{
				OnNavigateBackFromEscapeKey();
			}
		}

		protected virtual void OnNavigateBackFromEscapeKey()
		{
			NavigateBack();
		}

		public void SetCanvasGroupsInteractable(bool interactable)
		{
			foreach (CanvasGroup canvasGroup in CanvasGroups)
			{
				canvasGroup.interactable = interactable;
			}
		}

		public virtual bool RequestNavigationAwayTo<T>() where T : ScreenBase
		{
			if (RestrictNavigationAwayEnabled)
			{
				return false;
			}
			return true;
		}

		public bool ShouldAllowTogglePause()
		{
			return AllowTogglePause;
		}
	}
}
