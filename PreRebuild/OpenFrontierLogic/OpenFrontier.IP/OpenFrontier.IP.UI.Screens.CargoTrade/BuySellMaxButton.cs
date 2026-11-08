using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoTrade
{
	public class BuySellMaxButton : MonoBehaviour
	{
		public TradeType PlayerTradeType = TradeType.Buy;

		public CargoClass CargoClass;

		public Unit DockUnit;

		public Unit PlayerUnit;

		private Button button;

		public void Awake()
		{
			button = GetComponent<Button>();
		}

		public void Update()
		{
			button.interactable = ShouldBeInteractable();
		}

		public void BuySellMax()
		{
			if (!ShouldBeInteractable())
			{
				return;
			}
			switch (PlayerTradeType)
			{
			case TradeType.Buy:
				CargoTradeHelper.BuyMax(PlayerUnit, DockUnit, CargoClass);
				if (PlayerUnit != null)
				{
					PlayerUnit.TryPlayCargoDoorAudio();
				}
				break;
			case TradeType.Sell:
				CargoTradeHelper.SellMax(PlayerUnit, DockUnit, CargoClass);
				if (PlayerUnit != null)
				{
					PlayerUnit.TryPlayCargoDoorAudio();
				}
				break;
			}
		}

		private bool ShouldBeInteractable()
		{
			if (CargoClass != null && DockUnit != null && PlayerUnit != null)
			{
				switch (PlayerTradeType)
				{
				case TradeType.Buy:
					return CargoTradeHelper.GetMaxBuyable(PlayerUnit, DockUnit, CargoClass) > 0;
				case TradeType.Sell:
					return CargoTradeHelper.GetMaxSellable(PlayerUnit, DockUnit, CargoClass) > 0;
				}
			}
			return false;
		}
	}
}
