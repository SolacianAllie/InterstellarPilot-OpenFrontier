using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// Development overlay showing live screen metrics, for foldable /
	/// display-resize debugging. Active in development builds and the
	/// editor only; release builds are unaffected.
	/// </summary>
	public class DevHud : MonoBehaviour
	{
		private static DevHud instance;

		// Incremented by the UI layer whenever it re-syncs the display
		// (fold/unfold); shown here so the behavior can be verified.
		public static int DisplayResyncCount;

		private float fpsAccum;
		private int fpsFrames;
		private float currentFps;

		private void Update()
		{
			fpsAccum += Time.unscaledDeltaTime;
			fpsFrames++;
			if (fpsAccum >= 0.5f)
			{
				currentFps = fpsFrames / fpsAccum;
				fpsAccum = 0f;
				fpsFrames = 0;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Create()
		{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			if (instance == null)
			{
				GameObject go = new GameObject("DevHud");
				instance = go.AddComponent<DevHud>();
				Object.DontDestroyOnLoad(go);
			}
#endif
		}

		private string cachedText = "";
		private float nextTextAt;
		private static string cachedAndroidModes;
		private static GUIStyle hudStyle;

		private void OnGUI()
		{
			// Repaint only - see MusicDebugOverlay.OnGUI for why. Nothing here
			// uses GUILayout, so skipping the Layout event skips work whose
			// result is discarded.
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			// The text is rebuilt four times a second rather than sixty. A
			// frame counter moving in tenths does not need a new string
			// every frame, and building it was one allocation per OnGUI call.
			if (Time.realtimeSinceStartup >= nextTextAt)
			{
				nextTextAt = Time.realtimeSinceStartup + 0.25f;
				cachedText = BuildText();
			}

			Matrix4x4 old = GUI.matrix;
			GUI.matrix = scaledMatrix;

			GUI.Box(new Rect(8f, 8f, 340f, 66f), cachedText, HudStyle);

			GUI.matrix = old;
		}

		private static Matrix4x4 scaledMatrix =>
			Matrix4x4.Scale(new Vector3(DevOverlayScale.Value, DevOverlayScale.Value, 1f));

		/// <summary>Box style with word wrap off, so IMGUI never re-wraps.</summary>
		private static GUIStyle HudStyle
		{
			get
			{
				if (hudStyle == null)
				{
					hudStyle = new GUIStyle(GUI.skin.box)
					{
						wordWrap = false,
						richText = false,
						alignment = TextAnchor.UpperLeft
					};
				}

				return hudStyle;
			}
		}

		private string BuildText()
		{
			// Shared with the music overlay below this one, so the two read as
			// a single panel. See DevOverlayScale for why the old floor of
			// 2x is gone - it doubled the text on every desktop.
			string hz = "";
#if UNITY_ANDROID && !UNITY_EDITOR
			// ONCE, not per OnGUI call. This was a PlayerPrefs read and a
			// display-mode enumeration TWICE A FRAME, on a phone, inside the
			// render loop - neither of which changes between refresh rates,
			// and both of which cross into the JNI. A dev overlay that can
			// make a phone stutter is worse than no overlay.
			if (cachedAndroidModes == null)
			{
				int vsPref = UnityEngine.PlayerPrefs.GetInt("video_vsync", 1);
				cachedAndroidModes =
					"\nModes " + AndroidDisplayMetrics.GetSupportedModesSummary()
					+ "  now " + AndroidDisplayMetrics.GetCurrentRefreshRate().ToString("0")
					+ "Hz  vsyncPref " + (vsPref > 0 ? "on" : "off");
			}

			hz = cachedAndroidModes;
#endif
			return "FPS " + currentFps.ToString("0")
				+ "  (cap " + Application.targetFrameRate
				+ ", vSync " + QualitySettings.vSyncCount + ")" + hz;
		}
	}
}
