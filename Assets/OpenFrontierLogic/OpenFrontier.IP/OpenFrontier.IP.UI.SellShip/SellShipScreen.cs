using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.SellShip
{
	public class SellShipScreen : EngineScreen
	{
		public Image ShipImage;

		public Button ConfirmSaleButton;

		private int saleCredits;

		private Unit unitBeingSold;

		public Text SellShipLabel;

		protected override void awake()
		{
			base.awake();
			ConfirmSaleButton.onClick.AddListener(ConfirmSale);
		}

		protected override void refresh()
		{
			base.refresh();
			if (DockUI.PlayerDockUnit != null)
			{
				unitBeingSold = DockUI.PlayerCurrentUnit;
				saleCredits = GetTradeInCost(Eng, unitBeingSold, Eng.LocalFaction, DockUI.PlayerDockUnit.Faction);
				SellShipLabel.text = $"The ship dealer is offering you {TextFormattingHelper.FormatCredits(saleCredits)} credits for \"{unitBeingSold.Components.ShipName}\", a {unitBeingSold.UnitClass.GetClassAndSeriesName()} class. Sell the ship?";
				ShipImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(unitBeingSold.UnitClass);
			}
		}

		public static int GetTradeInCost(EngineASX engine, Unit unitBeingSold, Faction sellerFaction, Faction buyerFaction)
		{
			if (unitBeingSold != null)
			{
				int num = unitBeingSold.CalculateCurrentMoneyValue();
				return Maths.RoundUpToInt(buyerFaction.GetMarkedUpPriceAfterOpinionChange(TradeType.Buy, num, sellerFaction), engine.EconomySettings.ShipSaleRounding);
			}
			return 0;
		}

		private void ConfirmSale()
		{
			Eng.RegisterTaxedPlayerTrade(DockUI.PlayerDockUnit, saleCredits, FactionTransactionType.ShipPurchase, null, unitBeingSold.UnitClass);
			UIController.Instance.QuickMsg.AddMessage(string.Format("{0} Sold", unitBeingSold.GetClassAndSeriesName(), unitBeingSold.Components.ShipName));
			DockUI.MovePlayerToDockUnit();
			if (unitBeingSold != null)
			{
				unitBeingSold.SafeDestroy();
			}
			DockUI.NavigateToInitialScreen();
		}
	}
}
