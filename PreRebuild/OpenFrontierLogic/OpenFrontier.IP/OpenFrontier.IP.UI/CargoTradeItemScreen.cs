using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.CargoTrade;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class CargoTradeItemScreen : EngineScreen
	{
		public Image CargoIconImage;

		public Text VolumeText;

		public Text UnableToTradeText;

		public static CargoClass ShowItem;

		public Button AddCargoButton;

		public Text AvailableLabel;

		public Text BuyAllPriceLabel;

		public Button BuyMaxButton;

		public Button BuySellButton;

		public Text BuySellLabel;

		public CargoClass cargoClass;

		public AdvancedCargoUsageSlider PlayerCargoUsageSlider;

		public AdvancedCargoUsageSlider DockCargoUsageSlider;

		public Text CostLabel;

		public Text CurrentCargoTypeLabel;

		private Unit dockUnit;

		public Button InfoButton;

		private bool isSettingSliderManually;

		private int newPlayerQuantity;

		public Button NextCargoButton;

		private Unit playerUnit;

		public Button PreviousCargoButton;

		public Text PriceLabel;

		public Text QuantityChangeLabel;

		public Text QuantityLabel;

		public Slider QuantitySlider;

		public float QuantitySliderPower = 2f;

		public Button RemoveCargoButton;

		public Button SellMaxButton;

		public Text SellAllPriceLabel;

		public Button ShowPricesButton;

		private CargoTradeScreen tradeMenu;

		public CargoClass CargoClass
		{
			get
			{
				return cargoClass;
			}
			set
			{
				if (cargoClass != value)
				{
					cargoClass = value;
					ResetPlayerQuantity();
					ResetSliderPosition();
					RefreshVolumeLabel();
				}
			}
		}

		public bool IsBuying
		{
			get
			{
				if (CargoClass != null)
				{
					return newPlayerQuantity >= GetPlayerQuantity();
				}
				return false;
			}
		}

		public Unit DockedUnit
		{
			get
			{
				return playerUnit;
			}
			set
			{
				if (playerUnit != value)
				{
					_ = playerUnit;
					playerUnit = value;
				}
			}
		}

		public Unit DockUnit
		{
			get
			{
				return dockUnit;
			}
			set
			{
				if (dockUnit != value)
				{
					_ = dockUnit;
					dockUnit = value;
				}
			}
		}

		public int NewPlayerQuantity
		{
			get
			{
				return newPlayerQuantity;
			}
			set
			{
				if (newPlayerQuantity != value)
				{
					newPlayerQuantity = value;
					if (newPlayerQuantity < 0)
					{
						newPlayerQuantity = 0;
					}
				}
			}
		}

		public int BoughtQuantity
		{
			get
			{
				int num = newPlayerQuantity;
				int playerQuantity = GetPlayerQuantity();
				return num - playerQuantity;
			}
		}

		public int GetMaxPlayerQuantity()
		{
			return CargoTradeHelper.GetMaxPlayerQuantityIgnoreCredits(playerUnit, dockUnit, cargoClass);
		}

		public void SwitchCargo(int change)
		{
			if (tradeMenu != null)
			{
				CargoClass = tradeMenu.SwitchCargo(CargoClass, change);
				Refresh();
			}
		}

		public void RefreshBuySellButtonState()
		{
			string msg = "";
			bool flag = false;
			if (BoughtQuantity != 0)
			{
				flag = CanBuySell(out msg);
			}
			BuySellButton.interactable = (BoughtQuantity != 0) & flag;
			if (BoughtQuantity != 0)
			{
				bool flag2 = !flag && msg != null;
				UnableToTradeText.gameObject.SetActive(flag2);
				if (flag2)
				{
					UnableToTradeText.text = msg;
				}
			}
			else
			{
				UnableToTradeText.gameObject.SetActive(value: false);
			}
		}

		public int GetPlayerQuantityChange()
		{
			return newPlayerQuantity - GetPlayerQuantity();
		}

		public void RefreshQuantityAndCost()
		{
			if (CargoClass != null)
			{
				int num = newPlayerQuantity;
				QuantityLabel.text = num.ToString();
				int num2 = num - GetPlayerQuantity();
				RefreshQuantityChangeLabel(num2);
				AvailableLabel.text = DockUnit.Components.CargoTrader.GetCargoCountStr(CargoClass);
				if (IsBuying)
				{
					BuySellLabel.text = "Buy";
				}
				else
				{
					BuySellLabel.text = "Sell";
				}
				if (num2 != 0 && CanDoTransfer())
				{
					int price = 0;
					GetCurrentPrice(out price);
					PriceLabel.text = TextFormattingHelper.FormatCredits(price, includeSuffix: true);
					PriceLabel.color = Eng.GetPriceColor(IsBuying ? TradeType.Buy : TradeType.Sell, CargoClass, price);
					int cost = 0;
					GetCurrentCostToPlayer(out cost);
					CostLabel.text = TextFormattingHelper.FormatCredits(cost, includeSuffix: true);
					CostLabel.color = (CanPlayerAffordCost(cost) ? Eng.GameSettings.AffordableColor : Eng.GameSettings.UnaffordableColor);
				}
				else
				{
					CostLabel.text = "N/a";
					CostLabel.color = Color.white;
					PriceLabel.text = "-";
				}
			}
			else
			{
				PriceLabel.text = "-";
				QuantityChangeLabel.text = null;
				BuySellLabel.text = "Trade";
			}
		}

		public void RefreshCargoUsageSlider()
		{
			if (playerUnit != null && dockUnit != null && cargoClass != null && BoughtQuantity != 0)
			{
				float num = (float)BoughtQuantity * cargoClass.Volume;
				PlayerCargoUsageSlider.SetUnit(playerUnit);
				PlayerCargoUsageSlider.ExpectedCargoUsage = playerUnit.CargoBayComponent.Usage + num;
				PlayerCargoUsageSlider.Refresh();
				DockCargoUsageSlider.SetUnit(dockUnit);
				DockCargoUsageSlider.ExpectedCargoUsage = dockUnit.CargoBayComponent.Usage - num;
				DockCargoUsageSlider.Refresh();
			}
			else
			{
				PlayerCargoUsageSlider.SetUnitAndRefresh(playerUnit);
				DockCargoUsageSlider.SetUnitAndRefresh(dockUnit);
			}
		}

		public int GetMaxBuyable()
		{
			return CargoTradeHelper.GetMaxBuyable(playerUnit, dockUnit, cargoClass);
		}

		public int GetSliderCurrentQuantity()
		{
			float num = Mathf.Pow(Mathf.Abs(GetSliderValueNormalized()), QuantitySliderPower);
			if (QuantitySlider.value >= 0.5f)
			{
				int dockQuantity = GetDockQuantity();
				int freeSpaceFor = playerUnit.Components.CargoBayComponent.GetFreeSpaceFor(CargoClass);
				int num2 = Mathf.Min(dockQuantity, freeSpaceFor);
				return GetPlayerQuantity() + Mathf.Max(1, Mathf.RoundToInt(num * (float)num2));
			}
			int playerQuantity = GetPlayerQuantity();
			return playerQuantity - Mathf.RoundToInt(num * (float)playerQuantity);
		}

		public bool GetCurrentCostToPlayer(out int cost)
		{
			cost = 0;
			int price = 0;
			if (GetCurrentPrice(out price))
			{
				cost = (newPlayerQuantity - GetPlayerQuantity()) * price;
				return true;
			}
			return false;
		}

		public bool CanDoTransfer()
		{
			int cost = 0;
			return GetCurrentCostToPlayer(out cost);
		}

		public bool CanAffordSliderTransfer()
		{
			int cost = 0;
			if (GetCurrentCostToPlayer(out cost))
			{
				if (CanPlayerAffordCost(cost))
				{
					return CanTraderAffordCost(-cost);
				}
				return false;
			}
			return false;
		}

		public bool CanPlayerAffordCost(int cost)
		{
			return GetPlayerCredits() >= cost;
		}

		public bool CanTraderAffordCost(int cost)
		{
			return DockUI.PlayerDockUnit.Faction.Credits >= cost;
		}

		public int GetPlayerCredits()
		{
			return DockUI.Engine.LocalPlayer.Credits;
		}

		public float CargoUsageAfterTransfer()
		{
			float num = (float)BoughtQuantity * cargoClass.Volume;
			return playerUnit.Components.CargoBayComponent.Usage + num;
		}

		public float CargoUsage01AfterTransfer()
		{
			return CargoUsageAfterTransfer() / playerUnit.Components.CargoBayComponent.Capacity;
		}

		public bool CanBuySell(out string msg)
		{
			msg = null;
			int cost = 0;
			if (CargoClass != null)
			{
				int boughtQuantity = BoughtQuantity;
				if (boughtQuantity != 0)
				{
					if (GetCurrentCostToPlayer(out cost))
					{
						if (boughtQuantity > 0)
						{
							if (boughtQuantity > GetDockQuantity())
							{
								msg = "Insufficient stock";
								return false;
							}
							if (playerUnit.Components.CargoBayComponent.CanChangeCargo(CargoClass, boughtQuantity))
							{
								if (CanPlayerAffordCost(cost))
								{
									if (CanTraderAffordCost(-cost))
									{
										return true;
									}
									msg = "Station owner has insufficient credits";
								}
								else
								{
									msg = "Insufficient credits";
								}
							}
							else
							{
								msg = "Insufficient cargo space";
							}
							return false;
						}
						if (!CargoTradeHelper.DockHasInfiniteOfCurrentCargoClass(dockUnit, cargoClass) && !DockUnit.CargoBayComponent.CanChangeCargo(cargoClass, Mathf.Abs(boughtQuantity)))
						{
							msg = "Station owner has insufficient cargo space";
							return false;
						}
						if (!CargoTradeHelper.DockHasInfiniteOfCurrentCargoClass(dockUnit, cargoClass) && !CanTraderAffordCost(-cost))
						{
							msg = "Station owner has insufficient credits";
							return false;
						}
						return true;
					}
					msg = "Trader does not ";
					if (boughtQuantity > 0)
					{
						msg += "sell";
					}
					else
					{
						msg += "buy";
					}
				}
			}
			return false;
		}

		public int GetDockQuantity()
		{
			if (DockHasInfiniteOfCurrentCargoClass())
			{
				return 999;
			}
			return dockUnit.GetCargoCountOf(CargoClass);
		}

		private bool DockHasInfiniteOfCurrentCargoClass()
		{
			return dockUnit.Components.CargoTrader.HasInfiniteOf(CargoClass);
		}

		public int GetTotalQuantity()
		{
			return GetPlayerQuantity() + GetDockQuantity();
		}

		public int GetPlayerQuantity()
		{
			return GetPlayerQuantity(cargoClass);
		}

		public int GetPlayerQuantity(CargoClass c)
		{
			if (playerUnit != null)
			{
				return playerUnit.CargoBayComponent.GetCountOf(c);
			}
			return 0;
		}

		public int GetMaxSellable()
		{
			return CargoTradeHelper.GetMaxSellable(DockUI.PlayerCurrentUnit, DockUnit, cargoClass);
		}

		public void BuySell()
		{
			string msg = null;
			if (CanBuySell(out msg))
			{
				if (LogWrapper.LogMsgs)
				{
					Debug.Log("Playing buying / selling: " + CargoClass.ClassName);
				}
				int cost = 0;
				GetCurrentCostToPlayer(out cost);
				int quantity = newPlayerQuantity - GetPlayerQuantity();
				CargoTradeHelper.DoTransferToPlayerAndExchangeCredits(playerUnit, dockUnit, CargoClass, quantity, cost);
				OnBuyOrSell();
			}
			else if (msg != null)
			{
				UIController.Instance.QuickMsg.AddMessage(msg);
			}
		}

		private void OnBuyOrSell()
		{
			DockUI.PlayBuySellBeep();
			ResetPlayerQuantity();
			ResetSliderPosition();
			Refresh();
			if (DockedUnit != null)
			{
				DockedUnit.TryPlayCargoDoorAudio();
			}
		}

		public bool CanSellMax()
		{
			return GetMaxSellable() > 0;
		}

		public void SellMax()
		{
			CargoTradeHelper.SellMax(playerUnit, dockUnit, cargoClass);
			OnBuyOrSell();
		}

		public void BuyMax()
		{
			CargoTradeHelper.BuyMax(playerUnit, dockUnit, cargoClass);
			OnBuyOrSell();
		}

		protected override void awake()
		{
			base.awake();
			BuyMaxButton.onClick.AddListener(BuyMax);
			BuySellButton.onClick.AddListener(BuySell);
			SellMaxButton.onClick.AddListener(SellMax);
			QuantitySlider.onValueChanged.AddListener(QuantitySlider_ValueChanged);
			ShowPricesButton.onClick.AddListener(ShowPrices);
			InfoButton.onClick.AddListener(ShowCargoInfo);
			if (NextCargoButton != null)
			{
				NextCargoButton.onClick.AddListener(NextCargo);
			}
			if (PreviousCargoButton != null)
			{
				PreviousCargoButton.onClick.AddListener(PreviousCargo);
			}
			AddCargoButton.onClick.AddListener(AddCargo);
			RemoveCargoButton.onClick.AddListener(RemoveCargo);
			tradeMenu = UIController.Instance.ScreenNavigator.LoadedScreens.OfType<CargoTradeScreen>().FirstOrDefault();
		}

		protected override void update()
		{
			base.update();
			if (CargoClass != null && tradeMenu != null)
			{
				RefreshCargoUsageSlider();
				if (BuyMaxButton != null)
				{
					int maxBuyable = GetMaxBuyable();
					BuyMaxButton.interactable = maxBuyable > 0;
				}
				if (SellMaxButton != null)
				{
					SellMaxButton.interactable = CanSellMax();
				}
				if (CargoClass != null)
				{
					RefreshBuySellPrices();
					RefreshBuySellButtonState();
					RefreshQuantityAndCost();
				}
			}
		}

		protected override void onEnable()
		{
			base.onEnable();
			ResetSliderPosition();
		}

		protected override void refresh()
		{
			base.refresh();
			if (CargoClass != null)
			{
				RefreshItemNames();
				CargoIconImage.sprite = Eng.EngineResources.GetCargoSpriteOrDefault(cargoClass);
			}
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			DockedUnit = null;
			DockUnit = null;
		}

		private void ResetPlayerQuantity()
		{
			if (cargoClass != null)
			{
				NewPlayerQuantity = GetPlayerQuantity(cargoClass);
			}
			else
			{
				NewPlayerQuantity = 0;
			}
		}

		private void QuantitySlider_ValueChanged(float delta)
		{
			if (!isSettingSliderManually)
			{
				NewPlayerQuantity = GetSliderCurrentQuantity();
			}
		}

		private void RemoveCargo()
		{
			NewPlayerQuantity--;
			ResetSliderPosition();
		}

		private void AddCargo()
		{
			NewPlayerQuantity++;
			ResetSliderPosition();
		}

		private void PreviousCargo()
		{
			SwitchCargo(-1);
		}

		private void NextCargo()
		{
			SwitchCargo(1);
		}

		private void ShowCargoInfo()
		{
			UIController.Instance.ScreenNavigator.ShowCargoInfoScreen(CargoClass);
		}

		private void ShowPrices()
		{
			UIController.Instance.ScreenNavigator.ShowCargoPricesScreen(cargoClass);
		}

		private float GetSliderPosition()
		{
			if (cargoClass != null)
			{
				int maxPlayerQuantity = GetMaxPlayerQuantity();
				int playerQuantity = GetPlayerQuantity();
				if (newPlayerQuantity != playerQuantity)
				{
					if (newPlayerQuantity > playerQuantity)
					{
						if (maxPlayerQuantity <= playerQuantity)
						{
							return 0.5f;
						}
						return 0.5f + Mathf.Pow((float)(newPlayerQuantity - playerQuantity) / (float)(maxPlayerQuantity - playerQuantity), 1f / QuantitySliderPower) * 0.5f;
					}
					return Mathf.Pow((float)newPlayerQuantity / (float)playerQuantity, QuantitySliderPower) * 0.5f;
				}
			}
			return 0.5f;
		}

		private void RefreshVolumeLabel()
		{
			if (cargoClass != null)
			{
				VolumeText.text = $"{cargoClass.Volume:0.0}";
			}
			else
			{
				VolumeText.text = "-";
			}
		}

		private void ResetSliderPosition()
		{
			isSettingSliderManually = true;
			QuantitySlider.value = GetSliderPosition();
			isSettingSliderManually = false;
		}

		private void RefreshItemNames()
		{
			if (DockUI != null && CargoClass != null)
			{
				if (CurrentCargoTypeLabel != null)
				{
					CurrentCargoTypeLabel.text = CargoClass.ClassName;
				}
			}
			else if (CurrentCargoTypeLabel != null)
			{
				CurrentCargoTypeLabel.text = null;
			}
		}

		private void RefreshBuySellPrices()
		{
			int price = 0;
			if (tradeMenu.GetBuyPrice(CargoClass, GetMaxBuyable(), out price))
			{
				BuyAllPriceLabel.text = TextFormattingHelper.FormatCredits(price, includeSuffix: true);
				BuyAllPriceLabel.color = Eng.GetPriceColor(TradeType.Buy, CargoClass, price);
			}
			else
			{
				BuyAllPriceLabel.text = "-";
				BuyAllPriceLabel.color = Color.white;
			}
			int price2 = 0;
			int cargoCountOf = playerUnit.GetCargoCountOf(CargoClass);
			if (cargoCountOf > 0 && tradeMenu.GetSellPrice(CargoClass, cargoCountOf, out price2))
			{
				SellAllPriceLabel.text = TextFormattingHelper.FormatCredits(price2, includeSuffix: true);
				SellAllPriceLabel.color = Eng.GetPriceColor(TradeType.Sell, CargoClass, price2);
			}
			else
			{
				SellAllPriceLabel.text = "-";
				SellAllPriceLabel.color = Color.white;
			}
		}

		private void RefreshQuantityChangeLabel(int quantityChange)
		{
			if (quantityChange != 0)
			{
				if (quantityChange > 0)
				{
					QuantityChangeLabel.text = $"(+{quantityChange})";
					QuantityChangeLabel.color = Eng.GameSettings.AffordableColor;
				}
				else
				{
					QuantityChangeLabel.text = $"({quantityChange})";
					QuantityChangeLabel.color = Eng.GameSettings.UnaffordableColor;
				}
			}
			else
			{
				QuantityChangeLabel.text = null;
			}
		}

		private float GetSliderValueNormalized()
		{
			return (QuantitySlider.value - 0.5f) * 2f;
		}

		private bool GetCurrentPrice(out int price)
		{
			int quantity = Mathf.Abs(GetPlayerQuantityChange());
			if (IsBuying)
			{
				return tradeMenu.GetBuyPrice(CargoClass, quantity, out price);
			}
			return tradeMenu.GetSellPrice(CargoClass, quantity, out price);
		}
	}
}
