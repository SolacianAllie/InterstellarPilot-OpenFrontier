using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Comms;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Dialog_ChangeStage : EngineAction
	{
		public DialogStage Stage;

		public override ActionType Type => ActionType.Dialog_ChangeStage;

		public override void Execute()
		{
			base.Execute();
			engine.DialogController.ChangeStage(Stage);
		}
	}
}
