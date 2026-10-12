using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Diplomacy
{
	public class PayOffBountyConfirmStage : DynamicCommsStage
	{
		private int bountyPlacedOnPlayerFaction;

		public override void Show(ICommsController commsController)
		{
			base.Show(commsController);
			bountyPlacedOnPlayerFaction = BountyHelper.GetTotalBountyPlacedOnFactionByFaction(EngineASX.Instance.LocalFaction, DynamicCommsHandler.OwnerFaction);
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

		private string GetConfirmMessage()
		{
			return $"You want to pay {GetUsOrMeText(DynamicCommsHandler.OwnerFaction)} to remove your bounty?";
		}

		private string GetUsOrMeText(Faction ownerFaction)
		{
			if (ownerFaction.AISettings.PreferSingleShip)
			{
				return "me";
			}
			return "us";
		}

		private void AddConfirmOption()
		{
			CommsStageEventOption commsStageEventOption = new CommsStageEventOption
			{
				OptionText = CommsHelper.FormatMessageWithCreditsCost("Ok", bountyPlacedOnPlayerFaction)
			};
			commsStageEventOption.Selected += ConfirmOption_Selected;
			DynamicCommsStage.stageOptionCache.Add(commsStageEventOption);
		}

		private void ConfirmOption_Selected(CommsStageEventOption option, ICommsController commsController)
		{
			bountyPlacedOnPlayerFaction = BountyHelper.GetTotalBountyPlacedOnFactionByFaction(EngineASX.Instance.LocalFaction, DynamicCommsHandler.OwnerFaction);
			if (bountyPlacedOnPlayerFaction > 0)
			{
				if (CanPlayerAffordPayOffBounty())
				{
					EngineASX.Instance.CreditsAnimation.AllowCreditBeepAudio = true;
					BountyHelper.PayOffBountyPlacedByFactionOnFaction(EngineASX.Instance, EngineASX.Instance.LocalFaction, Handler.OwnerFaction);
					EngineASX.Instance.RaiseExchangedCreditsMessage(-bountyPlacedOnPlayerFaction);
					commsController.GoToHandlerDefaultStage();
				}
				else
				{
					UIController.Instance.ShowInsufficientCreditsMessageBox();
				}
			}
			else
			{
				commsController.GoToHandlerDefaultStage();
			}
		}

		private bool CanPlayerAffordPayOffBounty()
		{
			return EngineASX.Instance.LocalFaction.Credits >= bountyPlacedOnPlayerFaction;
		}
	}
}
