using System.Linq;
using System.Text;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Hypersleep;
using OpenFrontier.IP.Engine.Settings;
using OpenFrontier.IP.UI.Screens.SectorMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hypersleep
{
	public class HypersleepScreen : EngineScreen
	{
		public SectorMapLabelDrawer SectorMapLabelDrawer;

		public Button ToggleMapButton;

		public Transform SectorMapRoot;

		public TextMeshProUGUI CreditsLabel;

		public OpenFrontier.IP.UI.Screens.SectorMap.SectorMap SectorMap;

		public SectorMapSceneDrawer SectorMapDrawer;

		public TextMeshProUGUI CurrentTimeLabel;

		public TextMeshProUGUI CurrentTimeMultiplierLabel;

		public TextMeshProUGUI CurrentLocationLabel;

		public TextMeshProUGUI CurrentOrderLabel;

		private StringBuilder currentTimeStringBuilder = new StringBuilder();

		private StringBuilder currentTimeMultiplierStringBuilder = new StringBuilder();

		private StringBuilder currentLocationStringBuilder = new StringBuilder();

		public Button BackButton;

		public Transform TimeMultiplierOptionsTransform;

		public Toggle TimeMultiplierTogglePrefab;

		private float lastUpdateLocationRealTime;

		public Toggle ExitWhenCurrentOrderCompletesToggle;

		private ActiveFleetOrder currentPlayerOrder;

		public UnitConditionControllerUI UnitConditionController;

		public float NextSectorMapAutoRebuildTime = float.MaxValue;

		public float TimeBetweenSectorMapRebuild = 2f;

		private int oldCredits = -1;

		public bool IsSectorMapEnabled => SectorMapRoot.gameObject.activeSelf;

		protected override void awake()
		{
			base.awake();
			BackButton.onClick.AddListener(BackButtonClick);
			float hypersleepTimeMultiplier = GameController.Instance.HypersleepTimeMultiplier;
			foreach (HypersleepTimescaleSetting timeMultiplierOption in GameController.Instance.GameSettings.HypersleepSettings.TimescaleSettings.OrderBy((HypersleepTimescaleSetting e) => e.TimeMultiplier))
			{
				Toggle toggle = Object.Instantiate(TimeMultiplierTogglePrefab);
				toggle.transform.SetParent(TimeMultiplierOptionsTransform);
				toggle.isOn = timeMultiplierOption.TimeMultiplier == hypersleepTimeMultiplier;
				toggle.onValueChanged.AddListener((bool value) =>
				{
					GameController.Instance.HypersleepTimeMultiplier = timeMultiplierOption.TimeMultiplier;
				});
				toggle.transform.localScale = Vector3.one;
				toggle.group = TimeMultiplierOptionsTransform.GetComponentInChildren<ToggleGroup>();
				toggle.GetComponentInChildren<TextMeshProUGUI>().text = $"{timeMultiplierOption.TimeMultiplier:N2}x";
			}
			ExitWhenCurrentOrderCompletesToggle.onValueChanged.AddListener(ExitWhenCurrentOrderCompletesToggleValueChanged);
			ToggleMapButton.onClick.AddListener(ToggleMapButtonClick);
		}

		private void ToggleMapButtonClick()
		{
			ToggleSectorMap();
		}

		private void ToggleSectorMap()
		{
			SetSectorMapEnabled(!IsSectorMapEnabled);
		}

		private void SetSectorMapEnabled(bool value)
		{
			SectorMapRoot.gameObject.SetActive(value);
			if (value)
			{
				RefreshSectorMap();
			}
		}

		private void ExitWhenCurrentOrderCompletesToggleValueChanged(bool value)
		{
			if (value)
			{
				currentPlayerOrder = OrdersHelper.GetFleetCurrentOrder(EngineASX.Instance.LocalUnit);
			}
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (navigatedForward)
			{
				EngineASX.Instance.ActiveSector = null;
			}
		}

		protected override void update()
		{
			base.update();
			RefreshCurrentTime();
			RefreshCurrentTimeMultiplier();
			UpdateTimescale();
			if (!HypersleepHelper.IsHypersleepAvailable(out var reason))
			{
				UIController.Instance.QuickMsg.AddMessage("Exiting hypersleep: " + reason);
				ExitHypersleepAndSetUI();
			}
			else
			{
				UnitConditionController.LocalUnit = EngineASX.Instance.LocalUnit;
				UnitConditionController.Tick();
				ExitWhenCurrentOrderCompletesToggle.interactable = OrdersHelper.IsPilottedByNpc(EngineASX.Instance.LocalUnit);
				if (ExitWhenCurrentOrderCompletesToggle.isOn && currentPlayerOrder == null)
				{
					UIController.Instance.QuickMsg.AddMessage("Exiting hypersleep: Order complete");
					ExitHypersleepAndSetUI();
				}
			}
			if (RealTime.time > lastUpdateLocationRealTime + 1f)
			{
				RefreshCurrentLocation();
				RefreshCurrentOrder();
				lastUpdateLocationRealTime = RealTime.time;
			}
			if (IsSectorMapEnabled)
			{
				if ((EngineASX.Instance.LocalPlayerSector != null && Time.realtimeSinceStartup > NextSectorMapAutoRebuildTime) || SectorMapDrawer.Sector != EngineASX.Instance.LocalPlayerSector)
				{
					RefreshSectorMap();
					SetNextSectorMapAutoRebuildTime();
				}
				SectorMap.UpdateMapSize();
				SectorMapDrawer.Tick();
				if (EngineASX.Instance.LocalUnit != null)
				{
					SectorMap.CenterOnLocalUnit();
				}
				SectorMap.RepositionMapItems();
				SectorMapLabelDrawer.Tick();
			}
			RefreshCreditsLabel();
		}

		private void RefreshCreditsLabel()
		{
			if (EngineASX.Instance.LocalFaction.Credits != oldCredits)
			{
				oldCredits = EngineASX.Instance.LocalFaction.Credits;
				CreditsLabel.text = TextFormattingHelper.FormatCredits(EngineASX.Instance.LocalFaction.Credits, includeSuffix: true);
			}
		}

		public void SetNextSectorMapAutoRebuildTime()
		{
			NextSectorMapAutoRebuildTime = Time.realtimeSinceStartup + TimeBetweenSectorMapRebuild;
		}

		private static void UpdateTimescale()
		{
			if (Time.timeScale != GameController.Instance.HypersleepTimeMultiplier)
			{
				Time.timeScale = Mathf.MoveTowards(Time.timeScale, GameController.Instance.HypersleepTimeMultiplier, GameController.Instance.GameSettings.HypersleepSettings.TimeMultiplierChangeRate * RealTime.deltaTime);
			}
		}

		protected override void refresh()
		{
			base.refresh();
			UnitConditionController.LocalUnit = EngineASX.Instance.LocalUnit;
			UnitConditionController.Tick();
			RefreshCurrentOrder();
			RefreshCurrentTime();
			RefreshCurrentTimeMultiplier();
			RefreshCurrentLocation();
			if (IsSectorMapEnabled)
			{
				RefreshSectorMap();
				SetNextSectorMapAutoRebuildTime();
			}
			RefreshCreditsLabel();
		}

		private void RefreshSectorMap()
		{
			if (EngineASX.Instance.LocalPlayerSector != null)
			{
				SectorMapDrawer.Sector = EngineASX.Instance.LocalPlayerSector;
				SectorMap.Sector = EngineASX.Instance.LocalPlayerSector;
				SectorMapDrawer.RebuildMap();
				SectorMap.CenterOnLocalUnit();
				SectorMap.Refresh();
			}
		}

		private void RefreshCurrentOrder()
		{
			CurrentOrderLabel.text = GetCurrentOrderText();
		}

		private string GetCurrentOrderText()
		{
			if (!OrdersHelper.IsPilottedByNpc(EngineASX.Instance.LocalUnit))
			{
				return string.Empty;
			}
			return OrdersHelper.GetOrdersTextAndFleetStatus(EngineASX.Instance.LocalUnit, EngineASX.Instance.LocalFaction);
		}

		private void RefreshCurrentLocation()
		{
			currentLocationStringBuilder.Clear();
			DockUIHeader.GetLocationTextNonAlloc(EngineASX.Instance.LocalUnit, currentLocationStringBuilder);
			currentLocationStringBuilder.Append(" ");
			TextFormattingHelper.FormatSectorPositionNonAlloc(EngineASX.Instance.LocalUnit.SectorPosition, currentLocationStringBuilder);
			CurrentLocationLabel.SetText(currentLocationStringBuilder);
		}

		private void RefreshCurrentTime()
		{
			currentTimeStringBuilder.Clear();
			EngineASX.Instance.DateTimeUtils.GetFormattedGameWorldDateAndDayNonAlloc(currentTimeStringBuilder);
			CurrentTimeLabel.SetText(currentTimeStringBuilder);
		}

		private void RefreshCurrentTimeMultiplier()
		{
			currentTimeMultiplierStringBuilder.Clear();
			currentTimeMultiplierStringBuilder.Append("Time Multiplier: ");
			currentTimeMultiplierStringBuilder.AppendFormat("{0:N2}x", Time.timeScale);
			CurrentTimeMultiplierLabel.SetText(currentTimeMultiplierStringBuilder);
		}

		protected override void OnNavigateBackFromEscapeKey()
		{
			ExitHypersleepAndSetUI();
		}

		private void BackButtonClick()
		{
			ExitHypersleepAndSetUI();
		}

		protected override void onNotCurrentPanel()
		{
			base.onNotCurrentPanel();
			if (Time.timeScale > 1f)
			{
				Time.timeScale = 1f;
			}
		}

		public static void ExitHypersleepAndSetUI()
		{
			Time.timeScale = 1f;
			if (EngineASX.Instance.LocalUnit != null)
			{
				EngineASX.Instance.ActiveSector = EngineASX.Instance.LocalUnit.Sector;
			}
			EngineASX.Instance.SetUIFromPlayerStatus(forceExitFromHypersleep: true);
			GameController.Instance.SaveHypersleepTimeMultiplier();
			EngineASX.Instance.OnCameraMoved();
		}
	}
}
