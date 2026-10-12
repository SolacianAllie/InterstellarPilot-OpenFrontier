namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelSector
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public Vec3 MapPosition { get; set; }

		public string Description { get; set; }

		public float GateDistanceMultiplier { get; set; }

		public int RandomSeed { get; set; }

		public Vec3 BackgroundRotation { get; set; }

		public Vec3 DirectionLightRotation { get; set; }

		public Vec3 DirectionLightColor { get; set; }

		public Vec3 AmbientLightColor { get; set; }

		public double LastTimeChangedControl { get; set; }

		public float LightDirectionFudge { get; set; }

		public ModelSectorAppearance CustomAppearance { get; set; }
	}
}
