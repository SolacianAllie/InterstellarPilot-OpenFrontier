namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPlayerFleetSettingsCombined
	{
		public ModelSectorTarget HomeBase { get; set; }

		public ModelFleetSettings FleetSettings { get; set; }

		public int FormationId { get; set; } = -1;
	}
}
