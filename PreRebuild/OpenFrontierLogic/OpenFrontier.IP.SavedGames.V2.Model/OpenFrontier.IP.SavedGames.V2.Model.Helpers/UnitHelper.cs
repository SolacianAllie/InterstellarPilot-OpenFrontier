namespace OpenFrontier.IP.SavedGames.V2.Model.Helpers
{
	public static class UnitHelper
	{
		public static bool IsProjectile(ModelUnitClass unitClass)
		{
			return unitClass.ToString().Contains("Projectile");
		}
	}
}
