using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Extensions
{
	public static class UnityUIExtensions
	{
		public static void ResetScrollValue(this Scrollbar scrollbar)
		{
			switch (scrollbar.direction)
			{
			case Scrollbar.Direction.RightToLeft:
			case Scrollbar.Direction.BottomToTop:
				scrollbar.value = 1f;
				break;
			case Scrollbar.Direction.LeftToRight:
			case Scrollbar.Direction.TopToBottom:
				scrollbar.value = 0f;
				break;
			}
		}
	}
}
