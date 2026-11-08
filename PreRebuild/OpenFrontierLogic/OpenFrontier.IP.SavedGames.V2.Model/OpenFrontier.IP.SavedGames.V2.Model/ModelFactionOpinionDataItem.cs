namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelFactionOpinionDataItem
	{
		public ModelFaction OtherFaction { get; set; }

		public float Opinion { get; set; }

		public double CreatedTime { get; set; } = -1.0;
	}
}
