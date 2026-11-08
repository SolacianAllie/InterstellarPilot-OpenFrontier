using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CargoTraderPriceValidator
	{
		public void Validate(CargoTrader unitTrader)
		{
			foreach (CargoClass cargoClass in unitTrader.Unit.Engine.CargoClasses)
			{
				float buyPrice = 0f;
				float sellPrice = 0f;
				if (IsBuyPriceHigherThanSellPrice(unitTrader, cargoClass, out buyPrice, out sellPrice))
				{
					Debug.LogError($"{unitTrader.Unit.GetFriendlyName()}: buying cargo {cargoClass.ClassName} at greater price {buyPrice} than asking {sellPrice}. Will result in infinite money exploit", unitTrader);
				}
			}
		}

		private bool IsBuyPriceHigherThanSellPrice(CargoTrader unitTrader, CargoClass cargoClass, out float buyPrice, out float sellPrice)
		{
			buyPrice = 0f;
			sellPrice = 0f;
			if (unitTrader.GetBuyPriceMultiplier(cargoClass, null, 1, out buyPrice) && unitTrader.GetSellPriceMultiplier(cargoClass, null, 1, out sellPrice) && buyPrice > sellPrice)
			{
				return true;
			}
			return false;
		}
	}
}
