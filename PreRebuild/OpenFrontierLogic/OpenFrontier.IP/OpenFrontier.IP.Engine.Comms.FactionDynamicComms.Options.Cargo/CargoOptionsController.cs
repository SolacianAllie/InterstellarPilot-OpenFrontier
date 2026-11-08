using System.Collections.Generic;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Cargo
{
	public static class CargoOptionsController
	{
		public static void PopulateOptions(DynamicCommsHandler commsHandler, List<ICommsStageOption> stageOptionCache)
		{
			CargoBayComponent targetCargoBay = GetTargetCargoBay(commsHandler);
			CargoBayComponent localCargoBay = GetLocalCargoBay(commsHandler);
			FactionAIBase factionAIBase = null;
			if (commsHandler.OwnerFaction != null)
			{
				factionAIBase = commsHandler.OwnerFaction.FactionAI;
			}
			if (targetCargoBay != null && localCargoBay != null && factionAIBase != null && factionAIBase.WillTradeWith(EngineASX.Instance.LocalFaction))
			{
				new DynamicCommsStage
				{
					DynamicCommsHandler = commsHandler,
					Message = null
				};
				CommsStageOptionGroup optionGroup = new CommsStageOptionGroup
				{
					DisplayText = "Cargo..."
				};
				List<CommsTradeCargoItem> cargoItemsToSellToTarget = GetCargoItemsToSellToTarget(localCargoBay, targetCargoBay, factionAIBase);
				if (cargoItemsToSellToTarget.Count > 0)
				{
					CargoTradeOption item = new CargoTradeOption
					{
						OptionText = "Sell Cargo...",
						OptionGroup = optionGroup,
						CargoItems = cargoItemsToSellToTarget,
						AITradeType = TradeType.Buy
					};
					stageOptionCache.Add(item);
				}
				List<CommsTradeCargoItem> cargoItemsToBuyFromTarget = GetCargoItemsToBuyFromTarget(localCargoBay, targetCargoBay, factionAIBase);
				if (cargoItemsToBuyFromTarget.Count > 0)
				{
					CargoTradeOption item2 = new CargoTradeOption
					{
						OptionText = "Buy Cargo...",
						OptionGroup = optionGroup,
						CargoItems = cargoItemsToBuyFromTarget,
						AITradeType = TradeType.Sell
					};
					stageOptionCache.Add(item2);
				}
			}
		}

		private static CargoBayComponent GetTargetCargoBay(DynamicCommsHandler commsHandler)
		{
			if (commsHandler.OwnerPilot != null && commsHandler.OwnerPilot.IsPilot)
			{
				Unit currentUnit = commsHandler.OwnerPilot.CurrentUnit;
				if (currentUnit != null && currentUnit.Faction == commsHandler.OwnerPilot.Faction && currentUnit.Components != null && currentUnit.Components.CargoBayComponent != null)
				{
					return currentUnit.Components.CargoBayComponent;
				}
			}
			return null;
		}

		private static CargoBayComponent GetLocalCargoBay(DynamicCommsHandler commsHandler)
		{
			Unit localUnit = EngineASX.Instance.LocalUnit;
			if (localUnit != null && localUnit.IsPilottedByPlayer())
			{
				return localUnit.CargoBayComponent;
			}
			return null;
		}

		private static bool CanTradeCargoClass(CargoClass cargoClass)
		{
			return !cargoClass.IsReserved;
		}

		private static List<CommsTradeCargoItem> GetCargoItemsToSellToTarget(CargoBayComponent localCargoBay, CargoBayComponent targetCargoBay, FactionAIBase targetFactionAI)
		{
			List<CommsTradeCargoItem> list = new List<CommsTradeCargoItem>();
			foreach (CargoClass cargoClass in localCargoBay.CargoClasses)
			{
				if (!CanTradeCargoClass(cargoClass))
				{
					continue;
				}
				int countOf = localCargoBay.GetCountOf(cargoClass);
				int? resultingMaxQuantityToBuy = 0;
				int price = 0;
				if (FactionAICargoBuySellCheck.FactionWillBuyItem(targetFactionAI, targetCargoBay.Unit, cargoClass, countOf, EngineASX.Instance.LocalFaction, out price, out resultingMaxQuantityToBuy))
				{
					if (cargoClass.IsEquipment)
					{
						price = Mathf.CeilToInt((float)price * 1.8f);
					}
					int num = targetFactionAI.Faction.Credits / price;
					int num2 = Mathf.Min(targetCargoBay.GetFreeSpaceFor(cargoClass), localCargoBay.GetCountOf(cargoClass), num);
					if (resultingMaxQuantityToBuy.HasValue)
					{
						num2 = Mathf.Min(resultingMaxQuantityToBuy.Value, num2);
					}
					if (num2 > 0)
					{
						CommsTradeCargoItem item = new CommsTradeCargoItem
						{
							Quantity = num2,
							CargoClass = cargoClass,
							Price = price
						};
						list.Add(item);
					}
				}
			}
			return list;
		}

		private static List<CommsTradeCargoItem> GetCargoItemsToBuyFromTarget(CargoBayComponent localCargoBay, CargoBayComponent targetCargoBay, FactionAIBase targetFactionAI)
		{
			List<CommsTradeCargoItem> list = new List<CommsTradeCargoItem>();
			foreach (CargoClass cargoClass in targetCargoBay.CargoClasses)
			{
				if (!CanTradeCargoClass(cargoClass))
				{
					continue;
				}
				int? maxQuantity = 0;
				int price = 0;
				if (FactionAICargoBuySellCheck.FactionWillSellItem(targetFactionAI, targetCargoBay.Unit, cargoClass, EngineASX.Instance.LocalFaction, out price, out maxQuantity))
				{
					int num = EngineASX.Instance.LocalFaction.Credits / price;
					int num2 = Mathf.Min(localCargoBay.GetFreeSpaceFor(cargoClass), targetCargoBay.GetCountOf(cargoClass), num);
					if (maxQuantity.HasValue)
					{
						num2 = Mathf.Min(maxQuantity.Value, num2);
					}
					if (num2 > 0)
					{
						CommsTradeCargoItem item = new CommsTradeCargoItem
						{
							Quantity = num2,
							CargoClass = cargoClass,
							Price = price
						};
						list.Add(item);
					}
				}
			}
			return list;
		}
	}
}
