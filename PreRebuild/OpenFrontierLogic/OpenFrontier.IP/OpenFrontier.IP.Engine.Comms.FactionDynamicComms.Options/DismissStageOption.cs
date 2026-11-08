namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options
{
	public class DismissStageOption : DynamicCommsStageOption
	{
		public override void Select(ICommsController commsController)
		{
			commsController.Dismiss();
		}
	}
}
