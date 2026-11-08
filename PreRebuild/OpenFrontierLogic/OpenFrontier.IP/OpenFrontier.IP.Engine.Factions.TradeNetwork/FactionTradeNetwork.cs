using System.Collections.Generic;
using OpenFrontier.IP.Engine.AI.ActiveOrders;

namespace OpenFrontier.IP.Engine.Factions.TradeNetwork
{
	public class FactionTradeNetwork
	{
		public class TradeLocation
		{
			public int UnitId { get; set; }

			public Unit Unit { get; set; }

			public List<FactionTradeNetworkBuy> BuyingItems { get; set; } = new List<FactionTradeNetworkBuy>(4);
		}

		private ActiveAutonomousTradeOrder lockingTradeOrder;

		private Dictionary<int, TradeLocation> tradeLocationByFleetId = new Dictionary<int, TradeLocation>();

		private Dictionary<int, TradeLocation> tradeLocationsByTraderId = new Dictionary<int, TradeLocation>();

		public int GetQuantityOfCargoClassBeingBoughtAt(Fleet askingFleet, Unit traderLocation, CargoClass cargoClass)
		{
			if (tradeLocationsByTraderId.TryGetValue(traderLocation.UniqueId, out var value))
			{
				int num = 0;
				{
					foreach (FactionTradeNetworkBuy buyingItem in value.BuyingItems)
					{
						if (buyingItem.CargoClass == cargoClass && buyingItem.Fleet != askingFleet && buyingItem.Fleet != null)
						{
							num += buyingItem.BuyingQuantity;
						}
					}
					return num;
				}
			}
			return 0;
		}

		public void RegisterBuyingCargo(Fleet fleet, Unit traderLocation, CargoClass cargoClass, int buyingQuantity)
		{
			RemoveFleetBuyingCargo(fleet);
			TradeLocation value = null;
			if (!tradeLocationsByTraderId.TryGetValue(traderLocation.UniqueId, out value))
			{
				value = new TradeLocation
				{
					Unit = traderLocation,
					UnitId = traderLocation.UniqueId
				};
				tradeLocationsByTraderId[traderLocation.UniqueId] = value;
			}
			tradeLocationByFleetId[fleet.UniqueId] = value;
			value.BuyingItems.Add(new FactionTradeNetworkBuy
			{
				BuyingQuantity = buyingQuantity,
				CargoClass = cargoClass,
				Fleet = fleet
			});
		}

		public void RemoveFleetBuyingCargo(Fleet fleet)
		{
			if (tradeLocationByFleetId.TryGetValue(fleet.UniqueId, out var value))
			{
				for (int i = 0; i < value.BuyingItems.Count; i++)
				{
					if (value.BuyingItems[i].Fleet == fleet)
					{
						value.BuyingItems.RemoveAt(i);
						i--;
					}
				}
				if (value.BuyingItems.Count == 0)
				{
					tradeLocationsByTraderId.Remove(value.UnitId);
				}
			}
			tradeLocationByFleetId.Remove(fleet.UniqueId);
		}

		public bool IsLocked(ActiveAutonomousTradeOrder tradeOrder)
		{
			if (lockingTradeOrder != null && lockingTradeOrder != tradeOrder)
			{
				if (lockingTradeOrder.SearchOperation != null && !lockingTradeOrder.SearchOperation.HasFinished)
				{
					return true;
				}
				lockingTradeOrder = null;
			}
			return false;
		}

		internal void ObtainLock(ActiveAutonomousTradeOrder tradeOrder)
		{
			lockingTradeOrder = tradeOrder;
		}
	}
}
