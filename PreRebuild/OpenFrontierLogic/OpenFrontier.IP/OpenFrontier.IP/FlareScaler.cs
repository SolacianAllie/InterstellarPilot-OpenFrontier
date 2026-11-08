using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class FlareScaler : MonoBehaviour
	{
		public const float DistanceCheckFrequency = 2f;

		public const float FadeOutDistance = 60f;

		public ActiveUnit ActiveUnit;

		public float BrightnessLerpRate = 10f;

		[SerializeField]
		private float desiredBrightness;

		public float FlickerBrightnessChange;

		public float FlickerRate = 1f;

		private bool inRange = true;

		private LensFlare lensFlare;

		public float MaxBrightness = 1f;

		public float MaxFlickerBrightnessChange = 0.5f;

		public float MinBrightness;

		private float nextDistanceChk;

		public float NextFlickerTime;

		public float ThrottleBrightnessChange = 1f;

		public float UnitThrottleFlickerEffect = 1f;

		private void Awake()
		{
			ActiveUnit = UnityObjectHelper.FindInParentsOrSelf<ActiveUnit>(gameObject);
		}

		private void Start()
		{
			lensFlare = gameObject.GetComponent<LensFlare>();
			desiredBrightness = MaxBrightness;
			CheckInRange();
		}

		private void Update()
		{
			if (!EngineASX.LoadedAndReady)
			{
				return;
			}
			Camera mainCamera = GameController.Instance.MainCamera;
			if (!(mainCamera != null) || !(lensFlare != null))
			{
				return;
			}
			CheckInRange();
			if ((ActiveUnit == null || !ActiveUnit.Unit.IsDestroyed) && inRange)
			{
				float num = Vector3.Distance(transform.position, mainCamera.transform.position);
				float num2 = Mathf.Lerp(MaxBrightness, MinBrightness, num / 60f);
				if (ActiveUnit != null && ActiveUnit.Unit.Components != null)
				{
					num2 += ActiveUnit.Unit.Components.EngineThrottle;
					ApplyFlicker();
				}
				desiredBrightness = num2 + FlickerBrightnessChange;
			}
			lensFlare.brightness = Mathf.Lerp(lensFlare.brightness, desiredBrightness, Time.deltaTime * BrightnessLerpRate);
		}

		private bool ShouldBeEnabled()
		{
			return inRange;
		}

		private void UpdateEnabled()
		{
			lensFlare.enabled = ShouldBeEnabled();
		}

		private void CheckInRange()
		{
			if (!(Time.time > nextDistanceChk))
			{
				return;
			}
			if (ActiveUnit != null && ActiveUnit.Unit != null && !ActiveUnit.Unit.IsDestroyed)
			{
				bool flag = ActiveUnit.LastDistanceFromCamera < 60f;
				nextDistanceChk = Time.time + 2f;
				if (flag != inRange)
				{
					inRange = flag;
					UpdateEnabled();
				}
			}
			else
			{
				desiredBrightness = 0f;
			}
		}

		private void ApplyFlicker()
		{
			FlickerBrightnessChange = Mathf.Sin(Time.time * FlickerRate * (1f + ActiveUnit.Unit.Components.EngineThrottle * UnitThrottleFlickerEffect)) * MaxFlickerBrightnessChange;
		}
	}
}
