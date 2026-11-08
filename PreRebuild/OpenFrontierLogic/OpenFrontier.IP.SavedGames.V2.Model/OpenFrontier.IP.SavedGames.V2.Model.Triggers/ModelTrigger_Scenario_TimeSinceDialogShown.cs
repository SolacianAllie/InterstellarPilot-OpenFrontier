using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Scenario_TimeSinceDialogShown : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Scenario_TimeSinceDialogShown;

		public bool IncludePendingRequests { get; set; } = true;

		public float TimeSinceShown { get; set; } = 0.5f;
	}
}
