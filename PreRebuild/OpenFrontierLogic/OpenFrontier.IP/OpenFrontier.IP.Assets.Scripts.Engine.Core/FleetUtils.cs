using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens;
using UnityEngine;

namespace OpenFrontier.IP.Assets.Scripts.Engine.Core
{
	public static class FleetUtils
	{
		public static bool CanSplitFleet(Fleet fleet)
		{
			if (fleet != null && fleet.Ships.Count < 2)
			{
				return false;
			}
			return true;
		}

		public static Fleet SplitFleet(Fleet fleet, Fleet fleetPrefab)
		{
			if (fleet.Ships.Count < 2)
			{
				Debug.LogWarning($"Cannot split fleet. Ship count is {fleet.Ships.Count}", fleet);
				return null;
			}
			_ = fleet.Ships.Count / 2;
			Fleet fleet2 = OrdersHelper.CreateAndInitPlayerFleet(fleet.Faction, fleet.Sector, fleet.SectorPosition, fleetPrefab);
			UnitComponentHolder[] array = (from e in fleet.Ships
				where e != fleet.LeaderUnit
				orderby e.UnitClass.SaleCost
				select e).ToArray();
			for (int num = 0; num < array.Length; num += 2)
			{
				array[num].PilotPerson.NpcPilot.Fleet = fleet2;
			}
			float num2 = fleet2.VeryBasicFleetRadiusCalculation();
			float num3 = fleet.VeryBasicFleetRadiusCalculation() + num2;
			fleet.transform.localPosition = PhysicsNonOverlappingPositionFinder.FindSectorPosition(fleet.Sector, fleet.SectorPosition + Geometry.RandomXZUnitVector() * num3, num2, GameController.Instance.StaticNonOverlappingMask);
			return fleet2;
		}
	}
}
