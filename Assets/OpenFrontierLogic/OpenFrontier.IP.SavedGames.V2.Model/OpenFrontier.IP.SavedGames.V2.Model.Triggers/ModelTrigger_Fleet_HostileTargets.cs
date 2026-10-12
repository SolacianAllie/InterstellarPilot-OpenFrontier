using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Fleet_HostileTargets : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Fleet_HostileTargets;

		public ModelFleet Fleet { get; set; }
	}
}
