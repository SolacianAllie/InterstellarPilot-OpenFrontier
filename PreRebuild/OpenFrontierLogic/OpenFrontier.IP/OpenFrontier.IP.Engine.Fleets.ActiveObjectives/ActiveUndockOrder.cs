using OpenFrontier.IP.Common.FleetOrders;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Fleets.ActiveObjectives
{
	public class ActiveUndockOrder : ActiveFleetOrder
	{
		private float nextCheckTime;

		protected override void tick(float elapsedTime)
		{
			base.tick(elapsedTime);
			if (Time.time > nextCheckTime)
			{
				nextCheckTime = Time.time + 2f;
				if (fleet.AreAllShipsUndocked())
				{
					OnComplete();
				}
			}
		}

		public override DockedPreference GetPreferToDockWhenIdle()
		{
			return DockedPreference.Undock;
		}
	}
}
