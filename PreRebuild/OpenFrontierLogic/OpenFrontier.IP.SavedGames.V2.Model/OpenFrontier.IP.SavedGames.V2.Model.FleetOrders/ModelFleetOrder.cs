using OpenFrontier.IP.Common.FleetOrders;

namespace OpenFrontier.IP.SavedGames.V2.Model.FleetOrders
{
	public abstract class ModelFleetOrder
	{
		public float Priority;

		public int AvailableCredits { get; set; } = -1;

		public int Id { get; set; }

		public FleetOrderCompletionMode CompletionMode { get; set; }

		public bool AllowCombatInterception { get; set; }

		public FleetOrderCloakPreference CloakPreference { get; set; }

		public int MaxJumpDistance { get; set; }

		public bool AllowTimeout { get; set; }

		public float TimeoutTime { get; set; }

		public float MaxDuration { get; set; }

		public abstract FleetOrderType OrderType { get; }

		public bool Notifications { get; set; } = true;
	}
}
