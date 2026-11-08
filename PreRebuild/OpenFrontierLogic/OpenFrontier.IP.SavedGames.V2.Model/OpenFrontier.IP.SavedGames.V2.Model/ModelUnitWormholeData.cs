namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelUnitWormholeData
	{
		public ModelUnit TargetWormholeUnit { get; set; }

		public bool IsUnstable { get; set; }

		public double UnstableNextChangeTargetTime { get; set; }

		public Vec3 UnstableTargetPosition { get; set; }

		public Vec3 UnstableTargetRotation { get; set; }

		public ModelSector UnstableTargetSector { get; set; }
	}
}
