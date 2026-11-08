using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine
{
	public static class ComponentTradeHelper
	{
		public static int GetComponentClassSaleCost(EngineASX engine, ComponentClass componentClass, Faction sellerFaction, Faction buyerFaction)
		{
			int actualCost = componentClass.GetActualCost();
			int num = actualCost;
			if (sellerFaction != null && buyerFaction != null)
			{
				num = sellerFaction.GetMarkedUpPriceAfterOpinionChange(TradeType.Sell, actualCost, buyerFaction);
			}
			return Maths.RoundUpToInt(num, 25);
		}

		public static int GetComponentClassBuyCost(EngineASX engine, ComponentClass componentClass, Faction sellerFaction, Faction buyerFaction)
		{
			int actualCost = componentClass.GetActualCost();
			int num = actualCost;
			if (buyerFaction != null && sellerFaction != null)
			{
				num = buyerFaction.GetMarkedUpPriceAfterOpinionChange(TradeType.Buy, actualCost, sellerFaction);
			}
			return Maths.RoundUpToInt(num, 25);
		}

		public static int GetComponentBuyCost(EngineASX engine, ComponentBase component, Faction sellerFaction, Faction buyerFaction)
		{
			int num = component.CalculateMoneyValue();
			int num2 = num;
			if (buyerFaction != null && sellerFaction != null)
			{
				num2 = buyerFaction.GetMarkedUpPriceAfterOpinionChange(TradeType.Buy, num, sellerFaction);
			}
			return Maths.RoundUpToInt(num2, 25);
		}

		public static int AdjustComponentPrice(int price)
		{
			if (price <= 0)
			{
				return 0;
			}
			return Maths.RoundUpToInt((float)price * EngineASX.Instance.EconomySettings.EquipmentCostMultiplier, 25);
		}

		public static int AdjustComponentPrice(int price, EngineEconomySettings engineEconomySettings)
		{
			if (price <= 0)
			{
				return 0;
			}
			return Maths.RoundUpToInt((float)price * engineEconomySettings.EquipmentCostMultiplier, 25);
		}
	}
}
