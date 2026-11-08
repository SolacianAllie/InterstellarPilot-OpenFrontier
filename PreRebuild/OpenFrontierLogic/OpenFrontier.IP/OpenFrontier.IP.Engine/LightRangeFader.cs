using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Light))]
	public class LightRangeFader : MonoBehaviour
	{
		public LightIntensityFaderOption FaderOption;

		public float Duration = 3f;

		private float startTime;

		private float startRange;

		private Light ourLight;

		private void Awake()
		{
			ourLight = GetComponent<Light>();
			startRange = ourLight.range;
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
				ourLight.range = (1f - num / Duration) * startRange;
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
