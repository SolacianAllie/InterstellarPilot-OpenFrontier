namespace OpenFrontier.IP.SavedGames.V2.Model.Factions.Bounty
{
	public class ModelFactionBountyBoardItem
	{
		public ModelPerson TargetPerson { get; set; }

		public int Reward { get; set; }

		public ModelUnit LastKnownTargetUnit { get; set; }

		public ModelSector LastKnownTargetSector { get; set; }

		public Vec3? LastKnownTargetPosition { get; set; }

		public double? TimeOfLastSighting { get; set; }

		public ModelFaction SourceFaction { get; set; }
	}
}
