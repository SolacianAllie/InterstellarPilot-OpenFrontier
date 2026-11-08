using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitHangar : MonoBehaviour
	{
		public delegate void DockedUnitsChangedHandler(UnitHangar sender, UnitHangarBay bay, UnitComponentHolder oldUnit, UnitComponentHolder newUnit);

		private static List<ShipHullType> hullTypes = new List<ShipHullType>(10);

		private static List<UnitHangarBay> unoccupiedBayCache = new List<UnitHangarBay>();

		private UnitComponentHolder unitComponents;

		private List<UnitHangarBay> bays = new List<UnitHangarBay>();

		private List<UnitHangarBay> unoccupiedBays = new List<UnitHangarBay>();

		private List<UnitComponentHolder> dockedShips = new List<UnitComponentHolder>();

		private static List<UnitHangarBay> bayCache = new List<UnitHangarBay>();

		private ShipHullType largestDockableHullType = ShipHullType.Battleship;

		public UnitComponentHolder UnitComponents
		{
			get
			{
				return unitComponents;
			}
			set
			{
				unitComponents = value;
			}
		}

		public List<UnitComponentHolder> DockedShips => dockedShips;

		public int DockedUnitCount => dockedShips.Count;

		public ShipHullType LargestDockableHullType => largestDockableHullType;

		public List<UnitHangarBay> Bays => bays;

		public List<UnitHangarBay> UnoccupiedBays => unoccupiedBays;

		public bool IsFull => unoccupiedBays.Count == 0;

		public event DockedUnitsChangedHandler DockedUnitsChanged;

		public ShipHullType CalculateLargestDockableHullType()
		{
			ShipHullType shipHullType = ShipHullType.Scout;
			foreach (UnitHangarBay bay in bays)
			{
				if (bay.MaxHullType > shipHullType)
				{
					shipHullType = bay.MaxHullType;
				}
			}
			return shipHullType;
		}

		public void Init(UnitComponentHolder unitComponents)
		{
			this.unitComponents = unitComponents;
			if (this.unitComponents == null)
			{
				Debug.LogWarning("UnitHangar is missing UnitComponentHolder component");
				return;
			}
			this.unitComponents.HangarComponent = this;
			FindBays();
			if (bays.Count == 0)
			{
				Debug.LogError("Unit has hangar without any bays", this);
				return;
			}
			InitBays();
			largestDockableHullType = CalculateLargestDockableHullType();
		}

		private void OutputDuplicateBayIds()
		{
			List<IGrouping<int, UnitHangarBay>> list = (from e in bays
				group e by e.BayId into e
				where e.Count() > 1
				select (e)).ToList();
			if (list.Count > 0)
			{
				Debug.LogErrorFormat(this, "UnitHangar contains bay with non-unique Id: {0}", string.Join(",", list.Select((IGrouping<int, UnitHangarBay> e) => e.ToString()).ToArray()));
			}
		}

		public void FindBays()
		{
			bays.Clear();
			foreach (UnitHangarBay componentsInImmediateChild in transform.GetComponentsInImmediateChildren<UnitHangarBay>())
			{
				bays.Add(componentsInImmediateChild);
			}
		}

		public void InitBays()
		{
			foreach (UnitHangarBay bay in bays)
			{
				bay.Init();
			}
			RefreshUnoccupiedBays();
		}

		public void UpdateDockedUnitsScene()
		{
			foreach (UnitComponentHolder dockedShip in DockedShips)
			{
				dockedShip.Unit.Sector = unitComponents.Unit.Sector;
			}
		}

		public UnitHangarBay GetBestUnoccupiedBay(UnitComponentHolder unit)
		{
			return GetBestUnoccupiedBay(unit, UnoccupiedBays);
		}

		public UnitHangarBay GetBestUnoccupiedBay(ShipHullType hullType)
		{
			return GetBestUnoccupiedBay(hullType, UnoccupiedBays);
		}

		public UnitHangarBay GetBestUnoccupiedBay(UnitComponentHolder unit, List<UnitHangarBay> unoccupiedBays)
		{
			return GetBestUnoccupiedBay(unit.UnitClass.HullType, unoccupiedBays);
		}

		public UnitHangarBay GetBestUnoccupiedBay(ShipHullType hullType, List<UnitHangarBay> unoccupiedBays)
		{
			if (hullType > largestDockableHullType)
			{
				return null;
			}
			if (unoccupiedBays.Count > 0)
			{
				return unoccupiedBays[0];
			}
			return null;
		}

		internal void NotifyBayUnitChanged(UnitHangarBay bay, UnitComponentHolder oldUnit, UnitComponentHolder newUnit)
		{
			OnDockedUnitsChanged(bay, oldUnit, newUnit);
		}

		public bool AddUnit(UnitComponentHolder unit)
		{
			UnitHangarBay bestUnoccupiedBay = GetBestUnoccupiedBay(unit);
			if (bestUnoccupiedBay != null)
			{
				bestUnoccupiedBay.DockedUnit = unit;
				return true;
			}
			Debug.LogError("Cannot find bay for unit", this);
			return false;
		}

		private void OnDockedUnitsChanged(UnitHangarBay bay, UnitComponentHolder oldShip, UnitComponentHolder newShip)
		{
			if (!(oldShip != newShip))
			{
				return;
			}
			if (oldShip == null || newShip == null)
			{
				if (newShip == null)
				{
					unoccupiedBays.Add(bay);
					dockedShips.Remove(oldShip);
				}
				else
				{
					dockedShips.Add(newShip);
					unoccupiedBays.Remove(bay);
				}
			}
			if (DockedUnitsChanged != null)
			{
				DockedUnitsChanged(this, bay, oldShip, newShip);
			}
		}

		private void RefreshUnoccupiedBays()
		{
			dockedShips.Clear();
			unoccupiedBays.Clear();
			foreach (UnitHangarBay bay in bays)
			{
				UnitComponentHolder dockedUnit = bay.DockedUnit;
				if (dockedUnit == null)
				{
					unoccupiedBays.Add(bay);
				}
				else
				{
					dockedShips.Add(dockedUnit);
				}
			}
		}

		public bool CanUnitFitInHangarIgnoreOccupancy(UnitComponentHolder unit)
		{
			return unit.UnitClass.HullType <= largestDockableHullType;
		}

		public bool CanUnitsFitInHangarIgnoreOccupancy(List<UnitComponentHolder> units)
		{
			hullTypes.Clear();
			foreach (UnitComponentHolder unit in units)
			{
				hullTypes.Add(unit.UnitClass.HullType);
			}
			return CanUnitsFitInHangar(hullTypes, bays);
		}

		public bool CanUnitsFitInHangarIgnoreOccupancy(List<ShipHullType> hullTypes)
		{
			return CanUnitsFitInHangar(hullTypes, bays);
		}

		public bool CanUnitsFitInHangar(List<UnitComponentHolder> controlledUnits)
		{
			hullTypes.Clear();
			foreach (UnitComponentHolder controlledUnit in controlledUnits)
			{
				hullTypes.Add(controlledUnit.UnitClass.HullType);
			}
			return CanUnitsFitInHangar(hullTypes, UnoccupiedBays);
		}

		public bool CanUnitsFitInHangar(List<ShipHullType> unitHullTypes)
		{
			return CanUnitsFitInHangar(unitHullTypes, UnoccupiedBays);
		}

		public bool CanUnitsFitInHangar(List<ShipHullType> unitHullTypes, List<UnitHangarBay> availableBays)
		{
			ShipHullType shipHullType = ShipHullType.None;
			int num = 0;
			foreach (ShipHullType unitHullType in unitHullTypes)
			{
				num++;
				if (unitHullType > shipHullType)
				{
					shipHullType = unitHullType;
				}
			}
			if (shipHullType <= largestDockableHullType)
			{
				return num < availableBays.Count;
			}
			return false;
		}

		public UnitHangarBay GetBayById(int bayId)
		{
			foreach (UnitHangarBay bay in bays)
			{
				if (bay.BayId == bayId)
				{
					return bay;
				}
			}
			return null;
		}
	}
}
