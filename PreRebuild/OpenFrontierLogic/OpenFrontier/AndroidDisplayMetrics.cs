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

		/// <summary>
		/// Why the most recent SetPreferredRefreshRate could not reach the
		/// requested rate exactly, or "" when it could. Read by the dev overlay.
		/// Empty on non-Android platforms, which never take this path.
		/// </summary>
		public static string LastRefreshRateIssue = "";

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

		// Switch the display MODE to the rate that best matches targetHz at the
		// current resolution, via the window's preferredDisplayModeId. Needed
		// because Screen.resolutions does not expose high-refresh modes on some
		// devices.
		//
		// Open Frontier: the previous version took "smallest mode >= target" and,
		// failing that, silently fell through to the HIGHEST mode. On a 90Hz
		// panel with no 60Hz mode, asking for a 60 cap picked 90Hz - and 90/60 is
		// 1.5, a swap interval Vulkan's present modes cannot express. The cap was
		// then ignored and the game ran at 90. Mode choice now prefers a rate the
		// target divides evenly, and reports when the panel cannot supply one.
		//
		// Returns a short status line for the dev overlay, or "" when there is
		// nothing worth reporting. Never throws.
		public static string SetPreferredRefreshRate(float targetHz)
		{
			LastRefreshRateIssue = ApplyPreferredRefreshRate(targetHz);
			return LastRefreshRateIssue;
		}

		private static string ApplyPreferredRefreshRate(float targetHz)
		{
			try
			{
				using (AndroidJavaObject activity = CurrentActivity())
				using (AndroidJavaObject display = CurrentDisplay(activity))
				{
					AndroidJavaObject[] modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
					int curW = display.Call<int>("getPhysicalWidth");
					int curH = display.Call<int>("getPhysicalHeight");
					int divisorId = -1;
					float divisorHz = -1f;
					int smallestId = -1;
					float smallestHz = -1f;
					int highestId = -1;
					float highestHz = -1f;
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
							if (hz >= targetHz - 0.5f && (smallestId < 0 || hz < smallestHz))
							{
								smallestHz = hz;
								smallestId = id;
							}
							// A rate the target divides evenly can be held with a
							// whole swap interval. Those are the only ones that
							// actually keep a cap under Vulkan.
							if (hz >= targetHz - 0.5f && DividesEvenly(hz, targetHz)
								&& (divisorId < 0 || hz < divisorHz))
							{
								divisorHz = hz;
								divisorId = id;
							}
						}
						mode.Dispose();
					}
					int chosenId = divisorId;
					float chosenHz = divisorHz;
					if (chosenId < 0)
					{
						chosenId = smallestId;
						chosenHz = smallestHz;
					}
					bool fellBackToHighest = false;
					if (chosenId < 0)
					{
						chosenId = highestId;
						chosenHz = highestHz;
						fellBackToHighest = true;
					}
					if (chosenId < 0)
					{
						return "no display mode available";
					}

					using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
					using (AndroidJavaObject lp = window.Call<AndroidJavaObject>("getAttributes"))
					{
						lp.Set("preferredDisplayModeId", chosenId);
						window.Call("setAttributes", lp);
					}

					if (divisorId >= 0)
					{
						// Requested rate is exactly reachable. Nothing to report.
						return "";
					}
					// No even divisor available. Say so - this is the case where the
					// cap will not hold, and the overlay alone cannot explain it.
					string why = fellBackToHighest
						? "no mode at this resolution"
						: "no rate divides " + targetHz.ToString("0") + " evenly";
					return chosenHz.ToString("0") + "Hz, " + why;
				}
			}
			catch (System.Exception)
			{
				return "refresh rate request failed";
			}
		}

		/// <summary>
		/// True when hz is an integer multiple of targetHz within tolerance.
		/// Both arrive from Android as floats, hence the tolerance.
		/// </summary>
		private static bool DividesEvenly(float hz, float targetHz)
		{
			if (targetHz <= 0f)
			{
				return false;
			}
			float ratio = hz / targetHz;
			return System.Math.Abs(ratio - System.Math.Round(ratio)) < 0.02f;
		}
#endif
	}
}
