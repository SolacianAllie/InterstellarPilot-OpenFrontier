namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPlayerMessage
	{
		public int Id { get; set; }

		public double EngineTimeStamp { get; set; }

		public bool AllowDelete { get; set; }

		public bool Opened { get; set; }

		public ModelUnit SenderUnit { get; set; }

		public ModelUnit SubjectUnit { get; set; }

		public int MessageTemplateId { get; set; }

		public string ToText { get; set; }

		public string FromText { get; set; }

		public string MessageText { get; set; }

		public string SubjectText { get; set; }

		public ModelSector SenderUnitSector { get; set; }

		public Vec3 SenderUnitSectorPosition { get; set; }

		public ModelSector SubjectUnitSector { get; set; }

		public Vec3 SubjectUnitSectorPosition { get; set; }
	}
}
