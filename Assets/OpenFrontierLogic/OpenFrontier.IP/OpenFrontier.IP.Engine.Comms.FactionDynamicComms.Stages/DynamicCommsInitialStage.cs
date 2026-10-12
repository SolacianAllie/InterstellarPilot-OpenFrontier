using OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options;
using OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Cargo;
using OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Diplomacy;
using OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Intel;
using OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Mercenary;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Stages
{
	public class DynamicCommsInitialStage : DynamicCommsStage
	{
		public override void Show(ICommsController commsController)
		{
			base.Show(commsController);
			DynamicCommsStage.stageOptionCache.Clear();
			AddDismissOption();
			MercenaryOptionsController.PopulateOptions(DynamicCommsHandler, DynamicCommsStage.stageOptionCache);
			DiplomacyOptionsController.PopulateOptions(DynamicCommsHandler, DynamicCommsStage.stageOptionCache);
			CargoOptionsController.PopulateOptions(DynamicCommsHandler, DynamicCommsStage.stageOptionCache);
			IntelOptionsController.PopulateOptions(DynamicCommsHandler, DynamicCommsStage.stageOptionCache);
			PopulateIntroText(commsController);
		}

		public static void AddDismissOption()
		{
			DismissStageOption dismissStageOption = new DismissStageOption();
			dismissStageOption.OptionText = "Nevermind";
			DynamicCommsStage.stageOptionCache.Add(dismissStageOption);
		}

		private void PopulateIntroText(ICommsController commsController)
		{
			DynamicCommsMessage dynamicCommsMessage = new DynamicCommsMessage();
			dynamicCommsMessage.MessageText = GetIntroMessage(commsController);
			Message = dynamicCommsMessage;
		}

		private static string GetIntroMessage(ICommsController commsController)
		{
			return EngineASX.Instance.DynamicCommsWelcomeMessageCalculator.GetMessage(commsController);
		}
	}
}
