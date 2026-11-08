namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPlayerWaypoint
	{
		public Vec3 SectorPosition { get; set; }

		public ModelSector Sector { get; set; }

		public ModelUnit TargetUnit { get; set; }

		public bool HadTargetObject { get; set; }
	}
}
