using TMPro;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public static class ToggleExtensions
	{
		public static void SetText(this Toggle toggle, string text)
		{
			TextMeshProUGUI componentInChildren = toggle.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.text = text;
				return;
			}
			Text componentInChildren2 = toggle.GetComponentInChildren<Text>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.text = text;
			}
		}
	}
}
