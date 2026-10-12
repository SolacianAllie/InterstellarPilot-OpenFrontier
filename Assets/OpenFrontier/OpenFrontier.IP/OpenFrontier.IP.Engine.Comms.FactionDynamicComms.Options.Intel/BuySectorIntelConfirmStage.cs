using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Intel;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens;
using OpenFrontier.IP.UI.Screens.BuySectorIntel;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Intel
{
	public class BuySectorIntelConfirmStage : DynamicCommsStage
	{
		public Sector Sector;

		private List<Unit> discoverableUnits = new List<Unit>();

		private int costOfDiscoverableUnits;

		public override void Show(ICommsController commsController)
		{
			base.Show(commsController);
			discoverableUnits = GetDiscoverableUnits();
			costOfDiscoverableUnits = GetCostOfDiscoverableUnits(discoverableUnits);
			DynamicCommsStage.stageOptionCache.Clear();
			SetMessageText();
			AddNevermindOption();
			AddConfirmOption();
		}

		private int GetCostOfDiscoverableUnits(List<Unit> discoverableUnits)
		{
			return IntelHelper.GetCostToDiscoverUnits(discoverableUnits, EngineASX.Instance.LocalFaction);
		}

		private List<Unit> GetDiscoverableUnits()
		{
			return (from intelUnit in DynamicCommsHandler.OwnerFaction.Intel.GetDiscoveredUnitsInSceneWhichOtherFactionHasnt(Sector, EngineASX.Instance.LocalFaction)
				where BuySectorIntelScreen.CanPlayerBuySectorIntelOnUnit(intelUnit) && DynamicCommsHandler.OwnerFaction.FactionAI.WillSellIntelItemTo(EngineASX.Instance.LocalFaction, intelUnit)
				select intelUnit).ToList();
		}

		private void SetMessageText()
		{
			DynamicCommsMessage dynamicCommsMessage = new DynamicCommsMessage();
			dynamicCommsMessage.MessageText = GetConfirmMessage();
			Message = dynamicCommsMessage;
		}

		private string GetConfirmMessage()
		{
			return $"You want intel on the {Sector.Name} sector?";
		}

		private void AddConfirmOption()
		{
			CommsStageEventOption commsStageEventOption = new CommsStageEventOption
			{
				OptionText = CommsHelper.FormatMessageWithCreditsCost("Ok", costOfDiscoverableUnits)
			};
			commsStageEventOption.Selected += ConfirmOption_Selected;
			DynamicCommsStage.stageOptionCache.Add(commsStageEventOption);
		}

		private void ConfirmOption_Selected(CommsStageEventOption option, ICommsController commsController)
		{
			if (CanPlayerAffordToPurchaseIntel())
			{
				EngineASX.Instance.AddCreditsToPlayerFactionWithMsg(-costOfDiscoverableUnits, FactionTransactionType.MiscPurchase, DynamicCommsHandler.OwnerFaction);
				DynamicCommsHandler.OwnerFaction.ApplyTransaction(costOfDiscoverableUnits, FactionTransactionType.MiscPurchase, EngineASX.Instance.LocalFaction);
				foreach (Unit discoverableUnit in discoverableUnits)
				{
					EngineASX.Instance.LocalFaction.Intel.DiscoverUnit(discoverableUnit);
				}
				commsController.GoToHandlerDefaultStage();
				ScreenNavigator.Instance.ShowBuySectorIntelConfirmScreen(discoverableUnits);
			}
			else
			{
				UIController.Instance.ShowInsufficientCreditsMessageBox();
			}
		}

		private bool CanPlayerAffordToPurchaseIntel()
		{
			return EngineASX.Instance.LocalFaction.Credits >= costOfDiscoverableUnits;
		}
	}
}
