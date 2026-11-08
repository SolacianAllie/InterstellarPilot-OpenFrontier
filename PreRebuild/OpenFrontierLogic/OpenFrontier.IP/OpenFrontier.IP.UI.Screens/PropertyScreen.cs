using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Controls;
using OpenFrontier.IP.UI.Screens.FleetPicker;
using OpenFrontier.IP.UI.Screens.Fleets;
using OpenFrontier.IP.UI.Screens.Property;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens
{
	public class PropertyScreen : EngineScreen
	{
		public FleetPopupMenu FleetPopupMenu;

		public SectorFilter SectorFilterControl;

		public TMP_InputField PropertyListSearchFilter;

		public PropertyUnitPopupMenu PropertyUnitPopupMenu;

		public PropertyListSorter ListSorter;

		public ModalWindow ViewFilterModal;

		public Toggle ShowDockedShipsToggle;

		public Toggle ShowShipsToggle;

		public Toggle ShowCargoToggle;

		public Toggle ShowStationsToggle;

		public Toggle ShowTurretsToggle;

		private static float updatePropertyListFrequency = 0.5f;

		private double lastTimeRefreshPropertyList;

		private double nextPropertyListFullRefresh;

		private double nextTimeRefreshVisible;

		public double RefreshVisibleItemsInterval = 1.0;

		public PlayerPropertyList PropertyList;

		public GameObject SelectedItemRoot;

		public int MaxFilterableSectors = 16;

		public double TextFilterDebounceTime = 1.0;

		public double MaxTimeBeforeFullRefresh = 15.0;

		private bool isStale;

		private List<Unit> propertyListCache = new List<Unit>(100);

		public Sector SectorFilter
		{
			get
			{
				return SectorFilterControl.Sector;
			}
			set
			{
				SectorFilterControl.Sector = value;
			}
		}

		protected override void awake()
		{
			base.awake();
			PropertyList.SelectedItemChanged += PropertyList_SelectedItemChanged;
			ShowDockedShipsToggle.onValueChanged.AddListener(PropertyListFilterChanged);
			ShowShipsToggle.onValueChanged.AddListener(PropertyListFilterChanged);
			ShowCargoToggle.onValueChanged.AddListener(PropertyListFilterChanged);
			ShowStationsToggle.onValueChanged.AddListener(PropertyListFilterChanged);
			ShowTurretsToggle.onValueChanged.AddListener(PropertyListFilterChanged);
			PropertyList.SelectedItemsChanged += PropertyList_SelectedItemsChanged;
			ListSorter.Applying += ListSorter_Applying;
			PropertyListSearchFilter.onValueChanged.AddListener(OnPropertyListTextFilterChanged);
			SectorFilterControl.Changed += SectorFilterControl_Changed;
			EngineASX.Instance.PlayerPropertyChangedSector += Instance_PlayerPropertyChangedSector;
		}

		private void Instance_PlayerPropertyChangedSector(Unit unit, Sector oldSector, Sector newSector)
		{
			if (SectorFilter == null || oldSector == SectorFilter || newSector == SectorFilter)
			{
				Invalidate();
			}
		}

		public void Invalidate()
		{
			isStale = true;
		}

		private void SectorFilterControl_Changed(SectorFilter sender)
		{
			Refresh();
			PropertyList.ResetScrollPosition();
		}

		private void OnPropertyListTextFilterChanged(string value)
		{
			Invalidate();
			nextPropertyListFullRefresh = Math.Max(nextPropertyListFullRefresh, Time.realtimeSinceStartupAsDouble + TextFilterDebounceTime);
		}

		private void ListSorter_Applying(ListSorter sender)
		{
			Refresh();
		}

		private void PropertyList_SelectedItemsChanged(ScrollListBase sender)
		{
			RefreshPropertyPopupMenuVisible();
			RefreshFleetPopupMenu();
		}

		private void RefreshFleetPopupMenu()
		{
			Fleet fleet = SingleSelectedFleet();
			FleetPopupMenu.gameObject.SetActive(fleet != null && FleetsHelper.ShouldShowFleetContextMenu(fleet));
			FleetPopupMenu.Fleet = fleet;
			FleetPopupMenu.Refresh();
		}

		public Fleet SingleSelectedFleet()
		{
			if (PropertyList.SingleSelectedItem != null)
			{
				return PropertyList.SingleSelectedItem.GetFleet();
			}
			return null;
		}

		private void RefreshPropertyPopupMenuVisible()
		{
			PropertyUnitPopupMenu.gameObject.SetActive(PropertyList.SelectedItemCount > 0);
		}

		protected override void update()
		{
			base.update();
			if ((isStale || Time.realtimeSinceStartupAsDouble - lastTimeRefreshPropertyList > MaxTimeBeforeFullRefresh) && Time.realtimeSinceStartupAsDouble > nextPropertyListFullRefresh)
			{
				Refresh();
			}
			else if (Time.realtimeSinceStartupAsDouble > nextTimeRefreshVisible)
			{
				PropertyList.Refresh();
				SetNextRefreshVisibleItemsTime();
			}
			else
			{
				PropertyList.TickVisibleItems();
			}
		}

		private void SetNextRefreshVisibleItemsTime()
		{
			nextTimeRefreshVisible = Time.realtimeSinceStartupAsDouble + RefreshVisibleItemsInterval;
		}

		public void SetNextUpdatePropertyList()
		{
			nextPropertyListFullRefresh = Time.realtimeSinceStartupAsDouble + (double)updatePropertyListFrequency;
		}

		public void AssignUnitsToFleet(List<Unit> selectedUnits, FleetPickerItem fleetToAssignTo)
		{
			switch (fleetToAssignTo.FleetPickerItemType)
			{
			case FleetPickerItemType.CreateNew:
			{
				// A client cannot make a fleet here. CreateAndInitPlayerFleet
				// says so and returns null, and dereferencing it would be a
				// NullReferenceException inside a screen click - swallowed,
				// silent, and looking like the button did nothing.
				//
				// So the ships are handed over instead, which is what lets
				// the host build the fleet when the order arrives.
				Fleet fleet = OrdersHelper.CreateAndInitPlayerFleet(EngineASX.Instance.LocalFaction, selectedUnits[0].Sector, selectedUnits[0].SectorPosition, EngineASX.Instance.PlayerFleetPrefab);

				if (fleet == null)
				{
					Debug.LogWarning("PropertyScreen: cannot make a fleet on this copy."
						+ " The " + selectedUnits.Count + " selected ship(s) will be put in"
						+ " one by the host when they are next given an order.");

					OrdersHelper.NoteShipsForTheHost(selectedUnits);

					break;
				}

				{
					foreach (Unit selectedUnit in selectedUnits)
					{
						if (selectedUnit.Sector != fleet.Sector || Vector3.Distance(selectedUnit.SectorPosition, fleet.SectorPosition) > 1000f)
						{
							Fleet fleet2 = OrdersHelper.CreateAndInitPlayerFleet(EngineASX.Instance.LocalFaction, selectedUnit.Sector, selectedUnit.SectorPosition, EngineASX.Instance.PlayerFleetPrefab);
							OrdersHelper.FindOrCreateNpc(selectedUnit).Fleet = fleet2;
							OrdersHelper.OrderMergeWithFleet(fleet2, fleet, stack: false);
						}
						else
						{
							OrdersHelper.FindOrCreateNpc(selectedUnit).Fleet = fleet;
						}
					}
					break;
				}
			}
			case FleetPickerItemType.Fleet:
				if (fleetToAssignTo.Fleet.NpcPilots.Count + selectedUnits.Count > 8)
				{
					FleetsHelper.TooManyShipsInFleetMessage();
					break;
				}
				{
					foreach (Unit selectedUnit2 in selectedUnits)
					{
						OrdersHelper.FindOrCreateNpc(selectedUnit2).Fleet = fleetToAssignTo.Fleet;
					}
					break;
				}
			}
		}

		protected override void refresh()
		{
			base.refresh();
			SectorFilterControl.Refresh();
			if (Eng != null && Eng.LocalFaction != null && Eng.LocalFaction.IsValidInGame)
			{
				RefreshPropertyPopupMenuVisible();
				RefreshFleetPopupMenu();
				CacheProperty();
				RefreshSelectedItemOptions();
				RefreshPropertyList();
				RefreshFilterLabels();
			}
			isStale = false;
		}

		private void RefreshFilterLabels()
		{
			int countOfUnitType = EngineASX.Instance.LocalFaction.GetCountOfUnitType(UnitType.Ship);
			ShowShipsToggle.GetComponentInChildren<Text>().text = ((countOfUnitType > 0) ? ("Ships (" + TextFormattingHelper.FormatNumber(countOfUnitType) + ")") : "Ships");
			int countOfUnitType2 = EngineASX.Instance.LocalFaction.GetCountOfUnitType(UnitType.Station);
			int validNonMinorStationCount = EngineASX.Instance.LocalFaction.GetValidNonMinorStationCount();
			int num = countOfUnitType2 - validNonMinorStationCount;
			int countOfDockedShips = EngineASX.Instance.LocalFaction.GetCountOfDockedShips();
			ShowStationsToggle.GetComponentInChildren<Text>().text = ((validNonMinorStationCount > 0) ? ("Stations (" + TextFormattingHelper.FormatNumber(validNonMinorStationCount) + ")") : "Stations");
			ShowTurretsToggle.GetComponentInChildren<Text>().text = ((num > 0) ? ("Minor Stations (" + TextFormattingHelper.FormatNumber(num) + ")") : "Minor Stations");
			ShowDockedShipsToggle.GetComponentInChildren<Text>().text = ((countOfDockedShips > 0) ? ("Docked ships (" + TextFormattingHelper.FormatNumber(countOfDockedShips) + ")") : "Docked Ships");
			int countOfUnitType3 = EngineASX.Instance.LocalFaction.GetCountOfUnitType(UnitType.Cargo);
			ShowCargoToggle.GetComponentInChildren<Text>().text = ((countOfUnitType3 > 0) ? ("Cargo (" + TextFormattingHelper.FormatNumber(countOfUnitType3) + ")") : "Cargo");
		}

		private void RefreshPropertyList()
		{
			PropertyList.SetItems(propertyListCache);
			lastTimeRefreshPropertyList = Time.realtimeSinceStartupAsDouble;
			SetNextRefreshVisibleItemsTime();
			SetNextUpdatePropertyList();
		}

		private void PropertyListFilterChanged(bool value)
		{
			Refresh();
		}

		private void PropertyList_SelectedItemChanged(ScrollList<Unit> sender, Unit oldItem, Unit newItem)
		{
			RefreshSelectedItemOptions();
		}

		private void RefreshSelectedItemOptions()
		{
			if (SelectedItemRoot != null)
			{
				SelectedItemRoot.gameObject.SetActive(PropertyList.FirstSelectedItem != null);
				_ = PropertyList.FirstSelectedItem != null;
			}
		}

		private void CacheProperty()
		{
			propertyListCache.Clear();
			Unit localUnit = Eng.LocalUnit;
			IEnumerable<Unit> source = Eng.LocalFaction.Units.Where((Unit e) => ShouldShowUnit(e) && IsUnitFilteredBySector(e));
			List<Unit> list = new List<Unit>(Eng.LocalFaction.Units.Count * 2);
			OrderPropertyNonAlloc(source.Where((Unit e) => !e.IsImmediateDockOwnFaction()), localUnit, list);
			foreach (Unit item in list)
			{
				if (IsUnitFiltered(item))
				{
					propertyListCache.Add(item);
				}
				AddDockedShips(item, propertyListCache);
			}
		}

		private void AddDockedShips(Unit unit, List<Unit> propertyListCache)
		{
			if (!unit.HasDockedUnits())
			{
				return;
			}
			foreach (UnitComponentHolder dockedUnit in unit.GetDockedUnits())
			{
				if (dockedUnit.Unit.Faction == unit.Faction && IsUnitFiltered(dockedUnit.Unit))
				{
					propertyListCache.Add(dockedUnit.Unit);
				}
				AddDockedShips(dockedUnit.Unit, propertyListCache);
			}
		}

		private void OrderPropertyNonAlloc(IEnumerable<Unit> units, Unit localUnit, List<Unit> targetList)
		{
			targetList.AddRange(units.OrderBy((Unit e) => e, ListSorter));
		}

		private bool IsUnitFilteredBySector(Unit unit)
		{
			Sector sectorFilter = SectorFilter;
			if (sectorFilter != null && unit.Sector != sectorFilter)
			{
				return false;
			}
			return true;
		}

		private bool ShouldShowUnit(Unit unit)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed || unit.Sector == null)
			{
				return false;
			}
			if (!ShouldShowUnitType(unit.UnitType))
			{
				return false;
			}
			return true;
		}

		public static bool ShouldShowUnitType(UnitType unitType)
		{
			if ((uint)(unitType - 1) <= 1u || unitType == UnitType.Cargo)
			{
				return true;
			}
			return false;
		}

		private bool IsUnitFiltered(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Cargo:
				if (!ShowCargoToggle.isOn)
				{
					return false;
				}
				break;
			case UnitType.Ship:
				if (!ShowShipsToggle.isOn)
				{
					return false;
				}
				if (unit.IsDocked && !ShowDockedShipsToggle.isOn)
				{
					return false;
				}
				break;
			case UnitType.Station:
				if (unit.IsMinorStation() && !ShowTurretsToggle.isOn)
				{
					return false;
				}
				if (!unit.IsMinorStation() && !ShowStationsToggle.isOn)
				{
					return false;
				}
				break;
			default:
				return false;
			}
			if (!string.IsNullOrEmpty(PropertyListSearchFilter.text) && !unit.GetFriendlyName().Contains(PropertyListSearchFilter.text, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}
			return true;
		}

		protected override void onDisable()
		{
			base.onDisable();
			PropertyList.CleanupOnDisable();
			ViewFilterModal.ToggleActive(active: false);
		}

		public void ResetFiltersWhenShown()
		{
			Sector sector = EngineASX.Instance.ActiveSector;
			if (sector == null)
			{
				sector = EngineASX.Instance.LocalPlayerSector;
			}
			SectorFilter = sector;
			PropertyListSearchFilter.onValueChanged.RemoveListener(OnPropertyListTextFilterChanged);
			PropertyListSearchFilter.text = null;
			PropertyListSearchFilter.onValueChanged.AddListener(OnPropertyListTextFilterChanged);
			PropertyList.ResetScrollPosition();
		}
	}
}
