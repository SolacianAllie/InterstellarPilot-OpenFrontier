using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Cargo
{
	public class ConfirmCargoTradeStage : DynamicCommsStage
	{
		public TradeType AITradeType = TradeType.Buy;

		public CommsTradeCargoItem Item;

		public override void Show(ICommsController commsController)
		{
			base.Show(commsController);
			DynamicCommsStage.stageOptionCache.Clear();
			SetMessageText();
			AddNevermindOption();
			AddConfirmOption();
		}

		private void SetMessageText()
		{
			DynamicCommsMessage dynamicCommsMessage = new DynamicCommsMessage();
			dynamicCommsMessage.MessageText = GetConfirmMessage();
			Message = dynamicCommsMessage;
		}

		private void AddConfirmOption()
		{
			CommsStageEventOption commsStageEventOption = new CommsStageEventOption
			{
				OptionText = GetConfirmCargoTradeMessage()
			};
			commsStageEventOption.Selected += ConfirmOption_Selected;
			DynamicCommsStage.stageOptionCache.Add(commsStageEventOption);
		}

		private string GetConfirmCargoTradeMessage()
		{
			if (AITradeType == TradeType.Sell)
			{
				return CommsHelper.FormatMessageWithCreditsCost("Ok", CostOfTrade());
			}
			return "Ok";
		}

		private int CostOfTrade()
		{
			return Item.Price * Item.Quantity;
		}

		private string GetConfirmMessage()
		{
			string buySellMessage = GetBuySellMessage();
			string quantityAndPriceMessage = GetQuantityAndPriceMessage();
			return $"{buySellMessage} {quantityAndPriceMessage}";
		}

		private string GetQuantityAndPriceMessage()
		{
			return $"{TextFormattingHelper.FormatCargoAmount(Item.Quantity)}x {Item.CargoClass.ClassName} for {TextFormattingHelper.FormatCredits(Item.Price)} credits each (Total {TextFormattingHelper.FormatCredits(CostOfTrade())})";
		}

		private string GetBuySellMessage()
		{
			if (AITradeType == TradeType.Buy)
			{
				return "I'll buy from you";
			}
			return "I'll sell to you";
		}

		private void ConfirmOption_Selected(CommsStageEventOption option, ICommsController commsController)
		{
			DoTransfer();
			commsController.GoToHandlerDefaultStage();
		}

		private void DoTransfer()
		{
			int num = ((AITradeType == TradeType.Sell) ? Item.Quantity : (-Item.Quantity));
			int num2 = -num * Item.Price;
			EngineASX.Instance.TransferToPlayerCargoWithMsg(Item.CargoClass, num, ignoreCapacity: false);
			EngineASX.Instance.AddCreditsToPlayerFactionWithMsg(num2, FactionTransactionType.Trade, DynamicCommsHandler.OwnerFaction, null, Item.CargoClass);
			DynamicCommsHandler.OwnerPilot.CurrentUnit.CargoBayComponent.AddToCargo(Item.CargoClass, -num, ignoreCapacity: false);
			DynamicCommsHandler.OwnerFaction.ApplyTransaction(-num2, FactionTransactionType.Trade, EngineASX.Instance.LocalFaction, null, Item.CargoClass);
		}
	}
}
