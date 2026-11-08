using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class ScreenSizeToScale : MonoBehaviour
	{
		private int x;

		private int y;

		public void Apply()
		{
			Vector3 localScale = transform.localScale;
			localScale.x = Screen.width;
			localScale.y = Screen.height;
			transform.localScale = localScale;
		}

		private void Update()
		{
			if (Screen.width != x || Screen.height != y)
			{
				x = Screen.width;
				y = Screen.height;
				Apply();
			}
		}
	}
}
