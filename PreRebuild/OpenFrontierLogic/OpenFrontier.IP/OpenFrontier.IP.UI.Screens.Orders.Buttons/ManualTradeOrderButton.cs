using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.CargoPicker;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.SectorMap;
using OpenFrontier.IP.UI.Screens.UniverseMap;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public class ManualTradeOrderButton : OrderButton
	{
		private Unit buyLocation;

		private Unit sellLocation;

		protected override void OnButtonClick()
		{
			base.OnButtonClick();
			ShowUniverseMap("Select buy location sector", BuyLocationSectorPicked);
		}

		private void ShowUniverseMap(string title, Action<UniverseMapScreen, Sector> callback)
		{
			UIController.Instance.ScreenNavigator.ShowCustomUniverseMapScreen((UniverseMapScreen universeMap) =>
			{
				universeMap.Title = title;
				universeMap.AllowSectorSelection = true;
				universeMap.EnabledSectors = OrdersHelper.GetPlayerUniverseMapPickableSectors();
				universeMap.ShowSelectedSectorInfo = false;
				universeMap.SectorSelectedCallback = callback;
				universeMap.SetSectorFromOrderTarget(OrderTarget);
				universeMap.RestrictNavigationAway();
			});
		}

		private void BuyLocationSectorPicked(UniverseMapScreen sender, Sector selectedSector)
		{
			if (selectedSector != null)
			{
				ShowSectorMap(selectedSector, "Select buy location", BuyLocationSectorMapItemPicked, (Unit unit) => unit.CargoTrader != null && unit.CargoTrader.SoldTradableCargoClasses.Count > 0);
			}
			else
			{
				OnOrderCancelled();
			}
		}

		private void SellLocationSectorPicked(UniverseMapScreen sender, Sector selectedSector)
		{
			if (selectedSector != null)
			{
				ShowSectorMap(selectedSector, "Select sell location", SellLocationSectorMapItemPicked, (Unit unit) => unit.CargoTrader != null && unit.CargoTrader.BoughtTradableCargoClasses.Any((int cargoClassId) => buyLocation.CargoTrader.IsSellerOf(EngineASX.Instance.GetCargoClassById(cargoClassId))));
			}
			else
			{
				OnOrderCancelled();
			}
		}

		private void ShowSectorMap(Sector selectedSector, string title, Action<SectorMapForm, SectorMapSelectionItem> callback, Func<Unit, bool> selectionPredicate)
		{
			UIController.Instance.ScreenNavigator.ShowSectorMapScreen(selectedSector, (SectorMapScreen sectorMap) =>
			{
				sectorMap.SectorMapForm.Title = title;
				sectorMap.SectorMapForm.AllowSelectionConfirm = true;
				sectorMap.SectorMapForm.SectorMap.CustomUnitDisplayFilter = (Unit unit) => OpenFrontier.IP.UI.Screens.SectorMap.SectorMap.AlwaysShowUnitOnMap(unit) || OrderTarget.IsUnitOrdered(unit) || (OrdersHelper.CanOrderDockAtTarget(OrderTarget, unit) && selectionPredicate(unit));
				sectorMap.SectorMapForm.SectorMap.CustomSelectionFilter = (SectorMapSelectionItem item) => item.Unit != null && OrdersHelper.CanOrderDockAtTarget(OrderTarget, item.Unit) && item.Unit.Components.CargoTrader != null && selectionPredicate(item.Unit);
				sectorMap.SectorMapForm.SelectedCallback = callback;
				sectorMap.RestrictNavigationAway();
			});
		}

		private void BuyLocationSectorMapItemPicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			Unit unit = selected.Unit;
			if (unit != null)
			{
				buyLocation = unit;
				ShowUniverseMap("Select sell location sector", SellLocationSectorPicked);
			}
			else
			{
				OnOrderCancelled();
			}
		}

		private void SellLocationSectorMapItemPicked(SectorMapForm sender, SectorMapSelectionItem selected)
		{
			Unit unit = selected.Unit;
			if (unit != null)
			{
				sellLocation = unit;
				List<CargoPickerItem> list = new List<CargoPickerItem>();
				foreach (int soldTradableCargoClass in buyLocation.Components.CargoTrader.SoldTradableCargoClasses)
				{
					if (sellLocation.CargoTrader.BoughtTradableCargoClasses.Contains(soldTradableCargoClass))
					{
						CargoClass cargoClassById = EngineASX.Instance.GetCargoClassById(soldTradableCargoClass);
						if (cargoClassById != null)
						{
							list.Add(new CargoPickerItem
							{
								CargoClass = cargoClassById,
								MaxQuantity = buyLocation.GetCargoCountOf(cargoClassById)
							});
						}
					}
				}
				if (list.Count > 0)
				{
					UIController.Instance.ScreenNavigator.ShowCargoPickerScreen(list, CargoPickerItemPicked, "Select cargo to trade....", keepInNavigationStack: false, (CargoPickerScreen screen) =>
					{
						screen.AllowPickQuantity = false;
					});
				}
				else
				{
					OnOrderCancelled();
					UIController.Instance.ShowMessageBox("There is no cargo that can be traded", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				}
			}
			else
			{
				OnOrderCancelled();
			}
		}

		private void CargoPickerItemPicked(CargoPickerScreen sender, CargoPickerResult result)
		{
			if (result != null && result.CargoClass != null)
			{
				OnOrderIssuing(out var stack);
				OrdersHelper.OrderManualTrade(OrderTarget, buyLocation, sellLocation, result.CargoClass, stack);
				OnOrderIssued();
			}
			else if (sender.IsCurrentScreen)
			{
				sender.NavigateBack();
			}
		}
	}
}
