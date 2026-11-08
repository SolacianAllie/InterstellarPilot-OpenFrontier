using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.billing;

namespace OpenFrontier.IP.UI.Screens.ShipTrader
{
	public class ShipTraderItemWrapper
	{
		public CustomUnitVariant CustomUnitVariant { get; set; }

		public UnitShipTraderItem ShipTraderItem { get; set; }

		public float SellMultiplier { get; set; } = 1f;

		public UnitClass UnitClass { get; set; }

		public bool RequiresIap
		{
			get
			{
				if (Products.IAPEnabled && UnitClass != null)
				{
					return UnitClass.RequiredProduct != null;
				}
				return false;
			}
		}

		public IPProduct RequiredProduct
		{
			get
			{
				if (Products.IAPEnabled && UnitClass != null)
				{
					return UnitClass.RequiredProduct;
				}
				return null;
			}
		}

		public static ShipTraderItemWrapper FromShipTraderItem(UnitShipTraderItem shipTraderItem)
		{
			return new ShipTraderItemWrapper
			{
				ShipTraderItem = shipTraderItem,
				UnitClass = shipTraderItem.UnitClass,
				SellMultiplier = shipTraderItem.SellMultiplier
			};
		}

		public static ShipTraderItemWrapper FromCustomVariant(CustomUnitVariant customUnitVariant)
		{
			return new ShipTraderItemWrapper
			{
				CustomUnitVariant = customUnitVariant,
				UnitClass = customUnitVariant.UnitClass
			};
		}

		public string GetClassAndSeriesName()
		{
			if (CustomUnitVariant != null)
			{
				return CustomUnitVariant.FullName;
			}
			return UnitClass.GetClassAndSeriesName();
		}

		public string GetDescription()
		{
			if (CustomUnitVariant != null)
			{
				return CustomUnitVariant.Description;
			}
			return UnitClass.Description;
		}

		public int GetCost(Faction sellerFaction, Faction buyerFaction)
		{
			if (CustomUnitVariant != null)
			{
				return ShipBuyScreen.GetItemSaleCostPerUnit(EngineASX.Instance, CustomUnitVariantHelper.CalculateMoneyValue(CustomUnitVariant), sellerFaction, buyerFaction);
			}
			return ShipBuyScreen.GetItemSaleCostPerUnit(EngineASX.Instance, UnitClass.UnitPrefab, sellerFaction, buyerFaction);
		}
	}
}
