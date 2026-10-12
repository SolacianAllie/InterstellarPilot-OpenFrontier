using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.Engine.MissionObjectives;

namespace OpenFrontier.IP.Engine.EngineActions
{
	public class Action_Mission_CompleteObjective : EngineAction
	{
		public MissionObjective Objective;

		public bool Success = true;

		public override ActionType Type => ActionType.Mission_CompleteObjective;

		public override void Execute()
		{
			base.Execute();
			Objective.Complete(Success);
		}
	}
}
