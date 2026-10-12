using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public static class CargoTradeHelper
	{
		public static int GetUnitQuantity(Unit unit, CargoClass c)
		{
			if (unit != null && unit.CargoBayComponent != null)
			{
				return unit.CargoBayComponent.GetCountOf(c);
			}
			return 0;
		}

		public static int GetUnitFreeSpaceFor(Unit unit, CargoClass c)
		{
			if (unit != null && unit.CargoBayComponent != null)
			{
				return unit.CargoBayComponent.GetFreeSpaceFor(c);
			}
			return 0;
		}

		public static bool DockHasInfiniteOfCurrentCargoClass(Unit dockUnit, CargoClass cargoClass)
		{
			return dockUnit.Components.CargoTrader.HasInfiniteOf(cargoClass);
		}

		public static int GetMaxSellable(Unit playerUnit, Unit dockUnit, CargoClass cargoClass)
		{
			int unitQuantity = GetUnitQuantity(playerUnit, cargoClass);
			return dockUnit.Components.CargoTrader.GetMaxBuyable(unitQuantity, playerUnit.Faction, cargoClass);
		}

		public static int GetMaxBuyable(Unit playerUnit, Unit dockUnit, CargoClass cargoClass)
		{
			int freeSpaceFor = playerUnit.CargoBayComponent.GetFreeSpaceFor(cargoClass);
			int num = freeSpaceFor;
			if (!DockHasInfiniteOfCurrentCargoClass(dockUnit, cargoClass))
			{
				num = Mathf.Min(GetUnitQuantity(dockUnit, cargoClass), freeSpaceFor);
			}
			if (num > 0)
			{
				int price = 0;
				if (GetBuyPrice(playerUnit.Faction, dockUnit, cargoClass, 1, out price))
				{
					int b = ((price > 0) ? (playerUnit.Faction.Credits / price) : int.MaxValue);
					return Mathf.Min(num, b);
				}
			}
			return 0;
		}

		public static bool GetSellPrice(Faction playerFaction, Unit dockUnit, CargoClass cargoClass, int quantity, out int price)
		{
			if (dockUnit.Components.CargoTrader.GetBuyPrice(cargoClass, playerFaction, quantity, out price))
			{
				if (dockUnit.Faction == playerFaction && !dockUnit.Components.CargoTrader.HasInfiniteOf(cargoClass))
				{
					price = 0;
				}
				return true;
			}
			price = -1;
			return false;
		}

		public static bool GetBuyPrice(Faction playerFaction, Unit dockUnit, CargoClass cargoClass, int quantity, out int price)
		{
			if (dockUnit.Components.CargoTrader.GetSellPrice(cargoClass, playerFaction, quantity, out price))
			{
				if (dockUnit.Faction == playerFaction && !dockUnit.Components.CargoTrader.HasInfiniteOf(cargoClass))
				{
					price = 0;
				}
				return true;
			}
			price = -1;
			return false;
		}

		public static bool SellMax(Unit playerUnit, Unit dockUnit, CargoClass cargoClass)
		{
			int maxSellable = GetMaxSellable(playerUnit, dockUnit, cargoClass);
			if (maxSellable > 0)
			{
				int price = 0;
				if (GetSellPrice(playerUnit.Faction, dockUnit, cargoClass, maxSellable, out price))
				{
					int num = price * maxSellable;
					DoTransferToPlayerAndExchangeCredits(playerUnit, dockUnit, cargoClass, -maxSellable, -num);
					return true;
				}
			}
			return false;
		}

		public static bool BuyMax(Unit playerUnit, Unit dockUnit, CargoClass cargoClass)
		{
			int maxBuyable = GetMaxBuyable(playerUnit, dockUnit, cargoClass);
			if (maxBuyable > 0)
			{
				int price = 0;
				if (GetBuyPrice(playerUnit.Faction, dockUnit, cargoClass, maxBuyable, out price))
				{
					int costToPlayer = maxBuyable * price;
					DoTransferToPlayerAndExchangeCredits(playerUnit, dockUnit, cargoClass, maxBuyable, costToPlayer);
					return true;
				}
			}
			return false;
		}

		public static void DoTransferToPlayerAndExchangeCredits(Unit playerUnit, Unit dockUnit, CargoClass cargoClass, int quantity, int costToPlayer)
		{
			DoTransferToPlayer(playerUnit, dockUnit, cargoClass, quantity);
			if (costToPlayer != 0)
			{
				playerUnit.Engine.RegisterPlayerTrade(dockUnit, -costToPlayer, FactionTransactionType.Trade, cargoClass, null, quantity);
			}
		}

		public static void DoTransferToPlayer(Unit playerUnit, Unit dockUnit, CargoClass cargoClass, int quantity)
		{
			playerUnit.Engine.TransferToPlayerCargoWithMsg(playerUnit, cargoClass, quantity, ignoreCapacity: true);
			if (!DockHasInfiniteOfCurrentCargoClass(dockUnit, cargoClass))
			{
				dockUnit.CargoBayComponent.AddToCargoIfFits(cargoClass, -quantity);
			}
		}

		public static int GetMaxPlayerQuantityIgnoreCredits(Unit playerUnit, Unit dockUnit, CargoClass cargoClass)
		{
			int a = 999;
			if (!DockHasInfiniteOfCurrentCargoClass(dockUnit, cargoClass))
			{
				a = GetUnitQuantity(dockUnit, cargoClass);
			}
			int price = 0;
			if (GetBuyPrice(playerUnit.Faction, dockUnit, cargoClass, 1, out price))
			{
				int unitFreeSpaceFor = GetUnitFreeSpaceFor(playerUnit, cargoClass);
				return Mathf.Min(a, unitFreeSpaceFor);
			}
			return 0;
		}
	}
}
