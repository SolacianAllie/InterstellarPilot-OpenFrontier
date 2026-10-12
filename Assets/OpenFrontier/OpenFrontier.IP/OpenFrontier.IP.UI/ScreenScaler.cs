using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ScreenScaler : MonoBehaviour
	{
		[SerializeField]
		private ScreenNavigator screenNavigator;

		public ScreenNavigator ScreenNavigator
		{
			get
			{
				return screenNavigator;
			}
			set
			{
				if (screenNavigator != value)
				{
					ScreenNavigator oldScreenNavigator = screenNavigator;
					screenNavigator = value;
					OnScreenNavigatorChanged(oldScreenNavigator, screenNavigator);
				}
			}
		}

		private void Awake()
		{
			if (ScreenNavigator != null)
			{
				OnScreenNavigatorChanged(null, screenNavigator);
			}
		}

		private void Start()
		{
			if (ScreenNavigator == null)
			{
				ScreenNavigator = ScreenNavigator.Instance;
			}
			lastDisplay = Snapshot();
		}

		// Open Frontier: foldable/display-swap support. Unity does not
		// raise an event when the device display resizes (fold/unfold),
		// and on some devices even Screen.width/height go stale. Poll the
		// physical display signals; once they settle, re-sync the surface
		// with the same Screen.SetResolution nudge the video options use,
		// then re-apply UI scaling.
		private struct DisplaySnapshot
		{
			public int screenW, screenH, systemW, systemH, jniW, jniH;
			public Rect safeArea;
			public float dpi;

			public bool DiffersFrom(in DisplaySnapshot o)
			{
				return screenW != o.screenW || screenH != o.screenH
					|| systemW != o.systemW || systemH != o.systemH
					|| jniW != o.jniW || jniH != o.jniH
					|| safeArea != o.safeArea
					|| Mathf.Abs(dpi - o.dpi) > 1f;
			}
		}

		private DisplaySnapshot lastDisplay;
		private bool settlePollRunning;

		private static float jniPollTimer;
		private static Vector2Int jniSize;

		private static DisplaySnapshot Snapshot()
		{
			// JNI queries cost a little; throttle to ~4 Hz. Fold
			// transitions take much longer, so nothing is missed.
			if (Time.unscaledTime >= jniPollTimer)
			{
				jniPollTimer = Time.unscaledTime + 0.25f;
				jniSize = OpenFrontier.AndroidDisplayMetrics.GetWindowSize();
			}
			return new DisplaySnapshot
			{
				screenW = Screen.width,
				screenH = Screen.height,
				systemW = Display.main.systemWidth,
				systemH = Display.main.systemHeight,
				jniW = jniSize.x,
				jniH = jniSize.y,
				safeArea = Screen.safeArea,
				dpi = Screen.dpi
			};
		}

		private void Update()
		{
			DisplaySnapshot cur = Snapshot();
			if (!settlePollRunning && cur.DiffersFrom(lastDisplay))
			{
				StartCoroutine(ResyncWhenSettled());
			}
		}

		private System.Collections.IEnumerator ResyncWhenSettled()
		{
			settlePollRunning = true;
			DisplaySnapshot last = Snapshot();
			int stableFrames = 0;
			while (stableFrames < 5)
			{
				yield return null;
				DisplaySnapshot cur = Snapshot();
				if (cur.DiffersFrom(last))
				{
					last = cur;
					stableFrames = 0;
				}
				else
				{
					stableFrames++;
				}
			}
			lastDisplay = last;
			OpenFrontier.DevHud.DisplayResyncCount++;
			// Open Frontier: on foldables the OS swaps the physical display
			// (Display.main updates) but does not resize the app window, so
			// the window is stretched onto the new panel and every Unity
			// Screen metric goes stale. Force the window to the new physical
			// size - the same fix as manually re-applying the video options.
			if (Display.main.systemWidth > 0 && Display.main.systemHeight > 0
				&& (Display.main.systemWidth != Screen.width || Display.main.systemHeight != Screen.height))
			{
				Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, Screen.fullScreenMode);
			}
			else if (GameController.Instance != null)
			{
				GameController.Instance.ApplyResolution(Screen.fullScreenMode);
			}
			ScaleScreens();
			Canvas.ForceUpdateCanvases();
			// Let the new display mode apply, then re-apply frame pacing
			// (vsync-aware) for the new panel.
			yield return null;
			yield return null;
			if (GameController.Instance != null)
			{
				GameController.Instance.ApplyMobileFrameRate();
			}
			settlePollRunning = false;
		}

		public void ScaleScreens()
		{
			if ((bool)ScreenNavigator)
			{
				ScaleScreens(GameController.Instance.InterfaceReferenceHeight);
			}
		}

		public void ScaleScreens(float height)
		{
			ScaleScreen(UIController.Instance.HomeCanvas.GetComponent<CanvasScaler>(), height);
			foreach (ScreenBase loadedScreen in ScreenNavigator.LoadedScreens)
			{
				if (loadedScreen.CanvasScalers != null && loadedScreen.CanvasScalers.Count > 0)
				{
					ScaleScreen(loadedScreen.CanvasScalers, height);
				}
				else
				{
					ScaleScreen(loadedScreen.gameObject, height);
				}
			}
		}

		public void ScaleScreen(GameObject gameObject)
		{
			float interfaceReferenceHeight = GameController.Instance.InterfaceReferenceHeight;
			ScaleScreen(gameObject, interfaceReferenceHeight);
		}

		public void ScaleScreen(GameObject gameObject, float height)
		{
			ScaleScreen(gameObject.GetComponentsInChildren<CanvasScaler>(includeInactive: true), height);
		}

		public void ScaleScreen(IEnumerable<CanvasScaler> canvasScalers, float height)
		{
			foreach (CanvasScaler canvasScaler in canvasScalers)
			{
				ScaleScreen(canvasScaler, height);
			}
		}

		public void ScaleScreen(CanvasScaler canvasScaler, float height)
		{
			Vector2 referenceResolution = canvasScaler.referenceResolution;
			referenceResolution.y = height;
			canvasScaler.referenceResolution = referenceResolution;
		}

		private void OnScreenNavigatorChanged(ScreenNavigator oldScreenNavigator, ScreenNavigator newScreenNavigator)
		{
			if (oldScreenNavigator != null)
			{
				oldScreenNavigator.ScreenRegistered -= ScreenNavigator_ScreenRegistered;
			}
			if (screenNavigator != null)
			{
				screenNavigator.ScreenRegistered += ScreenNavigator_ScreenRegistered;
				ScaleScreens();
			}
		}

		private void ScreenNavigator_ScreenRegistered(ScreenNavigator sender, ScreenBase newScreen)
		{
			ScaleScreen(newScreen.gameObject, GameController.Instance.InterfaceReferenceHeight);
		}
	}
}
