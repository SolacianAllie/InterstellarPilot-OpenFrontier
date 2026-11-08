using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Dialog_MessageConfirmed : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Dialog_MessageConfirmed;

		public int DialogMessageId { get; set; }
	}
}
