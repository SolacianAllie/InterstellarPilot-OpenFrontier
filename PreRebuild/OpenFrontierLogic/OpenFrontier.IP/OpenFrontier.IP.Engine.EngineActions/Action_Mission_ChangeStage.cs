using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Mission_ChangeStage : EngineAction
	{
		public Mission Mission;

		public MissionStage Stage;

		public override ActionType Type => ActionType.Mission_ChangeStage;

		public override void Execute()
		{
			base.Execute();
			if (Mission != null && Stage != null)
			{
				Mission.ChangeStage(Stage);
			}
		}
	}
}
