using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Scenario_TimeElapsed : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Scenario_TimeElapsed;

		public float Time { get; set; } = 4f;
	}
}
