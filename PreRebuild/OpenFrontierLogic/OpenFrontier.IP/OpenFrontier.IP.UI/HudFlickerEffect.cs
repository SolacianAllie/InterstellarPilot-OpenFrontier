using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class HudFlickerEffect : MonoBehaviour
	{
		public FlickerPanelsAlpha FlickerController;

		private HudScreen hud;

		public float HudFlickerEffectThreshold = 0.15f;

		public float HudFlickerProbabilityOnDamage = 0.4f;

		private float lastKnownCondition;

		public float MaxTime = 5f;

		public float MinTime = 3f;

		private float deactivateTime;

		public float MinTimeBeforeRepeat = 20f;

		private float lastStartTime = float.MinValue;

		private void Awake()
		{
			hud = UnityObjectHelper.FindInParentsOrSelf<HudScreen>(gameObject);
			EngineASX.Instance.PlayerUnitChanged += Instance_PlayerUnitChanged;
		}

		private void Instance_PlayerUnitChanged(EngineASX sender, Unit oldUnit)
		{
			Cancel();
			CacheCondition();
		}

		private void CacheCondition()
		{
			if (EngineASX.Instance.LocalUnit != null && EngineASX.Instance.LocalUnit.Destructable != null)
			{
				lastKnownCondition = EngineASX.Instance.LocalUnit.Destructable.HealthNormalized;
			}
			else
			{
				lastKnownCondition = float.MinValue;
			}
		}

		public void Cancel()
		{
			FlickerController.enabled = false;
		}

		public void StartFlicker()
		{
			lastStartTime = Time.time;
			FlickerController.enabled = true;
			deactivateTime = Time.time + Random.Range(MinTime, MaxTime);
		}

		private void Update()
		{
			if (FlickerController.enabled)
			{
				if (Time.time > deactivateTime)
				{
					Cancel();
				}
			}
			else if (ShouldStartFlicker())
			{
				StartFlicker();
			}
			CacheCondition();
		}

		private bool ShouldStartFlicker()
		{
			if (Time.time < lastStartTime + MinTimeBeforeRepeat)
			{
				return false;
			}
			if (hud != null && hud.FadingState == ScreenFadeState.None)
			{
				Unit playerUnit = hud.PlayerUnit;
				if (playerUnit != null)
				{
					float healthNormalized = playerUnit.Destructable.HealthNormalized;
					if (healthNormalized < lastKnownCondition && healthNormalized < HudFlickerEffectThreshold && Random.value < HudFlickerProbabilityOnDamage)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
