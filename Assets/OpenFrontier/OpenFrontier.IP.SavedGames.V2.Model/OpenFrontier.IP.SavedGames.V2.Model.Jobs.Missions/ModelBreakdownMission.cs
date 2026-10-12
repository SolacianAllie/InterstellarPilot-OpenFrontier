using OpenFrontier.IP.Common;

namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs.Missions
{
	public class ModelBreakdownMission : ModelMission
	{
		public override MissionType MissionType => MissionType.Breakdown;

		public ModelUnit BaseUnit { get; set; }

		public ModelUnit BreakdownUnit { get; set; }
	}
}
