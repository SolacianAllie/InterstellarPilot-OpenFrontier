namespace OpenFrontier.IP.Engine.Fleets.ActiveOrders
{
	public enum CompositeTradeSearchState
	{
		None,
		SearchingExistingTradeableCargo,
		SearchingTradeRoutes,
		SearchingExistingIncompatibleEquipment,
		SearchingExistingCompatibleEquipment
	}
}
