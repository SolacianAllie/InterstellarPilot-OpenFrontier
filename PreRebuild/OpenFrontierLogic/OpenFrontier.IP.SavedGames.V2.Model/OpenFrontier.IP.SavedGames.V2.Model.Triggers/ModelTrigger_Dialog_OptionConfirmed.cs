using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Dialog_OptionConfirmed : ModelTrigger
	{
		public override TriggerType Type => TriggerType.Dialog_OptionConfirmed;

		public int DialogOptionId { get; set; }
	}
}
