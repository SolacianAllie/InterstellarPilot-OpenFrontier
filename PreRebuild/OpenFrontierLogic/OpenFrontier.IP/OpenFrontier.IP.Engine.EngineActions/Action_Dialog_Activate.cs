using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.Comms;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Dialog_Activate : EngineAction
	{
		public bool Allowed = true;

		public DialogBase Dialog;

		public override ActionType Type => ActionType.Dialog_Activate;

		public override void Execute()
		{
			base.Execute();
			if (Dialog != null)
			{
				Dialog.AllowDialogToShow = Allowed;
			}
		}
	}
}
