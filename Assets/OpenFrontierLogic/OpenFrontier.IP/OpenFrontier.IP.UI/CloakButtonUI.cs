using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(Button))]
	public class CloakButtonUI : MonoBehaviour
	{
		private Button button;

		public float CheckEnabledFrequency = 0.2f;

		private HudScreen hud;

		private float nextCheck;

		public void Cloak()
		{
			UpdateEnabled();
			if (button.interactable)
			{
				if (hud.PlayerUnit.Components.CloakComponent.State == CloakState.Decloaked)
				{
					hud.Eng.PlayerUnit.Components.CloakComponent.StartCloak();
				}
				else
				{
					hud.Eng.PlayerUnit.Components.CloakComponent.StartDecloak();
				}
			}
			UpdateEnabled();
		}

		public bool GetShouldBeEnabled()
		{
			UnitComponentHolder components = hud.PlayerUnit.Components;
			if (components.CloakComponent != null)
			{
				if (components.CloakComponent.State == CloakState.Cloaked)
				{
					return true;
				}
				if (components.CloakComponent.State == CloakState.Decloaked)
				{
					return components.CloakComponent.CanCloak;
				}
			}
			return false;
		}

		private void Start()
		{
			button = GetComponent<Button>();
			button.onClick.AddListener(Cloak);
			hud = UnityObjectHelper.FindInParentsOrSelf<HudScreen>(gameObject);
			UpdateEnabled();
		}

		private void Update()
		{
			if (Time.time > nextCheck)
			{
				UpdateEnabled();
				nextCheck = Time.time + CheckEnabledFrequency;
			}
		}

		private void UpdateEnabled()
		{
			bool shouldBeEnabled = GetShouldBeEnabled();
			if (shouldBeEnabled != button.interactable)
			{
				button.interactable = shouldBeEnabled;
			}
		}
	}
}
