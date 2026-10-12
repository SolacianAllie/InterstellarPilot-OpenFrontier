namespace OpenFrontier.IP.Engine.Fleets
{
	public struct RearmItem
	{
		public CargoClass CargoClass;

		public int RequiredQuantity;

		public RearmItem(CargoClass cargoClass, int requiredQuantity)
		{
			this = default;
			CargoClass = cargoClass;
			RequiredQuantity = requiredQuantity;
		}
	}
}
