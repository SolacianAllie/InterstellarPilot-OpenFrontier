using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Player_IsPilotting : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Player_IsPilotting;

		public bool WaitForHud { get; set; } = true;
	}
}
