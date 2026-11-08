namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Diplomacy
{
	public class PayOffBountyOption : DynamicCommsStageOption
	{
		public override void Select(ICommsController commsController)
		{
			PayOffBountyConfirmStage payOffBountyConfirmStage = new PayOffBountyConfirmStage();
			payOffBountyConfirmStage.DynamicCommsHandler = (DynamicCommsHandler)commsController.CommsHandler;
			commsController.ChangeStage(payOffBountyConfirmStage);
		}
	}
}
