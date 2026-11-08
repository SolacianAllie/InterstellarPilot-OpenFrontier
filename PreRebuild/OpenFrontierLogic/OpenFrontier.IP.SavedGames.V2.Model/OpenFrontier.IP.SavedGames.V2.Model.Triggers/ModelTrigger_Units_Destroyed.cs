using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;

namespace OpenFrontier.IP.SavedGames.V2.Model.Triggers
{
	public class ModelTrigger_Units_Destroyed : ModelTrigger
	{
		public List<ModelUnit> Units = new List<ModelUnit>();

		public override TriggerType Type => TriggerType.Units_Destroyed;

		public int MinCount { get; set; } = -1;
	}
}
