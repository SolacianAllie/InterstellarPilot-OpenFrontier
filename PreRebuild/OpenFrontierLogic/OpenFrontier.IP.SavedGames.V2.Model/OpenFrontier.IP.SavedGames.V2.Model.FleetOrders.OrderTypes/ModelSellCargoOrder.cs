using System.Collections.Generic;
using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.OrderTypes
{
	public class ModelSellCargoOrder : ModelFleetOrder
	{
		public int FreeUnitsCompleteThreshold { get; set; }

		public float MinBuyPriceMultiplier { get; set; }

		public bool SellOnlyListedCargos { get; set; }

		public bool CompleteWhenNoBuyerFound { get; set; }

		public bool CompleteWhenNoCargoToSell { get; set; }

		public ModelUnit ManualBuyerUnit { get; set; }

		public float CustomSellCargoTime { get; set; }

		public List<ModelCargoClass> SellCargoClasses { get; set; } = new List<ModelCargoClass>();

		public bool SellEquipment { get; set; }

		public override FleetOrderType OrderType => FleetOrderType.SellCargo;
	}
}
