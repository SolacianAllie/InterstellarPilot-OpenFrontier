using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.CargoTrade;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class TradeItemListUI : ScrollList<CargoClass>
	{
		public CargoTradeScreen TradeMenuUI;

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<CargoClass> list = new List<CargoClass>();
			Unit dockUnit = TradeMenuUI.DockUnit;
			Unit dockedUnit = TradeMenuUI.DockedUnit;
			if (dockUnit != null && dockedUnit != null && dockUnit.CargoBayComponent != null && TradeMenuUI.UnitTrader != null)
			{
				float price = 0f;
				foreach (CargoClass item in TradeMenuUI.Eng.CargoClasses.Where((CargoClass e) => !e.IsReserved))
				{
					if (IsItemFiltered(item))
					{
						bool flag = dockedUnit.CargoBayComponent.HasCargo(item);
						dockUnit.CargoBayComponent.HasCargo(item);
						bool flag2 = TradeMenuUI.UnitTrader.HasSellPrice(item) || TradeMenuUI.UnitTrader.GetSellPriceMultiplier(item, TradeMenuUI.Eng.LocalPlayer.Faction, 1, out price);
						bool flag3 = TradeMenuUI.UnitTrader.HasBuyPrice(item) || (flag && TradeMenuUI.UnitTrader.GetBuyPriceMultiplier(item, TradeMenuUI.Eng.LocalPlayer.Faction, 1, out price));
						if (TradeMenuUI.ShowAllCargos | flag2 | flag3)
						{
							list.Add(item);
						}
					}
				}
				foreach (CargoClass item2 in dockedUnit.CargoBayComponent.Cargos.Select((KeyValuePair<CargoClass, int> e) => e.Key))
				{
					if (IsItemFiltered(item2) && !list.Contains(item2))
					{
						list.Add(item2);
					}
				}
				list = SortItems(list);
			}
			TradeMenuUI.IsStale = false;
			SetItems(list);
		}

		protected override bool DetermineIsStale()
		{
			return TradeMenuUI.IsStale;
		}

		protected override bool IsItemFiltered(CargoClass item)
		{
			if (item == null)
			{
				throw new NullReferenceException("item");
			}
			if (item.IsEquipment && TradeMenuUI.ShowCompatibleAmmoOnlyToggle.isOn && !TradeMenuUI.DockedUnit.Components.IsCargoClassCompatibleWithAnyComponent(item))
			{
				return false;
			}
			return TradeMenuUI.CargoTypeFilter switch
			{
				CargoTradeScreen.TraderFilterCargoType.Cargo => !item.IsEquipment, 
				CargoTradeScreen.TraderFilterCargoType.Ammo => item.IsEquipment, 
				_ => true, 
			};
		}

		private int GetEquipmentSortOrder(CargoClass cargoClass)
		{
			GameObject relatedPrefab = cargoClass.RelatedPrefab;
			if (relatedPrefab != null)
			{
				Projectile component = relatedPrefab.GetComponent<Projectile>();
				if (component != null)
				{
					if (relatedPrefab.GetComponent<Countermeasure>() != null)
					{
						return 1;
					}
					if (component.ProjectileClass.IsMine)
					{
						return 2;
					}
					if (component.GetComponent<Missile>() != null)
					{
						return 3;
					}
					return 4;
				}
			}
			return 0;
		}

		private List<CargoClass> SortItems(IEnumerable<CargoClass> items)
		{
			switch (TradeMenuUI.SortMode)
			{
			case CargoTradeScreen.SortPriceMode.Name:
				items = ((TradeMenuUI.SortDirection != 1) ? items.OrderByDescending((CargoClass e) => e.ClassName) : items.OrderBy((CargoClass e) => e.ClassName));
				break;
			case CargoTradeScreen.SortPriceMode.DockQuantity:
				items = items.OrderBy((CargoClass e) => TradeMenuUI.DockUI.PlayerCurrentUnit.Components.DockUnit.GetCargoCountOf(e) * TradeMenuUI.SortDirection);
				break;
			case CargoTradeScreen.SortPriceMode.ShipQuantity:
				items = from e in items
					orderby TradeMenuUI.DockUI.PlayerCurrentUnit.GetCargoCountOf(e) * TradeMenuUI.SortDirection, e.IsEquipment, e.IsDeployable, GetEquipmentSortOrder(e), e.ClassName
					select e;
				break;
			case CargoTradeScreen.SortPriceMode.BuyPrice:
				items = items.OrderBy((CargoClass e) => TradeMenuUI.GetBuyPrice(e) * TradeMenuUI.SortDirection);
				break;
			case CargoTradeScreen.SortPriceMode.SellPrice:
				items = items.OrderBy((CargoClass e) => TradeMenuUI.GetSellPrice(e) * TradeMenuUI.SortDirection);
				break;
			}
			return items.ToList();
		}
	}
}
