namespace OpenFrontier.IP.SavedGames.V2.Model.Jobs
{
	public class ModelMissionObjective
	{
		public int Id { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public bool IsOptional { get; set; }

		public int Order { get; set; }

		public bool IsActive { get; set; }

		public bool IsComplete { get; set; }

		public bool Success { get; set; }

		public bool ShowInJournal { get; set; }
	}
}
