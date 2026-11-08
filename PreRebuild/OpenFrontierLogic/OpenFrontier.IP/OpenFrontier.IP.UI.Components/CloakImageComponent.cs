using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Components
{
	public class CloakImageComponent : MonoBehaviour
	{
		public CloakComponent TargetCloakComponent;

		private CloakState? previousCloakState;

		private float currentElapsedTime;

		public float Duration = 1f;

		public float FadeDuration = 0.5f;

		private Color startColor;

		public Image Image;

		public bool DeactivateInsteadOfDisable = true;

		private void Awake()
		{
			if (Image != null)
			{
				startColor = Image.color;
				SetImageActive(active: false);
			}
		}

		private void SetImageActive(bool active)
		{
			if (DeactivateInsteadOfDisable)
			{
				Image.gameObject.SetActive(active);
			}
			else
			{
				Image.enabled = active;
			}
		}

		public void ClearState()
		{
			SetImageActive(active: false);
			TargetCloakComponent = null;
			currentElapsedTime = 0f;
			previousCloakState = null;
		}

		public void Tick()
		{
			CloakState? cloakState = null;
			if (TargetCloakComponent != null)
			{
				cloakState = TargetCloakComponent.State;
			}
			if (cloakState != previousCloakState)
			{
				OnCloakStateChange(cloakState);
			}
			if (cloakState.HasValue)
			{
				switch (cloakState.GetValueOrDefault())
				{
				case CloakState.Decloaking:
				{
					AnimateTime();
					float a2 = (1f - Mathf.Clamp01(currentElapsedTime / FadeDuration)) * startColor.a;
					Image.color = new Color(startColor.r, startColor.g, startColor.b, a2);
					break;
				}
				case CloakState.Cloaking:
				{
					AnimateTime();
					float a = Mathf.Clamp01(currentElapsedTime / FadeDuration) * startColor.a;
					Image.color = new Color(startColor.r, startColor.g, startColor.b, a);
					break;
				}
				}
			}
		}

		private void OnCloakStateChange(CloakState? newState)
		{
			previousCloakState = newState;
			switch (newState)
			{
			case null:
			case CloakState.Decloaked:
				SetImageActive(active: false);
				break;
			case CloakState.Cloaked:
				SetImageActive(active: true);
				Image.color = Image.color.WithAlpha(1f);
				break;
			case CloakState.Decloaking:
				SetImageActive(active: true);
				currentElapsedTime = 0f;
				break;
			case CloakState.Cloaking:
				SetImageActive(active: true);
				currentElapsedTime = 0f;
				break;
			}
		}

		private void AnimateTime()
		{
			currentElapsedTime += Time.deltaTime;
			if (currentElapsedTime > Duration)
			{
				currentElapsedTime = 0f;
			}
		}

		private void OnDisable()
		{
			currentElapsedTime = 0f;
		}
	}
}
