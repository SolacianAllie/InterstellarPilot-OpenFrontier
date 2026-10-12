namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs
{
	public class ModelMissionStage
	{
		public ModelMission Mission { get; set; }

		public bool CompletesMission { get; set; }

		public string JournalEntry { get; set; }

		public bool MissionSuccess { get; set; } = true;
	}
}
