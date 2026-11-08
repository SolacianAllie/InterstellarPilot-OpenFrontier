using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Player_DockedAtUnit : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Player_DockedAtUnit;

		public ModelUnit TargetUnit { get; set; }
	}
}
