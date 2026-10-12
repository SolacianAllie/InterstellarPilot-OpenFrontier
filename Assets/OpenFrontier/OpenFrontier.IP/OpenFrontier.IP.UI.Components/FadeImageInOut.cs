using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Components
{
	public class FadeImageInOut : MonoBehaviour
	{
		public Image Image;

		public bool ForwardDirection = true;

		private float currentElapsedTime;

		public float Duration = 1f;

		public float FadeDuration = 0.5f;

		private Color startColor;

		private void Awake()
		{
			startColor = Image.color;
		}

		private void Update()
		{
			currentElapsedTime += Time.deltaTime;
			if (currentElapsedTime > Duration)
			{
				currentElapsedTime = 0f;
			}
			if (ForwardDirection)
			{
				float a = Mathf.Clamp01(currentElapsedTime / FadeDuration) * startColor.a;
				Image.color = new Color(startColor.r, startColor.g, startColor.b, a);
			}
			else
			{
				float a2 = (1f - Mathf.Clamp01(currentElapsedTime / FadeDuration)) * startColor.a;
				Image.color = new Color(startColor.r, startColor.g, startColor.b, a2);
			}
		}

		private void OnDisable()
		{
			currentElapsedTime = 0f;
		}
	}
}
