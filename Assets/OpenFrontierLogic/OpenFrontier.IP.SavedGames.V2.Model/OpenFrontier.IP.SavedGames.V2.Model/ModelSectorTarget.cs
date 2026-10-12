namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelSectorTarget
	{
		public Vec3 Position { get; set; }

		public ModelSector Sector { get; set; }

		public ModelUnit TargetUnit { get; set; }

		public ModelFleet TargetFleet { get; set; }

		public bool HadValidTarget { get; set; }
	}
}
