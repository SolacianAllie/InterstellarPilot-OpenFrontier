using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Unit_ReceivedDamage : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Unit_ReceivedDamage;

		public ModelUnit TargetUnit { get; set; }
	}
}
