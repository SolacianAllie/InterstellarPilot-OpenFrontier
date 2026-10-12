using OpenFrontier.IP.Engine.Fleets.FleetOrders;

namespace OpenFrontier.IP.Engine.AI.ActiveOrders
{
	public class ActiveDisposeCargoOrder : ActiveFleetOrder
	{
		public DisposeCargoOrder DisposeCargoObjective;

		protected override void onInit()
		{
			base.onInit();
			foreach (NpcPilot npcPilot in fleet.NpcPilots)
			{
				npcPilot.CurrentUnit.CargoBayComponent.RemoveAllCargo();
			}
			fleet.InvalidateCargoUsageStats();
			OnComplete();
		}
	}
}
