namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveExploreOrder : ModelActiveFleetOrder
	{
		public ModelSector CurrentTargetSector { get; set; }

		public Vec3 CurrentTargetSectorPosition { get; set; }

		public ModelUnit CurrentTargetWormhole { get; set; }
	}
}
