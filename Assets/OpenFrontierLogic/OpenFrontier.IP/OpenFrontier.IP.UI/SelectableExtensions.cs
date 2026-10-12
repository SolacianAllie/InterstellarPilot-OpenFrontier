using OpenFrontier.IP.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public static class SelectableExtensions
	{
		public static void SetTextColor(this Selectable selectable, Color color)
		{
			TextMeshProUGUI componentInChildren = selectable.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.color = color;
			}
		}

		public static void SetTextColorFromActiveState(this Selectable selectable, bool active)
		{
			TextMeshProUGUI componentInChildren = selectable.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.color = (active ? GameController.Instance.GameSettings.UIButtonColors.DefaultTextColor : GameController.Instance.GameSettings.UIButtonColors.NoItemsTextColor);
			}
		}

		public static void SetTextColorFromActiveState(this Graphic graphic, bool active)
		{
			TextMeshProUGUI componentInChildren = graphic.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.color = (active ? GameController.Instance.GameSettings.UIButtonColors.DefaultTextColor : GameController.Instance.GameSettings.UIButtonColors.NoItemsTextColor);
			}
		}
	}
}
