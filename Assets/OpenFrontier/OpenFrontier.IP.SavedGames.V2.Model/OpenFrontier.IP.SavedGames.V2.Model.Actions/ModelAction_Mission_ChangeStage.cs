using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;

namespace OpenFrontier.IP.SavedGames.V2.Model.Actions
{
	public class ModelAction_Mission_ChangeStage : ModelAction
	{
		public override ActionType Type => ActionType.Mission_ChangeStage;

		public ModelMissionStage Stage { get; set; }
	}
}
