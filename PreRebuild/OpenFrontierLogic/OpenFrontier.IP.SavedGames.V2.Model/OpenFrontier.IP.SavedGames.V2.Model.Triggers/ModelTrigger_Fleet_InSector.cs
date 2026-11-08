using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Fleet_InSector : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Fleet_InSector;

		public ModelFleet Fleet { get; set; }

		public ModelSector Sector { get; set; }
	}
}
