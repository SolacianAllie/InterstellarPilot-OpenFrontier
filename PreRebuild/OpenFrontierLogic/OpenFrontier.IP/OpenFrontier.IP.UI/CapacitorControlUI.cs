using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class CapacitorControlUI : MonoBehaviour
	{
		private EngineASX engine;

		private bool expire;

		private float expiryTime;

		public List<GameObject> HideableObjects = new List<GameObject>();

		private HudScreen hud;

		private float lastCapacitorValue = 1f;

		private bool visible;

		private void Awake()
		{
			hud = HudScreen.Instance;
			engine = EngineASX.Instance;
		}

		private void Start()
		{
			lastCapacitorValue = GetCapacitorNormalizedValue();
		}

		private void OnEnable()
		{
			lastCapacitorValue = GetCapacitorNormalizedValue();
		}

		private void SetHideableObjectsActive(bool active)
		{
			foreach (GameObject hideableObject in HideableObjects)
			{
				hideableObject.SetActive(active);
			}
		}

		private float GetCapacitorNormalizedValue()
		{
			float result = 1f;
			Unit playerUnit = hud.Eng.PlayerUnit;
			if (playerUnit != null && playerUnit.Components.Capacitor != null)
			{
				result = playerUnit.Components.Capacitor.ChargeNormalized;
			}
			return result;
		}

		private void Update()
		{
			float capacitorNormalizedValue = GetCapacitorNormalizedValue();
			if (capacitorNormalizedValue != lastCapacitorValue)
			{
				lastCapacitorValue = capacitorNormalizedValue;
				expire = false;
				if (!visible && lastCapacitorValue < engine.GameSettings.ShowCapacitorLowerThreshold)
				{
					SetHideableObjectsActive(active: true);
					visible = true;
				}
			}
			else if (!expire)
			{
				if (capacitorNormalizedValue >= 1f)
				{
					expire = true;
					expiryTime = Time.time + 2f;
				}
			}
			else if (Time.time > expiryTime && capacitorNormalizedValue > engine.GameSettings.ShowCapacitorUpperThreshold)
			{
				expire = false;
				SetHideableObjectsActive(active: false);
				visible = false;
			}
		}
	}
}
