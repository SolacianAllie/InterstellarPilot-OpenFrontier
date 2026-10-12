using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class FadeInWidget : MonoBehaviour
	{
		private Color fadeColor = Color.white;

		private float fadeProgress;

		public float FadeRate = 1f;

		private Graphic graphic;

		private Color initialColor = Color.white;

		private bool isFading;

		private float lastRealTime;

		private void Awake()
		{
			graphic = GetComponent<Graphic>();
			initialColor = graphic.color;
			fadeColor = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);
		}

		private void OnDisable()
		{
			if (!gameObject.activeSelf)
			{
				isFading = true;
				fadeProgress = 0f;
				SetWidgetColor();
			}
		}

		private void OnEnable()
		{
			lastRealTime = Time.realtimeSinceStartup;
		}

		private void Update()
		{
			if (isFading)
			{
				float num = Time.realtimeSinceStartup - lastRealTime;
				lastRealTime = Time.realtimeSinceStartup;
				fadeProgress += FadeRate * num;
				SetWidgetColor();
				if (fadeProgress >= 1f)
				{
					isFading = false;
				}
			}
		}

		private void SetWidgetColor()
		{
			graphic.color = Color.Lerp(fadeColor, initialColor, fadeProgress);
		}
	}
}
