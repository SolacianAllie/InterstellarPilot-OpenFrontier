namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelNpcPilot
	{
		public ModelFleet Fleet { get; set; }

		public bool DestroyWhenNoUnit { get; set; }

		public bool DestroyWhenNotPilotting { get; set; }

		public ModelPerson Person { get; set; }
	}
}
