using System.Collections.Generic;
using OpenFrontier.IP.Engine;

namespace OpenFrontier.IP.UI
{
	public class PassengersScreenItemList : ScrollList<PassengerGroup>
	{
		public PassengersScreen PassengerInfoScreen;

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<PassengerGroup> list = new List<PassengerGroup>();
			AddPassengersOnUnit(PassengerInfoScreen.CurrentUnit, list);
			if (PassengerInfoScreen.CurrentUnit == EngineASX.Instance.PlayerUnit && PassengerInfoScreen.DockUI.CanDockedShipTrade)
			{
				AddPassengersOnUnitWaitingForPickup(PassengerInfoScreen.DockUnit, list);
			}
			SetItems(list);
		}

		private void AddPassengersOnUnitWaitingForPickup(Unit unit, List<PassengerGroup> cache)
		{
			foreach (PassengerGroup passengerGroup in unit.Components.PassengerGroups)
			{
				if (passengerGroup.IsWaitingForPickup)
				{
					cache.Add(passengerGroup);
				}
			}
		}

		private void AddPassengersOnUnit(Unit unit, List<PassengerGroup> cache)
		{
			foreach (PassengerGroup passengerGroup in unit.Components.PassengerGroups)
			{
				cache.Add(passengerGroup);
			}
		}
	}
}
