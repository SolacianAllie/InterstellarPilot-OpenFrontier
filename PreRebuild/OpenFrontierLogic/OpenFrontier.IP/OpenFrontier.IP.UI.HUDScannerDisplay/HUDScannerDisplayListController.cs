using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.UI.HUDScannerDisplay
{
	public class HUDScannerDisplayListController : MonoBehaviour
	{
		public bool ReverseScannerListOrder;

		private static List<HUDScannerItem> itemCache = new List<HUDScannerItem>(40);

		public HudScreen HUD;

		private float nextListRefreshTime;

		public float ListRefreshFrequency = 1f;

		public HUDScannerDisplayList ScannerDisplayList;

		public HUDScannerDisplayFilterToggles FilterToggles;

		private static HashSet<SectorObject> missionPathObjects = new HashSet<SectorObject>();

		private void Start()
		{
			HUD.CurrentTargetChanged += HUD_CurrentTargetChanged;
			FilterToggles.AsteroidsToggle.Toggle.onValueChanged.AddListener(OnFilterToggleValueChanged);
			FilterToggles.ShipsToggle.Toggle.onValueChanged.AddListener(OnFilterToggleValueChanged);
			FilterToggles.StationsToggle.Toggle.onValueChanged.AddListener(OnFilterToggleValueChanged);
			FilterToggles.WormholesToggle.Toggle.onValueChanged.AddListener(OnFilterToggleValueChanged);
			FilterToggles.CargoToggle.Toggle.onValueChanged.AddListener(OnFilterToggleValueChanged);
			FilterToggles.HostilesToggle.Toggle.onValueChanged.AddListener(OnFilterToggleValueChanged);
		}

		private void OnFilterToggleValueChanged(bool value)
		{
			RefreshItems();
			ScannerDisplayList.ResetScrollPosition();
		}

		private void ScannerDisplayList_SelectedItemChanged(ScrollList<HUDScannerItem> sender, HUDScannerItem oldItem, HUDScannerItem newItem)
		{
			if (newItem != null && IsUnitValidTarget(newItem.Unit))
			{
				HUD.CurrentTarget = newItem.Unit;
			}
		}

		private void HUD_CurrentTargetChanged(HudScreen sender, Unit oldTarget)
		{
			if (gameObject.activeSelf)
			{
				SyncSelectedListItemWithHud();
			}
		}

		private void SyncSelectedListItemWithHud()
		{
			foreach (HUDScannerItem activeItem in ScannerDisplayList.ActiveItems)
			{
				if (activeItem.Unit == HUD.CurrentTarget)
				{
					ScannerDisplayList.FirstSelectedItem = activeItem;
					return;
				}
			}
			ScannerDisplayList.FirstSelectedItem = null;
		}

		public void Tick()
		{
			if (gameObject.activeSelf)
			{
				if (Time.time > nextListRefreshTime)
				{
					RefreshItems();
					SetNextAutoRefreshTime();
				}
				ScannerDisplayList.TickVisibleItems();
			}
		}

		private void SetNextAutoRefreshTime()
		{
			nextListRefreshTime = Time.time + ListRefreshFrequency;
		}

		private SectorObject GetCustomPathObject()
		{
			if (EngineASX.Instance.LocalPlayer.WaypointController.CustomPath.HasPath)
			{
				return EngineASX.Instance.LocalPlayer.WaypointController.CustomPath.Waypoints[0].TargetSectorObject;
			}
			return null;
		}

		private void AddMissionPathObjectsToHashSet(HashSet<SectorObject> hastSet)
		{
			foreach (PlayerWaypointPath missionPath in EngineASX.Instance.LocalPlayer.WaypointController.MissionPaths)
			{
				if (missionPath.HasPath && missionPath.Waypoints[0].TargetSectorObject != null)
				{
					hastSet.Add(missionPath.Waypoints[0].TargetSectorObject);
				}
			}
		}

		private HUDScannerScanResult GetItems()
		{
			HUDScannerScanResult result = default;
			HUDScannerScanFilterResult filterResult = default;
			itemCache.Clear();
			bool showAll = FilterToggles.IsShowingAll();
			Unit localUnit = HUD.Eng.LocalUnit;
			if (localUnit != null && localUnit.IsValid)
			{
				List<HudScannerUnit> scannedUnitCache = HUD.AutoScanner.ScannedUnitCache;
				SectorObject customPathObject = GetCustomPathObject();
				missionPathObjects.Clear();
				AddMissionPathObjectsToHashSet(missionPathObjects);
				Faction localFaction = HUD.Eng.LocalFaction;
				foreach (HudScannerUnit item2 in scannedUnitCache)
				{
					Unit unit = item2.Unit;
					if (IsUnitValidTarget(unit))
					{
						bool flag = localFaction != null && unit.IsHostileTo(localFaction);
						if ((!result.FilterResult.Hostiles & flag) && (unit.UnitType == UnitType.Ship || unit.UnitType == UnitType.Station))
						{
							filterResult.Hostiles = true;
						}
						switch (unit.UnitType)
						{
						case UnitType.Asteroid:
							filterResult.Asteroids = true;
							break;
						case UnitType.Cargo:
							filterResult.Cargo = true;
							break;
						case UnitType.Wormhole:
							filterResult.Wormholes = true;
							break;
						case UnitType.Ship:
							filterResult.Ships = true;
							break;
						case UnitType.Station:
							filterResult.Stations = true;
							break;
						}
						if (IsUnitFiltered(unit, flag, showAll))
						{
							HUDScannerItem item = new HUDScannerItem
							{
								Unit = unit,
								Distance = item2.DistanceFromLocalUnitIgnoringY,
								IsCustomWaypoint = (customPathObject == unit),
								IsMissionWaypoint = missionPathObjects.Contains(unit)
							};
							itemCache.Add(item);
						}
					}
				}
			}
			result.FilterResult = filterResult;
			if (ReverseScannerListOrder)
			{
				result.Items = itemCache.OrderByDescending((HUDScannerItem e) => e.Distance).ToList();
				return result;
			}
			result.Items = itemCache.OrderBy((HUDScannerItem e) => e.Distance).ToList();
			return result;
		}

		private bool IsUnitValidTarget(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed && unit.IsTargettable(HUD.Eng.LocalFaction))
			{
				return unit.IsInActiveSector;
			}
			return false;
		}

		private bool IsUnitFiltered(Unit unit, bool isHostile, bool showAll)
		{
			switch (unit.UnitType)
			{
			case UnitType.Ship:
				if (!showAll && !FilterToggles.ShipsToggle.Toggle.isOn)
				{
					return FilterToggles.HostilesToggle.Toggle.isOn & isHostile;
				}
				return true;
			case UnitType.Station:
				if (!showAll && !FilterToggles.StationsToggle.Toggle.isOn)
				{
					return FilterToggles.HostilesToggle.Toggle.isOn & isHostile;
				}
				return true;
			case UnitType.Asteroid:
				if (!showAll)
				{
					return FilterToggles.AsteroidsToggle.Toggle.isOn;
				}
				return true;
			case UnitType.Cargo:
				if (!showAll)
				{
					return FilterToggles.CargoToggle.Toggle.isOn;
				}
				return true;
			case UnitType.Wormhole:
				if (!showAll)
				{
					return FilterToggles.WormholesToggle.Toggle.isOn;
				}
				return true;
			default:
				return false;
			}
		}

		public void RefreshItems()
		{
			HUDScannerScanResult items = GetItems();
			ScannerDisplayList.SetItems(items.Items);
			SyncSelectedListItemWithHud();
			FilterToggles.RefreshToggles(items.FilterResult);
		}

		private void OnDisable()
		{
			ScannerDisplayList.SelectedItemChanged -= ScannerDisplayList_SelectedItemChanged;
		}

		public void CleanupOnDisable()
		{
			ScannerDisplayList.CleanupOnDisable();
		}

		public void Show()
		{
			ScannerDisplayList.SelectedItemChanged -= ScannerDisplayList_SelectedItemChanged;
			gameObject.SetActive(value: true);
			RefreshItems();
			ScannerDisplayList.ResetScrollPosition();
			SetNextAutoRefreshTime();
			ScannerDisplayList.SelectedItemChanged += ScannerDisplayList_SelectedItemChanged;
		}
	}
}
