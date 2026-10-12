using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Player_InSector : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Player_InSector;

		public ModelSector Sector { get; set; }
	}
}
