using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens.CreateUnitVariant;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.ShipTrader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens
{
	public class ShipBuyScreen : EngineScreen
	{
		public delegate void OnShipBoughtHandler(ShipBuyScreen sender, List<Unit> boughtShips);

		public Toggle CreateFleetToggle;

		public Slider QuantitySlider;

		public TextMeshProUGUI TotalCostText;

		public Image ShipImage;

		public Toggle DockShipsToggle;

		public Text DockShipsLabel;

		public Button BuyButton;

		public Button BuyAndEnterShipButton;

		private ShipTraderItemWrapper currentItem;

		public Text ShipDealerText;

		public Button ShowInfoButton;

		private bool? wasAffordable;

		public Button CustomizeButton;

		public Unit ShipTraderUnit { get; set; }

		public UnitShipTrader ShipTrader
		{
			get
			{
				if (ShipTraderUnit != null)
				{
					return ShipTraderUnit.ShipTrader;
				}
				return null;
			}
		}

		public ShipTraderItemWrapper CurrentItem
		{
			get
			{
				return currentItem;
			}
			set
			{
				currentItem = value;
			}
		}

		public int BuyQuantity => (int)QuantitySlider.value;

		public event OnShipBoughtHandler OnShipBought;

		public static int GetItemSaleCostPerUnit(EngineASX engine, Unit unitForSale, Faction sellerFaction, Faction buyerFaction)
		{
			return GetItemSaleCostPerUnit(engine, unitForSale.UnitClass.SaleCost, sellerFaction, buyerFaction);
		}

		public static int GetItemSaleCostPerUnit(EngineASX engine, int saleCost, Faction sellerFaction, Faction buyerFaction)
		{
			int num = saleCost;
			if (sellerFaction != null && buyerFaction != null)
			{
				num = sellerFaction.GetMarkedUpPriceAfterOpinionChange(TradeType.Sell, saleCost, buyerFaction);
			}
			return Maths.RoundUpToInt(num, engine.EconomySettings.ShipSaleRounding);
		}

		public static string GetUnitClassification(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Wormhole:
				return "Wormhole";
			case UnitType.Asteroid:
				return "Asteroid";
			case UnitType.Cargo:
				return "Cargo";
			default:
				if (unit.IsStationOrShip())
				{
					if (unit.UnitClass.IsFreighter)
					{
						return "Freighter";
					}
					return Enum.GetName(typeof(ShipHullType), unit.UnitClass.HullType);
				}
				return "-";
			}
		}

		public void ShowShipInfo()
		{
			if (CurrentItem != null)
			{
				UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(CurrentItem.UnitClass.UnitPrefab);
			}
		}

		protected override void awake()
		{
			base.awake();
			BuyButton.onClick.AddListener(TryBuyWithoutEnter);
			BuyAndEnterShipButton.onClick.AddListener(TryBuyAndEnter);
			ShowInfoButton.onClick.AddListener(ShowShipInfo);
			DockShipsToggle.gameObject.SetActive(value: true);
			DockShipsToggle.isOn = true;
			BuyButton.interactable = false;
			BuyAndEnterShipButton.interactable = false;
			QuantitySlider.onValueChanged.AddListener((float value) =>
			{
				RefreshCurentItemData();
			});
			CustomizeButton.onClick.AddListener(CustomizeButtonClick);
		}

		protected override void update()
		{
			base.update();
			if (ShipTraderUnit == null)
			{
				return;
			}
			if (BuyButton != null)
			{
				BuyButton.interactable = CurrentItem != null && CanAffordPurchase();
			}
			if (BuyAndEnterShipButton != null)
			{
				BuyAndEnterShipButton.interactable = CurrentItem != null && BuyQuantity == 1 && currentItem.UnitClass.IsPilottable && CanAffordPurchase() && WorldHelper.CanEnterUnitAtPosition(ShipTraderUnit.Sector, ShipTraderUnit.SectorPosition);
			}
			if (currentItem != null)
			{
				TotalCostText.color = (CanAffordPurchase() ? Eng.GameSettings.AffordableColor : Eng.GameSettings.UnaffordableColor);
				bool flag = UnitAbleToDockUnit(ShipTraderUnit, currentItem.UnitClass);
				RefreshTextWhenaffordableChanges();
				DockShipsToggle.interactable = flag && CurrentUnitCanDockUnit(ShipTraderUnit, currentItem.UnitClass, BuyQuantity);
				if (!DockShipsToggle.interactable)
				{
					DockShipsToggle.isOn = false;
				}
				CreateFleetToggle.interactable = BuyQuantity > 1;
				if (!CreateFleetToggle.interactable)
				{
					CreateFleetToggle.isOn = false;
				}
			}
		}

		private void RefreshTextWhenaffordableChanges()
		{
			bool flag = CanAffordSinglePurchase();
			if (!wasAffordable.HasValue || flag != wasAffordable.Value)
			{
				RefreshShipDealerText();
			}
			wasAffordable = flag;
		}

		private bool UnitAbleToDockUnit(Unit currentUnit, UnitClass buyUnitClass)
		{
			if (currentUnit != null && currentUnit.IsDockable)
			{
				return currentUnit.HasSameRootUnitAs(ShipTraderUnit);
			}
			return false;
		}

		private bool CurrentUnitCanDockUnit(Unit currentUnit, UnitClass buyUnitClass, int quantity)
		{
			UnitHangar hangar = currentUnit.GetHangar();
			if (hangar.GetBestUnoccupiedBay(buyUnitClass.HullType) != null)
			{
				return hangar.UnoccupiedBays.Count >= quantity;
			}
			return false;
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshCurentItemData();
		}

		private bool AbleToDockNewShip()
		{
			return CurrentUnitCanDockUnit(ShipTraderUnit, currentItem.UnitClass, BuyQuantity);
		}

		private bool TryBuy(bool enterShip)
		{
			if (DockShipsToggle.isOn)
			{
				if (AbleToDockNewShip())
				{
					return TryBuyInternal(enterShip, DockShipsToggle.isOn, CreateFleetToggle.isOn);
				}
				ShowNoFreeHangarsMessagePrompt(enterShip);
				return false;
			}
			return TryBuyInternal(enterShip, DockShipsToggle.isOn, CreateFleetToggle.isOn);
		}

		private void TryBuyWithoutEnter()
		{
			TryBuy(enterShip: false);
		}

		private void TryBuyAndEnter()
		{
			TryBuy(enterShip: true);
		}

		private void ShowNoFreeHangarsMessagePrompt(bool enter)
		{
			if (enter)
			{
				ShowNoFreeHangarsMessagePrompt(OnConfirmUndockNewShipWithEnter);
			}
			else
			{
				ShowNoFreeHangarsMessagePrompt(OnConfirmUndockNewShip);
			}
		}

		private void ShowNoFreeHangarsMessagePrompt(MessageBoxScreen.MessageBoxDismissedHandler callback)
		{
			UIController.Instance.ShowMessageBox("There are no free hangar bays for the new ship. Undock the ship?", MessageBoxButtons.OkCancel, callback, MessageBoxIcon.Warning, "Hangar Bays Occupied");
		}

		private void OnConfirmUndockNewShip(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				TryBuyInternal(enterShip: false, dockShips: false, CreateFleetToggle.isOn);
			}
		}

		private void OnConfirmUndockNewShipWithEnter(MessageBoxScreen sender, MessageBoxResult result)
		{
			if (result == MessageBoxResult.Ok)
			{
				TryBuyInternal(enterShip: true, dockShips: false, CreateFleetToggle.isOn);
			}
		}

		private bool TryBuyInternal(bool enterShip, bool dockShips, bool createFleet)
		{
			if (CanAffordPurchase())
			{
				Buy(enterShip, dockShips, createFleet);
				return true;
			}
			UIController.Instance.ShowInsufficientCreditsMessageBox();
			return false;
		}

		public void Buy(bool enterShip, bool docksShips, bool createFleet)
		{
			// Asked before anything is spent or built here.
			//
			// Buying creates a ship and spends money, and a copy of the world
			// that is not the one may do neither. So the host prices it,
			// charges it, builds it, and tells everybody - and the ship
			// arrives on this machine the same way an AI-built one does,
			// through the same NewUnitMessage, because the host's purchase
			// raises EngineASX.UnitCreated exactly as the AI's does.
			//
			// The request carries the dock and the class, never a price. A
			// price this copy worked out is not a price anybody can check.
			//
			// Returns false on a host and in singleplayer, where the purchase
			// happens here exactly as it always did.
			if (CurrentItem == null || CurrentItem.UnitClass == null || ShipTraderUnit == null)
			{
				return;
			}

			if (Authority.BuyShipRequest != null
				&& Authority.BuyShipRequest(
					ShipTraderUnit.UniqueId,
					CurrentItem.UnitClass.UniqueID,
					BuyQuantity))
			{
				return;
			}

			int totalCost = GetTotalCost();
			Debug.Log($"Player buying ship for a price of {totalCost}");
			RegisterTransaction(totalCost);
			List<Unit> list = new List<Unit>();
			for (int i = 0; i < BuyQuantity; i++)
			{
				Unit unit = SpawnUnit();
				unit.Sector = ShipTraderUnit.Sector;
				if (!docksShips || !AttemptToDockNewUnit(unit))
				{
					unit.TransformForUndockFrom(ShipTraderUnit);
				}
				if (!unit.UnitClass.IsPilottable)
				{
					unit.Components.UndockIfDocked();
				}
				list.Add(unit);
			}
			if (createFleet)
			{
				OrdersHelper.CreateFleetForUnits(ShipTraderUnit.Sector, list[0].SectorPosition, list);
			}
			if (enterShip && list.Count > 0)
			{
				Eng.LocalPlayer.Person.CurrentUnit = list[0];
			}
			AddMessages(list);
			if (OnShipBought != null)
			{
				OnShipBought(this, list);
			}
			Eng.SetUIFromPlayerStatus();
		}

		private bool AttemptToDockNewUnit(Unit newUnit)
		{
			return TryDockBoughtShipLogIfError(newUnit, ShipTraderUnit);
		}

		private bool TryDockBoughtShipLogIfError(Unit newUnit, Unit dock)
		{
			if (!newUnit.Components.TryDockInUnit(dock))
			{
				LogFailedDockAttempt(newUnit);
				return false;
			}
			return true;
		}

		private static void LogFailedDockAttempt(Unit newUnit)
		{
			Debug.LogError("Failed to dock bought ship", newUnit);
		}

		private static void AddMessages(List<Unit> newUnits)
		{
			if (newUnits.Count != 1)
			{
				return;
			}
			Unit unit = newUnits[0];
			if (unit.IsDocked)
			{
				if (!unit.IsPlayerCurrentUnit)
				{
					UIController.Instance.QuickMsg.AddMessage($"\"{unit.Components.ShipName}\" in Docking Bay of \"{unit.Components.DockUnit.GetFriendlyName()}\"");
				}
			}
			else if (!unit.IsPlayerCurrentUnit)
			{
				UIController.Instance.QuickMsg.AddMessage($"Your \"{unit.UnitClass.GetClassAndSeriesName()}\" is outside the station");
			}
		}

		private Unit SpawnUnit()
		{
			Unit component = UnityEngine.Object.Instantiate(CurrentItem.UnitClass.UnitPrefab.gameObject).GetComponent<Unit>();
			component.Init();
			component.Components.AutoAssignShipName();
			if (currentItem.CustomUnitVariant == null)
			{
				component.Components.InstallDefaultComponents();
				component.Components.AddDefaultCargoLoadout();
			}
			else
			{
				CustomUnitVariantHelper.InstallComponentsAddCargoAndRename(currentItem.CustomUnitVariant, component);
			}
			component.Faction = Eng.LocalPlayer.Faction;
			return component;
		}

		private void RegisterTransaction(int cost)
		{
			Eng.RegisterTaxedPlayerTrade(ShipTraderUnit, -cost, FactionTransactionType.ShipPurchase, null, currentItem.UnitClass, BuyQuantity);
		}

		private Sprite GetShipSprite()
		{
			if (currentItem != null)
			{
				return EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(currentItem.UnitClass);
			}
			return EngineASX.Instance.EngineResources.DefaultUnitClassRenderSprite;
		}

		private string GetShipDealerText()
		{
			if (currentItem != null)
			{
				string text = $"The ship dealer is offering the {GetItemName(currentItem)} for {TextFormattingHelper.FormatCredits(GetCurrentItemCostPerUnit())} credits each.";
				if (CanAffordSinglePurchase())
				{
					return text + "\n\nWould you like to buy it?";
				}
				return text + "\n\nYou cannot afford this ship";
			}
			return null;
		}

		private string GetItemName(ShipTraderItemWrapper item)
		{
			if (item.UnitClass.ShipType == ShipType.Normal)
			{
				return $"{currentItem.GetClassAndSeriesName()} class {GetUnitClassification(CurrentItem.UnitClass.UnitPrefab)}";
			}
			return currentItem.UnitClass.GetClassAndSeriesName();
		}

		private int GetCurrentItemCostPerUnit()
		{
			return currentItem.GetCost(ShipTraderUnit.Faction, Eng.LocalFaction);
		}

		private int GetTotalCost()
		{
			return GetCurrentItemCostPerUnit() * BuyQuantity;
		}

		private void RefreshCurentItemData()
		{
			DockShipsLabel.text = $"Dock bought ship(s) with {ShipTraderUnit.GetFriendlyName()}";
			RefreshText();
			RefreshSprite();
		}

		private void RefreshSprite()
		{
			ShipImage.sprite = GetShipSprite();
		}

		private void RefreshText()
		{
			RefreshShipDealerText();
			TotalCostText.text = GetTotalCostText();
		}

		private void RefreshShipDealerText()
		{
			ShipDealerText.text = GetShipDealerText();
		}

		private string GetTotalCostText()
		{
			return TextFormattingHelper.FormatCredits(GetTotalCost(), includeSuffix: true);
		}

		private bool CanAffordSinglePurchase()
		{
			return Eng.LocalPlayer.Credits >= GetCurrentItemCostPerUnit();
		}

		private bool CanAffordPurchase()
		{
			return Eng.LocalPlayer.Credits >= GetTotalCost();
		}

		private void CustomizeButtonClick()
		{
			if (currentItem != null)
			{
				if (currentItem.CustomUnitVariant == null)
				{
					currentItem.CustomUnitVariant = CustomUnitVariantHelper.CreateFromUnitClass(currentItem.UnitClass);
				}
				currentItem.CustomUnitVariant = currentItem.CustomUnitVariant.Clone();
				UIController.Instance.ScreenNavigator.ShowCreateCustomVariantScreen(currentItem.CustomUnitVariant, isNewItem: true, null, (CreateUnitVariantScreen screen) =>
				{
					screen.TransientVariant = true;
				});
			}
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (!navigatedForward)
			{
				Refresh();
			}
		}
	}
}
