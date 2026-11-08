using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Dialog_NumTimesStageShown : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Dialog_NumTimesStageShown;

		public int Count { get; set; }

		public int DialogStageId { get; set; }
	}
}
