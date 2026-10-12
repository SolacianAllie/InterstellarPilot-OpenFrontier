using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Light))]
	public class LightIntensityFader : MonoBehaviour
	{
		public LightIntensityFaderOption FaderOption;

		public float Duration = 3f;

		private float startTime;

		private float startIntensity;

		private Light ourLight;

		private void Awake()
		{
			ourLight = GetComponent<Light>();
			startIntensity = ourLight.intensity;
		}

		private void OnEnable()
		{
			startTime = Time.time;
		}

		private void Update()
		{
			float num = Time.time - startTime;
			if (num > Duration)
			{
				Finish();
			}
			else
			{
				ourLight.intensity = (1f - num / Duration) * startIntensity;
			}
		}

		public void Finish()
		{
			switch (FaderOption)
			{
			case LightIntensityFaderOption.Destroy:
				Object.Destroy(gameObject);
				break;
			case LightIntensityFaderOption.Deactivate:
				gameObject.SetActive(value: false);
				break;
			}
		}
	}
}
