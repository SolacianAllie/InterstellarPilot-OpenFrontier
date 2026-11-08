namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveUniverseRoamOrder : ModelActiveFleetOrder
	{
		public ModelSector CurrentTargetSector { get; set; }

		public Vec3 CurrentTargetPosition { get; set; }
	}
}
