using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.CargoPicker;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Cargo
{
	public class CargoTradeOption : DynamicCommsStageOption
	{
		private ICommsController commsController;

		private TradeType aiTradeType = TradeType.Buy;

		public TradeType AITradeType
		{
			get
			{
				return aiTradeType;
			}
			set
			{
				aiTradeType = value;
			}
		}

		public List<CommsTradeCargoItem> CargoItems { get; set; }

		public override void Select(ICommsController commsController)
		{
			this.commsController = commsController;
			base.Select(commsController);
			if (CargoItems != null)
			{
				List<CargoPickerItem> items = CargoItems.Select((CommsTradeCargoItem e) => new CargoPickerItem
				{
					CargoClass = e.CargoClass,
					MinQuantiy = 1,
					MaxQuantity = e.Quantity,
					AvailableText = ((aiTradeType == TradeType.Sell) ? "can be bought" : "can be sold")
				}).ToList();
				ScreenNavigator.Instance.ShowCargoPickerScreen(items, CargoItemPicked);
			}
		}

		private void CargoItemPicked(CargoPickerScreen sender, CargoPickerResult result)
		{
			sender.Finished -= CargoItemPicked;
			CommsTradeCargoItem selectedItem = null;
			if (TryPickItemFromCargoPicker(result, out selectedItem))
			{
				OnCargoItemPicked(selectedItem);
			}
			else
			{
				sender.NavigateBack();
			}
		}

		private bool TryPickItemFromCargoPicker(CargoPickerResult result, out CommsTradeCargoItem selectedItem)
		{
			selectedItem = null;
			if (result != null)
			{
				CommsTradeCargoItem commsTradeCargoItem = CargoItems.FirstOrDefault((CommsTradeCargoItem e) => e.CargoClass == result.CargoClass);
				if (commsTradeCargoItem != null)
				{
					commsTradeCargoItem.Quantity = result.Quantity;
					selectedItem = commsTradeCargoItem;
					return true;
				}
			}
			return false;
		}

		protected virtual void OnCargoItemPicked(CommsTradeCargoItem cargoItem)
		{
			ConfirmCargoTradeStage confirmCargoTradeStage = new ConfirmCargoTradeStage();
			confirmCargoTradeStage.Item = cargoItem;
			confirmCargoTradeStage.AITradeType = aiTradeType;
			confirmCargoTradeStage.DynamicCommsHandler = (DynamicCommsHandler)commsController.CommsHandler;
			commsController.ChangeStage(confirmCargoTradeStage);
		}
	}
}
