using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hud
{
	public class HudAutoPilotController : MonoBehaviour
	{
		public GameObject AutoPilotEnabledRoot;

		public Button CancelAutoPilotButton;

		public Button SetOrderButton;

		private EngineASX engine;

		public TextMeshProUGUI CurrentOrderText;

		public TextMeshProUGUI AutoPilotText;

		private double lastRefreshTime;

		private void Awake()
		{
			engine = EngineASX.Instance;
			CancelAutoPilotButton.onClick.AddListener(CancelAutoPilot);
			SetOrderButton.onClick.AddListener(SetOrderButtonClick);
		}

		private void Update()
		{
			if (engine != null)
			{
				bool flag = ShouldShowAutoPilot();
				if (flag && Time.realtimeSinceStartupAsDouble > lastRefreshTime + 2.0)
				{
					Refresh();
				}
				AutoPilotEnabledRoot.gameObject.SetActive(flag);
			}
		}

		private void SetOrderButtonClick()
		{
			NewOrderTarget newOrderTarget = NewOrderTarget.CreateFromFleets(engine.PlayerUnit.GetFleet());
			UIController.Instance.ScreenNavigator.ShowNewFleetOrderScreen(newOrderTarget, () =>
			{
				UIController.Instance.ScreenNavigator.NavigateToRootScreen(UIController.Instance.ScreenNavigator.RootScreen);
			}, stack: false);
		}

		public void Refresh()
		{
			lastRefreshTime = Time.realtimeSinceStartupAsDouble;
			CurrentOrderText.text = GetCurrentOrderText();
			AutoPilotText.text = GetAutopilotText();
		}

		private string GetAutopilotText()
		{
			string autopilotName = GetAutopilotName();
			string text = ((!string.IsNullOrWhiteSpace(autopilotName)) ? autopilotName : "[Unknown]");
			return "Ship pilotted by " + text;
		}

		private string GetAutopilotName()
		{
			if (EngineASX.Instance.LocalUnit == null || EngineASX.Instance.LocalUnit.GetPilot() == null)
			{
				return null;
			}
			return EngineASX.Instance.LocalUnit.Components.PilotPerson.ShortNameWithFullRank;
		}

		private bool ShouldShowAutoPilot()
		{
			Unit localUnit = engine.LocalUnit;
			if (localUnit != null && localUnit.IsValidAndNotDestroyed && localUnit.IsOwnedByPlayer)
			{
				return OrdersHelper.IsPilottedByNpc(localUnit);
			}
			return false;
		}

		private string GetCurrentOrderText()
		{
			return OrdersHelper.GetOrdersTextAndFleetStatus(engine.LocalUnit, EngineASX.Instance.LocalFaction);
		}

		private void CancelAutoPilot()
		{
			Unit playerUnit = engine.PlayerUnit;
			if (playerUnit != null)
			{
				if (playerUnit.GetFleet() != null)
				{
					EngineASX.Instance.CachedFleetSettingsController.CacheForUnit(playerUnit);
				}
				OrdersHelper.CancelAutoPilot(playerUnit);
				if (engine.Hud != null)
				{
					engine.Hud.ClearDesiredTurn();
					engine.Hud.ClearAutoTurnDesiredBearing();
				}
			}
		}
	}
}
