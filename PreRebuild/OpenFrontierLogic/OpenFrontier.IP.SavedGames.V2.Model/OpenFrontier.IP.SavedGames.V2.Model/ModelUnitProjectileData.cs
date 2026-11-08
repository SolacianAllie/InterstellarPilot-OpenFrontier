namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelUnitProjectileData
	{
		public ModelUnit SourceUnit { get; set; }

		public ModelUnit TargetUnit { get; set; }

		public double FireTime { get; set; }

		public float RemainingMovement { get; set; }

		public ModelDamageType DamageType { get; set; }
	}
}
