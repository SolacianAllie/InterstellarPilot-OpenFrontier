using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Fleet_InState : ModelTrigger
	{
		public FleetState State;

		public override TriggerType Type => TriggerType.Fleet_InState;

		public ModelFleet Fleet { get; set; }
	}
}
