using System;
using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Extensions
{
	public static class ButtonExtensions
	{
		[Obsolete("Use toggle button instead")]
		public static bool GetButtonActivated(this Button button)
		{
			return false;
		}

		[Obsolete("Use toggle button instead")]
		public static void SetButtonActivated(this Button button, bool activated)
		{
		}

		public static void SetText(this Button button, string text)
		{
			TextMeshProUGUI componentInChildren = button.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.text = text;
				return;
			}
			Text componentInChildren2 = button.GetComponentInChildren<Text>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.text = text;
			}
		}

		public static string GetText(this Button button)
		{
			TextMeshProUGUI componentInChildren = button.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				return componentInChildren.text;
			}
			Text componentInChildren2 = button.GetComponentInChildren<Text>();
			if (componentInChildren2 != null)
			{
				return componentInChildren2.text;
			}
			return string.Empty;
		}
	}
}
