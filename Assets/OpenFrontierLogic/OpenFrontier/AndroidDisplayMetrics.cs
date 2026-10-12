using UnityEngine;

namespace OpenFrontier
{
	/// <summary>
	/// Queries the real window size from Android's DisplayMetrics, bypassing
	/// UnityEngine.Screen, which can go stale on foldables when the display
	/// is swapped. On non-Android platforms it reports Screen.width/height.
	/// </summary>
	public static class AndroidDisplayMetrics
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		private static AndroidJavaClass unityPlayerClass;
#endif

		public static Vector2Int GetWindowSize()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				using (AndroidJavaObject activity = CurrentActivity())
				using (AndroidJavaObject resources = activity.Call<AndroidJavaObject>("getResources"))
				using (AndroidJavaObject metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
				{
					return new Vector2Int(metrics.Get<int>("widthPixels"), metrics.Get<int>("heightPixels"));
				}
			}
			catch (System.Exception)
			{
				return new Vector2Int(Screen.width, Screen.height);
			}
#else
			return new Vector2Int(Screen.width, Screen.height);
#endif
		}

		// Physical pixel density of the current display, from Android's
		// DisplayMetrics - Screen.dpi can return 0 on foldables, which
		// silently breaks DPI-scaled touch thresholds. Returns 0 when
		// unavailable.
		public static float GetDensityDpi()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				using (AndroidJavaObject activity = CurrentActivity())
				using (AndroidJavaObject resources = activity.Call<AndroidJavaObject>("getResources"))
				using (AndroidJavaObject metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
				{
					return metrics.Get<int>("densityDpi");
				}
			}
			catch (System.Exception)
			{
				return 0f;
			}
#else
			return Screen.dpi;
#endif
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		private static AndroidJavaObject CurrentActivity()
		{
			if (unityPlayerClass == null)
			{
				unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			}
			return unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
		}

		private static AndroidJavaObject CurrentDisplay(AndroidJavaObject activity)
		{
			AndroidJavaObject wm = activity.Call<AndroidJavaObject>("getWindowManager");
			return wm.Call<AndroidJavaObject>("getDefaultDisplay");
		}

		// The panel's currently active refresh rate (the REAL value, from
		// Android's Display API - Unity's Screen.currentResolution can lag).
		public static float GetCurrentRefreshRate()
		{
			try
			{
				using (AndroidJavaObject activity = CurrentActivity())
				using (AndroidJavaObject display = CurrentDisplay(activity))
				{
					return display.Call<float>("getRefreshRate");
				}
			}
			catch (System.Exception)
			{
				return 0f;
			}
		}

		// Distinct supported refresh rates of the active display, sorted.
		public static System.Collections.Generic.List<int> GetSupportedRates()
		{
			System.Collections.Generic.SortedSet<int> rates = new System.Collections.Generic.SortedSet<int>();
#if UNITY_ANDROID && !UNITY_EDITOR
			try
			{
				using (AndroidJavaObject activity = CurrentActivity())
				using (AndroidJavaObject display = CurrentDisplay(activity))
				{
					AndroidJavaObject[] modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
					foreach (AndroidJavaObject mode in modes)
					{
						rates.Add((int)(mode.Call<float>("getRefreshRate") + 0.5f));
						mode.Dispose();
					}
				}
			}
			catch (System.Exception)
			{
			}
#endif
			if (rates.Count == 0)
			{
				rates.Add(60);
			}
			return new System.Collections.Generic.List<int>(rates);
		}

		// Summary for the debug overlay: distinct supported rates + active rate.
		public static string GetSupportedModesSummary()
		{
			try
			{
				using (AndroidJavaObject activity = CurrentActivity())
				using (AndroidJavaObject display = CurrentDisplay(activity))
				{
					AndroidJavaObject[] modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
					System.Collections.Generic.SortedSet<int> rates = new System.Collections.Generic.SortedSet<int>();
					foreach (AndroidJavaObject mode in modes)
					{
						rates.Add((int)(mode.Call<float>("getRefreshRate") + 0.5f));
						mode.Dispose();
					}
					return string.Join("/", rates) + "Hz";
				}
			}
			catch (System.Exception)
			{
				return "?";
			}
		}

		// Switch the display MODE to the smallest supported rate >= targetHz
		// (or the highest available) at the current resolution, via the
		// window's preferredDisplayModeId. Needed because Screen.resolutions
		// does not expose high-refresh modes on some devices.
		public static void SetPreferredRefreshRate(float targetHz)
		{
			try
			{
				using (AndroidJavaObject activity = CurrentActivity())
				using (AndroidJavaObject display = CurrentDisplay(activity))
				{
					AndroidJavaObject[] modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
					int curW = display.Call<int>("getPhysicalWidth");
					int curH = display.Call<int>("getPhysicalHeight");
					int bestId = -1;
					float bestHz = -1f;
					float highestHz = -1f;
					int highestId = -1;
					foreach (AndroidJavaObject mode in modes)
					{
						float hz = mode.Call<float>("getRefreshRate");
						int w = mode.Call<int>("getPhysicalWidth");
						int h = mode.Call<int>("getPhysicalHeight");
						int id = mode.Call<int>("getModeId");
						if (w == curW && h == curH)
						{
							if (hz > highestHz)
							{
								highestHz = hz;
								highestId = id;
							}
							if (hz >= targetHz - 0.5f && (bestId < 0 || hz < bestHz))
							{
								bestHz = hz;
								bestId = id;
							}
						}
						mode.Dispose();
					}
					if (bestId < 0)
					{
						bestId = highestId;
					}
					if (bestId >= 0)
					{
						using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
						using (AndroidJavaObject lp = window.Call<AndroidJavaObject>("getAttributes"))
						{
							lp.Set("preferredDisplayModeId", bestId);
							window.Call("setAttributes", lp);
						}
					}
				}
			}
			catch (System.Exception)
			{
			}
		}
#endif
	}
}
