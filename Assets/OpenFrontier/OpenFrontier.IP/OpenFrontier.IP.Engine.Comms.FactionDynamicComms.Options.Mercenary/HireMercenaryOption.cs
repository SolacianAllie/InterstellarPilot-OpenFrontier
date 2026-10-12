namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Mercenary
{
	public class HireMercenaryOption : DynamicCommsStageOption
	{
		public override void Select(ICommsController commsController)
		{
			HireMercenaryConfirmStage hireMercenaryConfirmStage = new HireMercenaryConfirmStage();
			hireMercenaryConfirmStage.DynamicCommsHandler = (DynamicCommsHandler)commsController.CommsHandler;
			commsController.ChangeStage(hireMercenaryConfirmStage);
		}
	}
}
