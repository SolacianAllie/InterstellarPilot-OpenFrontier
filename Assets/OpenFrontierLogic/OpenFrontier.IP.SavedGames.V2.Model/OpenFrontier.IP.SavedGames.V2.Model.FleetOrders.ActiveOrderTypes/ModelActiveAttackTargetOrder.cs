namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders.ActiveOrderTypes
{
	public class ModelActiveAttackTargetOrder : ModelActiveFleetOrder
	{
		public ModelUnit TargetUnit { get; set; }

		public ModelFaction OriginalTargetFaction { get; set; }
	}
}
