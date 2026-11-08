namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFleetSpawnParamsItem
	{
		public string PilotResourceName { get; set; }

		public string ShipName { get; set; }

		public ModelUnitClass UnitClass { get; set; }

		public bool AddCargoLoadout { get; set; } = true;
	}
}
