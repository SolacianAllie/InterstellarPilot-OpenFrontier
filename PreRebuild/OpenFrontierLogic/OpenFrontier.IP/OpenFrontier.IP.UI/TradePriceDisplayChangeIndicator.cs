using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class TradePriceDisplayChangeIndicator : MonoBehaviour
	{
		public Image DisplayImage;

		public bool PositiveChange = true;

		public bool GoodChange = true;

		private float onEnableTime;

		public float ShowDuration = 2f;

		public float VisibleThreshold;

		public float BlinkRate = 8f;

		public EngineASX Engine;

		private void Awake()
		{
			DisplayImage.gameObject.SetActive(value: false);
		}

		private void OnEnable()
		{
			onEnableTime = Time.realtimeSinceStartup;
			RefreshAppearance();
		}

		private void Update()
		{
			float num = Time.realtimeSinceStartup - onEnableTime;
			if (num > ShowDuration)
			{
				gameObject.SetActive(value: false);
			}
			else
			{
				DisplayImage.gameObject.SetActive(Mathf.Sin(num * BlinkRate) > VisibleThreshold);
			}
		}

		public void Show()
		{
			RefreshAppearance();
			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(value: true);
			}
			else
			{
				ResetTime();
			}
		}

		private void RefreshAppearance()
		{
			if (Engine != null)
			{
				DisplayImage.color = (GoodChange ? Engine.CreditsUpColor : Engine.CreditsDownColor);
			}
			DisplayImage.rectTransform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, PositiveChange ? 90f : (-90f)));
		}

		public void ResetTime()
		{
			onEnableTime = Time.realtimeSinceStartup;
		}
	}
}
