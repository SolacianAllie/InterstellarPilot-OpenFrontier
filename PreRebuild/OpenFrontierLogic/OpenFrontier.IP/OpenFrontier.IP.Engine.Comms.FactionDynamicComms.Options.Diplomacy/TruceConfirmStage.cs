using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Diplomacy
{
	public class TruceConfirmStage : DynamicCommsStage
	{
		private int truceCost;

		private bool truceResult;

		public override void Show(ICommsController commsController)
		{
			base.Show(commsController);
			truceResult = DynamicCommsHandler.OwnerFaction.FactionAI.RequestTruce(EngineASX.Instance.LocalFaction, out truceCost);
			DynamicCommsStage.stageOptionCache.Clear();
			SetMessageText();
			AddNevermindOption();
			if (truceResult)
			{
				AddConfirmOption();
			}
		}

		private void SetMessageText()
		{
			DynamicCommsMessage dynamicCommsMessage = new DynamicCommsMessage();
			dynamicCommsMessage.MessageText = GetConfirmMessage();
			Message = dynamicCommsMessage;
		}

		private string GetConfirmMessage()
		{
			if (truceResult)
			{
				return GetAvailableTruceMessage();
			}
			return GetNoTruceMessage();
		}

		private string GetNoTruceMessage()
		{
			return $"{GetFactionDescription(Handler.OwnerFaction)} not willing to discuss peace at this time";
		}

		private string GetAvailableTruceMessage()
		{
			return $"{GetFactionDescription(Handler.OwnerFaction)} willing to forgive your past actions and cease hostilities for the sum of {TextFormattingHelper.FormatCredits(truceCost)} credits.";
		}

		private string GetFactionDescription(Faction faction)
		{
			if (faction.AISettings != null && faction.AISettings.PreferSingleShip)
			{
				return "I am";
			}
			return string.Format("We are", faction.GetLongNameElseShort());
		}

		private void AddConfirmOption()
		{
			CommsStageEventOption commsStageEventOption = new CommsStageEventOption
			{
				OptionText = CommsHelper.FormatMessageWithCreditsCost("Ok", truceCost)
			};
			commsStageEventOption.Selected += ConfirmOption_Selected;
			DynamicCommsStage.stageOptionCache.Add(commsStageEventOption);
		}

		private void ConfirmOption_Selected(CommsStageEventOption option, ICommsController commsController)
		{
			if (CanPlayerAffordTruceCost())
			{
				MakeTruce();
				commsController.GoToHandlerDefaultStage();
			}
			else
			{
				UIController.Instance.ShowInsufficientCreditsMessageBox();
			}
		}

		private bool CanPlayerAffordTruceCost()
		{
			return EngineASX.Instance.LocalFaction.Credits >= truceCost;
		}

		private void MakeTruce()
		{
			TruceHelper.PlayerMakeTruceWithCreditsExchange(EngineASX.Instance.LocalFaction, EngineASX.Instance.LocalPlayer.Person, Handler.OwnerFaction, truceCost);
		}
	}
}
