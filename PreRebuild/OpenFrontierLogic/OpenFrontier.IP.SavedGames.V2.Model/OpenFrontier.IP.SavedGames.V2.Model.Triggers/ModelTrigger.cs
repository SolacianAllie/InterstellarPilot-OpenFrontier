using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger
	{
		public bool Invert { get; set; }

		public virtual TriggerType Type => TriggerType.Unspecified;
	}
}
