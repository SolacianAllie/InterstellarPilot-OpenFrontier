using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_LegacyPlayerWithinDistOfUnit : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Player_NearUnit;

		public float Distance { get; set; } = 200f;

		public ModelUnit TargetUnit { get; set; }
	}
}
