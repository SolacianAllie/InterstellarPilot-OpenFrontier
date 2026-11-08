namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelNpcPilotSettings
	{
		public float RestrictedWeaponPreference { get; set; }

		public float CombatEfficiency { get; set; }

		public bool CheatAmmo { get; set; }

		public bool AllowDitchShip { get; set; } = true;
	}
}
