using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.UI.Controls;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Fleets
{
	public class FleetsListSorter : ListSorter, IComparer<Fleet>
	{
		public FleetsSortMode SortMode;

		public override int GetSortMode()
		{
			return (int)SortMode;
		}

		public override void SetSortMode(int sortType)
		{
			SortMode = (FleetsSortMode)sortType;
		}

		public override List<string> GetSortOptionNames()
		{
			return (from e in Enum.GetNames(typeof(FleetsSortMode))
				select TextUtils.FromTitleCase(e)).ToList();
		}

		public int Compare(Fleet x, Fleet y)
		{
			switch (SortMode)
			{
			case FleetsSortMode.Distance:
			{
				Sector activeSector = EngineASX.Instance.ActiveSector;
				Vector3 sectorPosition = EngineASX.Instance.LocalUnit.SectorPosition;
				float distanceOfFleetFromLocalUnit = FleetsHelper.GetDistanceOfFleetFromLocalUnit(x, activeSector, sectorPosition);
				float distanceOfFleetFromLocalUnit2 = FleetsHelper.GetDistanceOfFleetFromLocalUnit(y, activeSector, sectorPosition);
				return distanceOfFleetFromLocalUnit.CompareTo(distanceOfFleetFromLocalUnit2) * (int)SortOrder;
			}
			case FleetsSortMode.Health:
			{
				float num2 = x.GetCachedHealth() / x.GetCachedMaxHealth() + x.GetCachedShieldHealth() / x.GetCachedMaxShieldHealth();
				float value2 = y.GetCachedHealth() / y.GetCachedMaxHealth() + y.GetCachedShieldHealth() / y.GetCachedMaxShieldHealth();
				return num2.CompareTo(value2) * (int)SortOrder;
			}
			case FleetsSortMode.Order:
			{
				ActiveFleetOrder activeOrder = x.ActiveOrder;
				ActiveFleetOrder activeOrder2 = y.ActiveOrder;
				if (activeOrder == null || activeOrder2 == null)
				{
					return (activeOrder2 == null).CompareTo(activeOrder == null) * (int)SortOrder;
				}
				return activeOrder.FleetOrder.GetDescription().CompareTo(activeOrder2.FleetOrder.GetDescription()) * (int)SortOrder;
			}
			case FleetsSortMode.CargoSpace:
				return x.GetCachedFreeCargoSpace01().CompareTo(y.GetCachedFreeCargoSpace01()) * (int)SortOrder;
			case FleetsSortMode.CargoValue:
				return x.GetCachedTradableCargoValue().CompareTo(y.GetCachedTradableCargoValue()) * (int)SortOrder;
			case FleetsSortMode.Ammo:
			{
				float num = x.GetCachedCompatibleEquipmentLoad() / x.GetCachedTotalCargoCapacity();
				float value = y.GetCachedCompatibleEquipmentLoad() / y.GetCachedTotalCargoCapacity();
				return num.CompareTo(value) * (int)SortOrder;
			}
			case FleetsSortMode.CombatRating:
				return x.GetCachedSimpleCombatRating().CompareTo(y.GetCachedSimpleCombatRating()) * (int)SortOrder;
			default:
				return x.GetFriendlyNameNoPrefix().CompareTo(y.GetFriendlyNameNoPrefix()) * (int)SortOrder;
			}
		}
	}
}
