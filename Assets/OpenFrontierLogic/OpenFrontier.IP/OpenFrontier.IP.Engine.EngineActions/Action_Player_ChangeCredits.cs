using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Player_ChangeCredits : EngineAction
	{
		public int Quantity = 1000;

		public override ActionType Type => ActionType.Player_ChangeCredits;

		public override void Execute()
		{
			base.Execute();
			engine.AddCreditsToPlayerFactionWithMsg(Quantity);
		}
	}
}
