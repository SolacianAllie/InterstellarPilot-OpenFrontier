using DigitalRubyShared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenFrontier.IP.UI
{
	/// <summary>
	/// Open Frontier: FingersScript.Awake reads Screen.dpi and falls back to
	/// its DefaultDPI (200, with a red error) when it is 0 - which happens on
	/// Android foldables and on Linux - leaving every gesture threshold at
	/// the wrong physical size. This overrides DeviceInfo with the real DPI
	/// from ScreenDpi (JNI-backed) after Fingers wakes, and re-applies on
	/// scene loads (each FingersScript.Awake re-breaks it) and on resolution
	/// changes (fold swap changes the display density).
	/// </summary>
	public class FingersDpiFix : MonoBehaviour
	{
		private int oldScreenX = Screen.width;

		private int oldScreenY = Screen.height;

		private void OnEnable()
		{
			SceneManager.sceneLoaded += SceneLoaded;
		}

		private void OnDisable()
		{
			SceneManager.sceneLoaded -= SceneLoaded;
		}

		private void Start()
		{
			Apply();
		}

		private void Update()
		{
			if (Screen.width != oldScreenX || Screen.height != oldScreenY)
			{
				oldScreenX = Screen.width;
				oldScreenY = Screen.height;
				ScreenDpi.Invalidate();
				Apply();
			}
		}

		private void SceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Apply();
		}

		private void Apply()
		{
			if (Screen.dpi <= 0f)
			{
				DeviceInfo.PixelsPerInch = ScreenDpi.Value;
				DeviceInfo.UnitMultiplier = ScreenDpi.Value;
			}
		}
	}
}
