using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Fleet_DestroyedOrNoPilots : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Fleet_DestroyedOrNoPilots;

		public ModelFleet Fleet { get; set; }
	}
}
