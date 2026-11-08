namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Diplomacy
{
	public class TruceOption : DynamicCommsStageOption
	{
		public override void Select(ICommsController commsController)
		{
			TruceConfirmStage truceConfirmStage = new TruceConfirmStage();
			truceConfirmStage.DynamicCommsHandler = (DynamicCommsHandler)commsController.CommsHandler;
			commsController.ChangeStage(truceConfirmStage);
		}
	}
}
