using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class SavingGameIcon : MonoBehaviour
	{
		public Image Image;

		private float enabledRealTime;

		public float FlashRate = 2f;

		public float MinAlpha = 0.2f;

		public float MaxAlpha = 0.8f;

		public float Duration = 2f;

		public void Awake()
		{
			Image.enabled = false;
		}

		public void Activate()
		{
			enabled = true;
			Image.enabled = true;
			enabledRealTime = Time.realtimeSinceStartup;
		}

		private void Update()
		{
			float t = (Mathf.Sin((Time.realtimeSinceStartup - enabledRealTime) * FlashRate) + 1f) / 2f;
			float a = Mathf.Lerp(MinAlpha, MaxAlpha, t);
			Image.color = new Color(1f, 1f, 1f, a);
			if (Time.realtimeSinceStartup - enabledRealTime > Duration)
			{
				enabled = false;
				Image.enabled = false;
			}
		}
	}
}
