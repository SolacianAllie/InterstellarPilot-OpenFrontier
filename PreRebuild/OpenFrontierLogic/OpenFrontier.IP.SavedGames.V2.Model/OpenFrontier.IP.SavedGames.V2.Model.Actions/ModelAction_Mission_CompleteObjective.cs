using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;

namespace OpenFrontier.IP.SavedGames.V2.Model.Actions
{
	public class ModelAction_Mission_CompleteObjective : ModelAction
	{
		public override ActionType Type => ActionType.Mission_CompleteObjective;

		public ModelMissionObjective MissionObjective { get; set; }

		public bool Success { get; set; } = true;
	}
}
