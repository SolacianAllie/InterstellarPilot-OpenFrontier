using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Dialog_StageFinished : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Dialog_StageFinished;

		public int DialogStageId { get; set; }
	}
}
