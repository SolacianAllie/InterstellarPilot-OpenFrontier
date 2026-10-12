using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Extensions
{
	public static class ScrollRectExtensions
	{
		public static void ResetScrollPosition(this ScrollRect scrollRect)
		{
			if (scrollRect.verticalScrollbar != null)
			{
				scrollRect.verticalScrollbar.ResetScrollValue();
			}
			if (scrollRect.horizontalScrollbar != null)
			{
				scrollRect.horizontalScrollbar.ResetScrollValue();
			}
		}

		public static void ScrollToPosition(this ScrollRect scrollRect, float normalizedPosition)
		{
			if (scrollRect.vertical)
			{
				scrollRect.verticalNormalizedPosition = 1f - normalizedPosition;
			}
			if (scrollRect.horizontal)
			{
				scrollRect.horizontalNormalizedPosition = 1f - normalizedPosition;
			}
		}
	}
}
