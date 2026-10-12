using System.Collections.Generic;
using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;

namespace OpenFrontier.IP.SavedGames.V2.Model.Actions
{
	public class ModelAction_Mission_ActivateObjective : ModelAction
	{
		public override ActionType Type => ActionType.Mission_ActivateObjective;

		public List<ModelMissionObjective> Objectives { get; set; } = new List<ModelMissionObjective>();
	}
}
