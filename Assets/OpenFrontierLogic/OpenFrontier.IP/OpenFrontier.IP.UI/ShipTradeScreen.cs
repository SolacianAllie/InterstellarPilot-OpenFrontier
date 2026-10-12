using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.ShipTrader;
using OpenFrontier.IP.billing;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ShipTradeScreen : EngineScreen
	{
		public Button CreateNewVariantButton;

		private ShipTraderItemWrapper buyingItem;

		public ShipTradeItemListUI ItemList;

		public ShipBuyScreen.OnShipBoughtHandler OnShipBoughtHandler;

		public Button ManageVariantsButton;

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

		public Faction ShipTraderFaction
		{
			get
			{
				Unit shipTraderUnit = ShipTraderUnit;
				if (shipTraderUnit != null)
				{
					return shipTraderUnit.Faction;
				}
				return null;
			}
		}

		public Unit ShipTraderUnit { get; set; }

		protected override void awake()
		{
			base.awake();
			CreateNewVariantButton.onClick.AddListener(CreateNewVariantButtonClick);
			ManageVariantsButton.onClick.AddListener(ManageVariantsButtonClick);
		}

		private void ManageVariantsButtonClick()
		{
			if (CustomUnitVariantIO.LoadCustomVariantShips().Any())
			{
				UIController.Instance.ScreenNavigator.ShowManageCustomUnitVariantsScreen();
				return;
			}
			UIController.Instance.ShowMessageBox("You have not created any variants. Create one now?", MessageBoxButtons.OkCancel, (MessageBoxScreen sender, MessageBoxResult result) =>
			{
				if (result == MessageBoxResult.Ok)
				{
					CreateNewVariant();
				}
			}, MessageBoxIcon.Question);
		}

		private void CreateNewVariantButtonClick()
		{
			CreateNewVariant();
		}

		private void CreateNewVariant()
		{
			UIController.Instance.ScreenNavigator.ShowCreateShipVariantWizard((from e in ItemList.ActiveItems
				where e.ShipTraderItem != null
				select e.UnitClass).ToList());
		}

		public ShipTraderItemWrapper ChangeShip(ShipTraderItemWrapper currentShipItem, int change)
		{
			if (ItemList.ActiveItems.Count > 0)
			{
				int index = Maths.WrapValue(ItemList.ActiveItems.IndexOf(currentShipItem) + change, 0, ItemList.ActiveItems.Count);
				return ItemList.ActiveItems[index];
			}
			return null;
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			Refresh();
		}

		protected override void refresh()
		{
			base.refresh();
			if (ShipTrader != null)
			{
				SetItems();
			}
		}

		public void AttemptToBuyShip(ShipTraderItemWrapper item)
		{
			buyingItem = item;
			IPProduct requiredProduct = item.UnitClass.RequiredProduct;
			if (!item.RequiresIap || Products.HasProduct(requiredProduct))
			{
				NavigateToBuyShip();
			}
			else if (item.RequiresIap)
			{
				NavigateToPurchaseGameScreen(item.UnitClass.RequiredProduct);
			}
		}

		private void NavigateToBuyShip()
		{
			UIController.Instance.ScreenNavigator.ShowBuyShipScreen(ShipTraderUnit, buyingItem, OnShipBoughtHandler);
			buyingItem = null;
		}

		private void NavigateToPurchaseGameScreen(IPProduct requestedProduct = null)
		{
			UIController.Instance.ScreenNavigator.ShowStoreScreen(requestedProduct);
		}

		private List<ShipTraderItemWrapper> GetItems()
		{
			List<ShipTraderItemWrapper> list = new List<ShipTraderItemWrapper>();
			if (ShipTrader != null)
			{
				ShipTrader.TrimDuplicates();
				foreach (UnitShipTraderItem shipItem in ShipTrader.GetShipItems())
				{
					if (shipItem.UnitClass != null)
					{
						if (shipItem.UnitClass.IsUsable && !shipItem.UnitClass.IsBarebonesShip)
						{
							list.Add(ShipTraderItemWrapper.FromShipTraderItem(shipItem));
						}
					}
					else
					{
						Debug.LogError($"UnitShipTraderItem has no UnitClass: {shipItem}", ShipTrader);
					}
				}
				try
				{
					foreach (CustomUnitVariant customVariant in CustomUnitVariantIO.LoadCustomVariantShips())
					{
						if (list.Any((ShipTraderItemWrapper e) => e.UnitClass.IsUsable && e.UnitClass.UnitSeries == customVariant.UnitClass.UnitSeries))
						{
							ShipTraderItemWrapper item = ShipTraderItemWrapper.FromCustomVariant(customVariant);
							list.Add(item);
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to load custom unit variants: " + ex.Message);
				}
			}
			return (from e in list
				orderby e.UnitClass.UnitSeries.DisplayOrder, e.GetCost(ShipTraderUnit.Faction, EngineASX.Instance.LocalFaction)
				select e).ToList();
		}

		private void SetItems()
		{
			ItemList.SetItems(GetItems());
		}
	}
}
