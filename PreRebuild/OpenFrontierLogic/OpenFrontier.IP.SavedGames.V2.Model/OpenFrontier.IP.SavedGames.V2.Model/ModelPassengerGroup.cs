namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelPassengerGroup
	{
		public int Id { get; set; }

		public ModelUnit Unit { get; set; }

		public ModelUnit SourceUnit { get; set; }

		public ModelUnit DestinationUnit { get; set; }

		public int PassengerCount { get; set; }

		public double ExpiryTime { get; set; }

		public int Revenue { get; set; }
	}
}
