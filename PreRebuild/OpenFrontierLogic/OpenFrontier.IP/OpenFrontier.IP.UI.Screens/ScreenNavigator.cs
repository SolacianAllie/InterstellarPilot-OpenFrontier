using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens
{
	public class ScreenNavigator : MonoBehaviour
	{
		private Dictionary<Type, ScreenBase> screenNameMapping = new Dictionary<Type, ScreenBase>();

		public static ScreenNavigator Instance;

		public Button BackButton;

		private float lastScreenChangeRealTime;

		private List<ScreenBase> loadedScreens = new List<ScreenBase>();

		private List<ScreenBase> navigationStack = new List<ScreenBase>();

		public float ScreenAlphaChangeRate = 6f;

		public ScreenBase CurrentScreen
		{
			get
			{
				if (navigationStack.Count > 0)
				{
					return navigationStack[navigationStack.Count - 1];
				}
				return null;
			}
		}

		public List<ScreenBase> LoadedScreens => loadedScreens;

		public List<ScreenBase> NavigationStack => navigationStack;

		public float RealTimeSinceLastScreenChange => Time.realtimeSinceStartup - lastScreenChangeRealTime;

		public Dictionary<Type, ScreenBase> ScreenNameMapping => screenNameMapping;

		public ScreenBase RootScreen
		{
			get
			{
				if (navigationStack.Count > 0)
				{
					return navigationStack[0];
				}
				return null;
			}
		}

		public event ScreenNavigatedHandler ScreenNavigated;

		public event NavigatedBackHandler NavigatedBack;

		public event ScreenRegisteredHandler ScreenRegistered;

		private void AddToNavigationStack(ScreenBase screen)
		{
			if (screen == null)
			{
				throw new NullReferenceException("screen");
			}
			RegisterScreen(screen);
			ScreenBase currentScreen = CurrentScreen;
			navigationStack.Add(screen);
			screen.NavigationStackIndex = navigationStack.Count - 1;
			if (currentScreen != null)
			{
				currentScreen.OnNotCurrentScreen();
			}
			if (screen.PauseWhenShown && !screen.previousTimeScale.HasValue)
			{
				screen.previousTimeScale = Time.timeScale;
				Time.timeScale = 0f;
			}
			screen.timeScaleWhenMadeCurrent = Time.timeScale;
			screen.OnMadeCurrentScreen(navigatedForward: true);
			UpdateLastScreenChangeTime();
			RefreshBackButtonVisibility();
		}

		public void RemoveAllInStack(ScreenBase exclusion)
		{
			ScreenBase[] array = navigationStack.ToArray();
			ScreenBase currentScreen = CurrentScreen;
			if (exclusion != null)
			{
				ScreenBase screenBase = exclusion.NextInStack();
				if (screenBase != null && screenBase.previousTimeScale.HasValue)
				{
					Time.timeScale = screenBase.previousTimeScale.Value;
				}
			}
			for (int i = 0; i < navigationStack.Count; i++)
			{
				ScreenBase screenBase2 = navigationStack[i];
				if (screenBase2 != exclusion)
				{
					navigationStack.RemoveAt(i);
					i--;
					if (screenBase2 != null)
					{
						screenBase2.NavigationStackIndex = -1;
						screenBase2.DestroyOrDeactivate();
					}
				}
			}
			ReassignScreenNavigationStackIndices();
			if (currentScreen != CurrentScreen)
			{
				if (currentScreen != null)
				{
					currentScreen.OnNotCurrentScreen();
				}
				if (CurrentScreen != null)
				{
					if (CurrentScreen.PauseWhenShown)
					{
						if (!CurrentScreen.previousTimeScale.HasValue)
						{
							CurrentScreen.previousTimeScale = Time.timeScale;
						}
						Time.timeScale = 0f;
					}
					CurrentScreen.timeScaleWhenMadeCurrent = Time.timeScale;
					CurrentScreen.OnMadeCurrentScreen(navigatedForward: false);
				}
				UpdateLastScreenChangeTime();
			}
			ScreenBase[] array2 = array;
			foreach (ScreenBase screenBase3 in array2)
			{
				if (screenBase3 != exclusion)
				{
					screenBase3.previousTimeScale = null;
					screenBase3.OnRemovedFromNavigationStack();
				}
			}
			RefreshBackButtonVisibility();
		}

		private void ReassignScreenNavigationStackIndices()
		{
			for (int i = 0; i < navigationStack.Count; i++)
			{
				if (navigationStack[i] != null)
				{
					navigationStack[i].NavigationStackIndex = i;
				}
			}
		}

		internal void RemoveFromNavigationStack(ScreenBase screen)
		{
			int index = navigationStack.IndexOf(screen);
			RemoveFromNavigationStack(index);
		}

		private void RemoveFromNavigationStack(int index)
		{
			if (index < 0 || index > navigationStack.Count - 1)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			ScreenBase screenBase = navigationStack[index];
			bool isCurrentScreen = screenBase.IsCurrentScreen;
			if (screenBase.previousTimeScale.HasValue && index < navigationStack.Count - 1)
			{
				navigationStack[index + 1].previousTimeScale = screenBase.previousTimeScale;
				Time.timeScale = screenBase.previousTimeScale.Value;
			}
			else if (index == navigationStack.Count - 1 && screenBase.previousTimeScale.HasValue)
			{
				Time.timeScale = screenBase.previousTimeScale.Value;
				screenBase.previousTimeScale = null;
			}
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"UIControllerMain - Removing screen {screenBase}. Is current? {isCurrentScreen}: ", screenBase, 2);
			}
			navigationStack.RemoveAt(index);
			screenBase.NavigationStackIndex = -1;
			ReassignScreenNavigationStackIndices();
			if (isCurrentScreen)
			{
				screenBase.OnNotCurrentScreen();
				UpdateLastScreenChangeTime();
			}
			screenBase.previousTimeScale = null;
			screenBase.OnRemovedFromNavigationStack();
			if (CurrentScreen != null)
			{
				CurrentScreen.timeScaleWhenMadeCurrent = Time.timeScale;
				if (CurrentScreen.PauseWhenShown)
				{
					if (!CurrentScreen.previousTimeScale.HasValue)
					{
						CurrentScreen.previousTimeScale = Time.timeScale;
					}
					Time.timeScale = 0f;
				}
				CurrentScreen.OnMadeCurrentScreen(navigatedForward: false);
			}
			RefreshBackButtonVisibility();
		}

		private void RemoveTopFromNavigationStack()
		{
			RemoveFromNavigationStack(CurrentScreen);
		}

		private void UpdateLastScreenChangeTime()
		{
			lastScreenChangeRealTime = Time.realtimeSinceStartup;
		}

		public void NavigateToScreen(ScreenBase newScreen, bool navigateBackToIfInStack = true)
		{
			if (newScreen != null)
			{
				newScreen.gameObject.SetActive(value: true);
				if (!(newScreen != CurrentScreen))
				{
					return;
				}
				if (navigateBackToIfInStack && navigationStack.Contains(newScreen))
				{
					NavigateBackTo(newScreen);
					return;
				}
				AddToNavigationStack(newScreen);
				if (ScreenNavigated != null)
				{
					ScreenNavigated(this, newScreen);
				}
			}
			else
			{
				Debug.LogError("Navigate called with null argument", this);
			}
		}

		public void NavigateBackTo(ScreenBase screen)
		{
			int num = navigationStack.IndexOf(screen);
			if (num > -1)
			{
				int num2 = navigationStack.Count - 1 - num;
				for (int i = 0; i < num2; i++)
				{
					RemoveTopFromNavigationStack();
				}
				if (NavigatedBack != null)
				{
					NavigatedBack(this, screen);
				}
			}
			else
			{
				Debug.LogErrorFormat(this, "Cannot navigate to screen {0}. Has not been navigated to", screen);
			}
		}

		public void NavigateToRootScreen(ScreenBase screen)
		{
			RemoveAllInStack(screen);
			if (screen != null)
			{
				NavigateToScreen(screen);
			}
		}

		[ContextMenu("Navigate Back")]
		public void TryNavigateBackWithSounds()
		{
			TryNavigateBack();
		}

		public void TryNavigateBack()
		{
			if (CurrentScreen != null)
			{
				ScreenBase backTarget = CurrentScreen.GetBackTarget();
				if (backTarget != null)
				{
					TryNavigateBackTo(backTarget);
				}
			}
		}

		public void TryNavigateBackTo(ScreenBase backTarget)
		{
			if (CurrentScreen != null && CurrentScreen.OnNavigatingBack() && backTarget != null)
			{
				NavigateBackTo(backTarget);
			}
		}

		public ScreenBase GetBackTargetScreen(ScreenBase screen)
		{
			int num = navigationStack.IndexOf(screen);
			if (num > 0)
			{
				return navigationStack[num - 1];
			}
			return null;
		}

		public void NavigateToRootScene<T>() where T : ScreenBase
		{
			ScreenNavigationRequest<T> request = new ScreenNavigationRequest<T>();
			NavigateToRootScene(request);
		}

		public void NavigateToRootScene(IScreenNavigationRequest request)
		{
			ScreenBase loadedScreen = GetLoadedScreen(request.ScreenType);
			if (loadedScreen != null)
			{
				request.ScreenResult = loadedScreen;
				NavigateToRootScreen(loadedScreen);
			}
			else
			{
				RemoveAllInStack(null);
				LoadAndNavigateToScreen(request);
			}
		}

		public void NavigateToScreen<T>() where T : ScreenBase
		{
			ScreenNavigationRequest<T> request = new ScreenNavigationRequest<T>();
			NavigateToScreen(request);
		}

		public void NavigateToScreen(IScreenNavigationRequest request)
		{
			ScreenBase screenBase = null;
			switch (request.ReuseLoadedExistingScreen)
			{
			case ReuseScreenMode.Any:
				screenBase = GetLoadedScreen(request.ScreenType);
				break;
			case ReuseScreenMode.IfNotInNavigationStack:
				screenBase = GetLoadedScreen(request.ScreenType, excludeIfInNavigationStack: true);
				break;
			}
			if (screenBase != null)
			{
				request.ScreenResult = screenBase;
				NavigateToScreen(screenBase);
			}
			else
			{
				LoadAndNavigateToScreen(request);
			}
		}

		public ScreenBase LoadAndNavigateToScreen(IScreenNavigationRequest request)
		{
			ScreenBase screenPrefab = GetScreenPrefab(request.ScreenType);
			if (screenPrefab == null)
			{
				Debug.LogError("Could not find screen prefab for type of " + request.ScreenType.Name);
				return null;
			}
			ScreenBase screenBase = UnityEngine.Object.Instantiate(screenPrefab);
			screenBase.Awake();
			request.ScreenResult = screenBase;
			if (!request.LoadOnly)
			{
				NavigateToScreen(screenBase);
			}
			return screenBase;
		}

		public void RegisterScreen(ScreenBase screen)
		{
			if (!loadedScreens.Contains(screen))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log($"Registering screen {screen} \"{screen.gameObject.name}\"", screen, 3);
				}
				loadedScreens.Add(screen);
				if (ScreenRegistered != null)
				{
					ScreenRegistered(this, screen);
				}
			}
		}

		public void DeregisterScreen(ScreenBase screen)
		{
			if (loadedScreens.Contains(screen))
			{
				if (LogWrapper.LogMsgs)
				{
					LogWrapper.Log("UIControllerMain: Deregistering screen: " + screen, this, 3);
				}
				loadedScreens.Remove(screen);
				navigationStack.Remove(screen);
				screen.NavigationStackIndex = -1;
			}
		}

		public T GetLoadedScreen<T>() where T : ScreenBase
		{
			return (T)GetLoadedScreen(typeof(T));
		}

		public ScreenBase GetLoadedScreen(Type t, bool excludeIfInNavigationStack = false)
		{
			foreach (ScreenBase loadedScreen in loadedScreens)
			{
				if (loadedScreen != null && loadedScreen.GetType() == t && (!excludeIfInNavigationStack || !loadedScreen.IsInNavigationStack()))
				{
					return loadedScreen;
				}
			}
			return null;
		}

		public bool IsScreenOfTypeInNavigationStack<T>() where T : ScreenBase
		{
			foreach (ScreenBase item in navigationStack)
			{
				if (item != null && item.GetType() == typeof(T))
				{
					return true;
				}
			}
			return false;
		}

		private void Awake()
		{
			Instance = this;
			RefreshBackButtonVisibility();
		}

		private void OnDestroy()
		{
			Instance = null;
		}

		private void Update()
		{
			RefreshBackButtonVisibility();
		}

		private void RefreshBackButtonVisibility()
		{
			BackButton.gameObject.SetActive(CurrentScreen != null && navigationStack.Count > 1 && CurrentScreen.ShowDefaultBackButton && (CurrentScreen.GetBackTarget() != null || CurrentScreen.ForceShowBackButton()));
		}

		public ScreenBase GetScreenPrefab(Type t)
		{
			if (screenNameMapping.TryGetValue(t, out var value))
			{
				return value;
			}
			return null;
		}
	}
}
