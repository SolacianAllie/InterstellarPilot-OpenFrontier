namespace OpenFrontier.IP.SavedGames.V2.Model
{
	public class ModelUnitDebrisData
	{
		public int ScrapQuantity { get; set; }

		public bool Expires { get; set; }

		public double ExpiryTime { get; set; }

		public ModelUnitClass RelatedUnitClass { get; set; }
	}
}
