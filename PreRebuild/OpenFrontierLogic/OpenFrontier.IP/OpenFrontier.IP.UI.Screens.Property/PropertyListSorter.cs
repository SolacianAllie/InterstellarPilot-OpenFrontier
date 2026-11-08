using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Controls;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Property
{
	public class PropertyListSorter : ListSorter, IComparer<Unit>
	{
		public PropertySortMode SortMode;

		public override int GetSortMode()
		{
			return (int)SortMode;
		}

		public override void SetSortMode(int sortType)
		{
			SortMode = (PropertySortMode)sortType;
		}

		public override List<string> GetSortOptionNames()
		{
			return (from e in Enum.GetNames(typeof(PropertySortMode))
				select TextUtils.FromTitleCase(e)).ToList();
		}

		public float GetUnitCargoSpace(Unit unit)
		{
			CargoBayComponent cargoBayComponent = unit.CargoBayComponent;
			if (cargoBayComponent != null)
			{
				return cargoBayComponent.FreeSpacePercentage;
			}
			return 0f;
		}

		public float GetUnitCargoValue(Unit unit)
		{
			CargoBayComponent cargoBayComponent = unit.CargoBayComponent;
			if (cargoBayComponent != null)
			{
				return cargoBayComponent.TradableCargoValue;
			}
			return 0f;
		}

		public float GetUnitAmmoLoad(Unit unit)
		{
			CargoBayComponent cargoBayComponent = unit.CargoBayComponent;
			if (cargoBayComponent != null)
			{
				return cargoBayComponent.EquipmentLoad01;
			}
			return 0f;
		}

		public int Compare(Unit x, Unit y)
		{
			switch (SortMode)
			{
			case PropertySortMode.Distance:
			{
				float distanceBetweenUnits = GetDistanceBetweenUnits(EngineASX.Instance.LocalUnit, x);
				float distanceBetweenUnits2 = GetDistanceBetweenUnits(EngineASX.Instance.LocalUnit, y);
				return distanceBetweenUnits.CompareTo(distanceBetweenUnits2) * (int)SortOrder;
			}
			case PropertySortMode.Health:
			{
				float num = ((x.Destructable != null) ? x.Destructable.HealthNormalized : 1f) + ((x.Components != null) ? x.Components.GetShieldHealthNormalized() : 1f);
				float value = ((y.Destructable != null) ? y.Destructable.HealthNormalized : 1f) + ((y.Components != null) ? y.Components.GetShieldHealthNormalized() : 1f);
				return num.CompareTo(value) * (int)SortOrder;
			}
			case PropertySortMode.Order:
			{
				ActiveFleetOrder activeFleetOrder = x.GetActiveFleetOrder();
				ActiveFleetOrder activeFleetOrder2 = y.GetActiveFleetOrder();
				if (activeFleetOrder == null || activeFleetOrder2 == null)
				{
					return (activeFleetOrder2 == null).CompareTo(activeFleetOrder == null) * (int)SortOrder;
				}
				return activeFleetOrder.FleetOrder.GetDescription().CompareTo(activeFleetOrder2.FleetOrder.GetDescription()) * (int)SortOrder;
			}
			case PropertySortMode.CargoSpace:
				return GetUnitCargoSpace(x).CompareTo(GetUnitCargoSpace(y)) * (int)SortOrder;
			case PropertySortMode.CargoValue:
				return GetUnitCargoValue(x).CompareTo(GetUnitCargoValue(y)) * (int)SortOrder;
			case PropertySortMode.Ammo:
				return GetUnitAmmoLoad(x).CompareTo(GetUnitAmmoLoad(y)) * (int)SortOrder;
			case PropertySortMode.CombatRating:
				return x.CombatRating.CompareTo(y.CombatRating) * (int)SortOrder;
			case PropertySortMode.Class:
				if (x.UnitType != y.UnitType)
				{
					return x.UnitType.CompareTo(y.UnitType);
				}
				return x.GetClassAndSeriesName().CompareTo(y.GetClassAndSeriesName());
			default:
				return GetUnitName(x).CompareTo(GetUnitName(y)) * (int)SortOrder;
			}
		}

		public string GetUnitName(Unit unit)
		{
			if (unit.UnitType == UnitType.Ship)
			{
				return unit.Components.ShipName;
			}
			return unit.GetFriendlyName();
		}

		private float GetDistanceBetweenUnits(Unit unit1, Unit unit2)
		{
			if (unit1.Sector == unit2.Sector)
			{
				return Vector3.Distance(unit1.SectorPosition, unit2.SectorPosition);
			}
			return (float)unit1.Sector.GetJumpDistanceTo(unit2.Sector) * 100000f;
		}
	}
}
