using UnityEngine;

namespace OpenFrontier.IP.Assets.Scripts.UI
{
	[RequireComponent(typeof(RectTransform))]
	public class FixedSizeOnStandalone : MonoBehaviour
	{
		private RectTransform rectTransform;

		public Vector2 Size = new Vector2(800f, 600f);

		public bool Enabled;

		private void Awake()
		{
			if (Enabled)
			{
				rectTransform = GetComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
				rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
				rectTransform.sizeDelta = Size;
				rectTransform.anchoredPosition = Vector2.zero;
			}
		}
	}
}
