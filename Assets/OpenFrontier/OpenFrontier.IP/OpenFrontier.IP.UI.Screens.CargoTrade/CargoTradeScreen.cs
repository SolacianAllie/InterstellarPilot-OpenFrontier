using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.MessageBox;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoTrade
{
	public class CargoTradeScreen : EngineScreen
	{
		public enum SortPriceMode
		{
			Name,
			BuyPrice,
			SellPrice,
			ShipQuantity,
			DockQuantity
		}

		public enum TraderFilterCargoType
		{
			All,
			Cargo,
			Ammo
		}

		public Button CargoTradeWarningButton;

		public bool InvalidateWhenUnitsNull;

		public Text TradingShipLabel;

		public Text TitleLabel;

		public Toggle ShowCompatibleAmmoOnlyToggle;

		public AdvancedCargoUsageSlider PlayerCargoUsageSlider;

		public AdvancedCargoUsageSlider DockCargoUsageSlider;

		public TraderFilterCargoType CargoTypeFilter;

		private Dictionary<SortPriceMode, TradeMenuColumnHeader> columnHeaders = new Dictionary<SortPriceMode, TradeMenuColumnHeader>();

		private Unit dockedUnit;

		private Unit dockUnit;

		public Button FilterModeButton;

		public Text FilterModeLabel;

		private bool isStale = true;

		public TradeItemListUI ItemList;

		public bool ShowAllCargos = true;

		public int SortDirection = -1;

		public SortPriceMode SortMode = SortPriceMode.ShipQuantity;

		private CargoTrader unitTrader;

		public Unit DockedUnit
		{
			get
			{
				return dockedUnit;
			}
			set
			{
				if (dockedUnit != value)
				{
					Unit unit = dockedUnit;
					dockedUnit = value;
					if (unit != null)
					{
						unit.CargoBayComponent.CargoAddRemove -= CargoBayComponent_CargoAddRemove;
					}
					if (dockedUnit != null)
					{
						dockedUnit.CargoBayComponent.CargoAddRemove += CargoBayComponent_CargoAddRemove;
					}
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
					Unit unit = dockUnit;
					dockUnit = value;
					if (unit != null)
					{
						unit.CargoBayComponent.CargoAddRemove -= CargoBayComponent_CargoAddRemove;
					}
					if (dockUnit != null)
					{
						dockUnit.CargoBayComponent.CargoAddRemove += CargoBayComponent_CargoAddRemove;
					}
				}
			}
		}

		public CargoTrader UnitTrader => unitTrader;

		public bool IsStale
		{
			get
			{
				return isStale;
			}
			set
			{
				isStale = value;
			}
		}

		public void RegisterColumnHeader(SortPriceMode sortMode, TradeMenuColumnHeader t)
		{
			columnHeaders[sortMode] = t;
		}

		public int GetBuyPrice(CargoClass cargoClass)
		{
			int price = -1;
			if (GetBuyPrice(cargoClass, 1, out price))
			{
				return price;
			}
			return -1;
		}

		public bool GetBuyPrice(CargoClass cargoClass, out int price)
		{
			return GetBuyPrice(cargoClass, 1, out price);
		}

		public bool GetBuyPrice(CargoClass cargoClass, int quantity, out int price)
		{
			return CargoTradeHelper.GetBuyPrice(Eng.LocalFaction, DockUnit, cargoClass, quantity, out price);
		}

		public int GetSellPrice(CargoClass cargoClass)
		{
			int price = -1;
			if (GetSellPrice(cargoClass, 1, out price))
			{
				return price;
			}
			return -1;
		}

		public bool GetSellPrice(CargoClass cargoClass, out int price)
		{
			return GetSellPrice(cargoClass, 1, out price);
		}

		public bool GetSellPrice(CargoClass cargoClass, int quantity, out int price)
		{
			return CargoTradeHelper.GetSellPrice(Eng.LocalFaction, DockUnit, cargoClass, quantity, out price);
		}

		public bool IsCargoClassCompatible(CargoClass cargoClass)
		{
			if (cargoClass == null)
			{
				throw new NullReferenceException("cargoClass");
			}
			if (cargoClass.IsEquipment)
			{
				return dockedUnit.Components.IsCargoClassCompatibleWithAnyComponent(cargoClass);
			}
			return true;
		}

		public void SortItems(SortPriceMode sortMode)
		{
			if (SortMode != sortMode)
			{
				SortMode = sortMode;
			}
			else
			{
				SortDirection *= -1;
			}
			Refresh();
		}

		public CargoClass SwitchCargo(CargoClass curCargoClass, int movement)
		{
			int num = ItemList.ActiveItems.IndexOf(curCargoClass);
			num += movement;
			num = Maths.WrapValue(num, 0, ItemList.ActiveItems.Count);
			if (num >= 0 && num < ItemList.ActiveItems.Count)
			{
				return ItemList.ActiveItems[num];
			}
			return null;
		}

		protected override void awake()
		{
			if (FilterModeButton != null)
			{
				FilterModeButton.onClick.AddListener(ChangeFilterMode);
			}
			ShowCompatibleAmmoOnlyToggle.onValueChanged.AddListener(ShowCompatibleAmmoOnlyToggleValueChanged);
			RefreshFilterModeLabel();
			CargoTradeWarningButton.onClick.AddListener(CargoTradeWarningButtonClick);
			base.awake();
		}

		protected override void start()
		{
			base.start();
			PositionFilterGraphic();
		}

		protected override void update()
		{
			base.update();
			if (IsInvalid())
			{
				if (InvalidateWhenUnitsNull)
				{
					UIController.Instance.QuickMsg.AddMessage("Lost contact with target");
					Eng.SetUIFromPlayerStatus();
				}
				return;
			}
			ItemList.Tick();
			if (isStale)
			{
				Refresh();
				isStale = false;
			}
			if (dockUnit != null && dockedUnit != null)
			{
				DockCargoUsageSlider.SetUnitAndRefresh(dockUnit);
				PlayerCargoUsageSlider.SetUnitAndRefresh(dockedUnit);
			}
		}

		private bool IsInvalid()
		{
			if (!(dockUnit == null) && !(dockedUnit == null))
			{
				return dockedUnit.GetDockUnit() != dockUnit;
			}
			return true;
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshTitleLabel();
			RefreshTradingShipLabel();
			if (DockUI != null && dockUnit != null && DockedUnit != null)
			{
				unitTrader = dockUnit.GetComponent<CargoTrader>();
				if (unitTrader != null)
				{
					ItemList.Refresh();
				}
			}
			CargoTradeWarningButton.gameObject.SetActive(ShouldShowLowCreditsWarning());
		}

		private void RefreshTradingShipLabel()
		{
			TradingShipLabel.text = GetTradingShipLabelText();
		}

		private string GetTradingShipLabelText()
		{
			if (dockedUnit != null)
			{
				return $"\"{dockedUnit.Components.ShipName}\" Cargo";
			}
			return "Ship Cargo";
		}

		private void RefreshTitleLabel()
		{
			TitleLabel.text = GetTitleLabelText();
		}

		private string GetTitleLabelText()
		{
			if (dockUnit != null)
			{
				return $"Cargo Trade with {UnitNamer.GetNameAndFactionLongNameInParenthesisForPlayer(dockUnit)}";
			}
			return "Cargo Trade";
		}

		protected override void onDestroy()
		{
			base.onDestroy();
			DockedUnit = null;
			DockUnit = null;
		}

		private void RefreshFilterModeLabel()
		{
			if (FilterModeLabel != null)
			{
				FilterModeLabel.text = GetFilterModeLabel();
			}
		}

		private string GetFilterModeLabel()
		{
			return "Showing " + Enum.GetName(typeof(TraderFilterCargoType), CargoTypeFilter);
		}

		[ContextMenu("Position filter graphic")]
		private void PositionFilterGraphic()
		{
			if (columnHeaders.ContainsKey(SortMode))
			{
				columnHeaders[SortMode].PositionFilterGraphic();
			}
			else
			{
				Debug.LogWarning("Could not find column header: " + SortMode);
			}
		}

		private void ChangeFilterMode()
		{
			CargoTypeFilter = (TraderFilterCargoType)Maths.WrapValue((int)(CargoTypeFilter + 1), 0, 3);
			RefreshFilterModeLabel();
			Refresh();
			ItemList.ResetScrollPosition();
		}

		private void ShowCompatibleAmmoOnlyToggleValueChanged(bool value)
		{
			Refresh();
			ItemList.ResetScrollPosition();
		}

		private void CargoBayComponent_CargoAddRemove(CargoBayComponent cargoBay, CargoClass cargoClass, int quantityChange)
		{
			isStale = true;
		}

		private bool ShouldShowLowCreditsWarning()
		{
			if (unitTrader.Unit.Faction != EngineASX.Instance.LocalFaction)
			{
				return unitTrader.Unit.Faction.Credits < 5000;
			}
			return false;
		}

		private void CargoTradeWarningButtonClick()
		{
			UIController.Instance.ShowMessageBox("The trader is currently low on credits - it may not be possible to sell to the trader at this time", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning, "Low trader credits");
		}
	}
}
