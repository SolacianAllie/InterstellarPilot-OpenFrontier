using OpenFrontier.IP.Common.Triggers;
using OpenFrontier.IP.SavedGames.V2.Model.Jobs;

namespace OpenFrontier.IP.SavedGames.V2.Model.Actions
{
	public class ModelAction_Mission_Activate : ModelAction
	{
		public override ActionType Type => ActionType.Mission_Activate;

		public ModelMission Mission { get; set; }
	}
}
