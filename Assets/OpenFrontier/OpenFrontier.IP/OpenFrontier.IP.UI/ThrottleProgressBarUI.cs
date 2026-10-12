using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ThrottleProgressBarUI : MonoBehaviour
	{
		public enum ThrottleState
		{
			NotVisible,
			Solid,
			Fading
		}

		public bool AutoHide;

		private float currentAlpha;

		public CanvasGroup FadeCanvasGroup;

		public float FadeDuration = 1f;

		private bool hasUpdated;

		public HudScreen Hud;

		public float MaxAlpha = 1f;

		public float MinProgressBarWidth = 2f;

		public Graphic ProgressBar;

		public RectTransform SliderRect;

		private ThrottleState state;

		private float stateChangeTime;

		public Slider ThumbSlider;

		public float VisibleDuration = 1f;

		public ThrottleState State
		{
			get
			{
				return state;
			}
			set
			{
				state = value;
				stateChangeTime = Time.time;
				switch (state)
				{
				case ThrottleState.Fading:
					CurrentAlpha = 1f;
					gameObject.SetActive(value: true);
					UpdateSliderValue();
					break;
				case ThrottleState.Solid:
					CurrentAlpha = 1f;
					gameObject.SetActive(value: true);
					UpdateSliderValue();
					break;
				case ThrottleState.NotVisible:
					gameObject.SetActive(value: false);
					break;
				}
			}
		}

		public float CurrentAlpha
		{
			get
			{
				return currentAlpha;
			}
			set
			{
				if (currentAlpha != value)
				{
					currentAlpha = value;
					if (AutoHide)
					{
						SetObjectsAlpha();
					}
				}
			}
		}

		public void NotifyValueChanged()
		{
			if (Hud.PlayerUnit != null)
			{
				State = ThrottleState.Solid;
			}
		}

		private void Awake()
		{
			if (AutoHide)
			{
				gameObject.SetActive(value: false);
			}
			ThumbSlider.value = 0f;
			if (ThumbSlider != null)
			{
				ThumbSlider.onValueChanged.AddListener(ThumbSlider_ValueChanged);
			}
		}

		private void ThumbSlider_ValueChanged(float delta)
		{
			if (hasUpdated && !Hud.Eng.PlayerUnitAutoPilotEnabled)
			{
				Hud.PlayerUnit.Components.EngineThrottle = ThumbSlider.value;
			}
		}

		private void Update()
		{
			UpdateSliderValue();
			if (!AutoHide)
			{
				return;
			}
			switch (state)
			{
			case ThrottleState.Solid:
				if (Time.time > stateChangeTime + VisibleDuration)
				{
					State = ThrottleState.Fading;
				}
				break;
			case ThrottleState.Fading:
				if (Time.time > stateChangeTime + FadeDuration)
				{
					State = ThrottleState.NotVisible;
				}
				else
				{
					CurrentAlpha = MaxAlpha * (1f - (Time.time - stateChangeTime) / FadeDuration);
				}
				break;
			}
		}

		private void SetObjectsAlpha()
		{
			FadeCanvasGroup.alpha = CurrentAlpha;
		}

		private void UpdateSliderValue()
		{
			hasUpdated = true;
			if (Hud.PlayerUnit != null && Hud.PlayerUnit.ActiveUnit != null && Hud.PlayerUnit.ActiveUnit.UnitRigidBody != null)
			{
				UnitEngineComponent engineComponent = Hud.PlayerUnit.Components.EngineComponent;
				if (engineComponent != null)
				{
					ThumbSlider.value = engineComponent.EngineThrottle;
					float value = Hud.PlayerUnit.ActiveUnit.UnitRigidBody.linearVelocity.magnitude / Hud.PlayerUnit.Components.EngineComponent.EngineClass.GetCurrentMaxSpeed(Hud.PlayerUnit);
					UpdateSliderValue(Mathf.Clamp01(value));
				}
			}
		}

		private void UpdateSliderValue(float val)
		{
			Vector3 localScale = ProgressBar.transform.localScale;
			localScale.x = val * SliderRect.GetWidth();
			ProgressBar.gameObject.SetActive(localScale.x >= MinProgressBarWidth);
			ProgressBar.rectTransform.SetWidth(Mathf.RoundToInt(localScale.x));
		}
	}
}
