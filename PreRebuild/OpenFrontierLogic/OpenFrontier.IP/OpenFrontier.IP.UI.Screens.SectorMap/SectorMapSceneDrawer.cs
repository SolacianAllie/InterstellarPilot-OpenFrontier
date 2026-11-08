using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Core.Units;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapSceneDrawer : MonoBehaviour
	{
		public SectorMapGridLineItem MajorGridLinePrefab;

		public SectorMapGridLineItem MinorGridLinePrefab;

		public SectorMapScanRangeItem ScanRangeItemPrefab;

		public Sector Sector;

		public SectorMap SectorMap;

		public SectorMapUnitItem UnitWidgetPrefab;

		private static List<Unit> unitCache = new List<Unit>(200);

		public bool ShowUnitLabels => SectorMap.ShowLabelsToggle.isOn;

		public EngineASX Eng => EngineASX.Instance;

		public void RebuildMap()
		{
			SectorMap.ClearItems();
			CreateGridLines();
			if (!(Sector != null) || !(SectorMap != null))
			{
				return;
			}
			unitCache.Clear();
			EngineASX.Instance.LocalFaction.Intel.GetDiscoveredUnitIdsInSectorNonAlloc(Sector, unitCache, SectorMap.GetTargetPersistTime());
			foreach (Unit item3 in unitCache)
			{
				if (item3 != null && SectorMap.ShouldShowScanRangeItemForUnit(item3))
				{
					SectorMapScanRangeItem item = CreateScanRangeItem(ScanRangeItemPrefab, item3);
					SectorMap.AddItem(item);
				}
			}
			foreach (Unit item4 in unitCache)
			{
				if (item4 != null && SectorMap.ShouldShowUnitOnMap(item4, Sector))
				{
					SectorMapItem item2 = CreateUnitWidget(UnitWidgetPrefab, item4);
					SectorMap.AddItem(item2);
				}
			}
			ShowUndiscoverableUnitType(UnitType.GasCloud);
			ShowUndiscoverableUnitType(UnitType.Waypoint);
		}

		public void ShowUndiscoverableUnitType(UnitType unitType)
		{
			List<Unit> unitsByType = Sector.GetUnitsByType(unitType);
			if (unitsByType == null)
			{
				return;
			}
			foreach (Unit item2 in unitsByType)
			{
				if (item2 != null && SectorMap.ShouldShowUnitOnMap(item2, Sector))
				{
					SectorMapItem item = CreateUnitWidget(UnitWidgetPrefab, item2);
					SectorMap.AddItem(item);
				}
			}
		}

		public SectorMapItem CreateUnitWidget(SectorMapUnitItem prefab, Unit unit)
		{
			Transform transformTargetForUnit = SectorMap.GetTransformTargetForUnit(unit);
			SectorMapUnitItem sectorMapUnitItem = SectorMap.CreateAndActivateItemWidget(prefab, transformTargetForUnit);
			sectorMapUnitItem.Unit = unit;
			RefreshUnitShowSprite(sectorMapUnitItem);
			RefreshUnitShowLabel(sectorMapUnitItem);
			sectorMapUnitItem.Refresh();
			return sectorMapUnitItem;
		}

		public SectorMapScanRangeItem CreateScanRangeItem(SectorMapScanRangeItem prefab, Unit unit)
		{
			SectorMapScanRangeItem sectorMapScanRangeItem = SectorMap.CreateAndActivateItemWidget(ScanRangeItemPrefab, SectorMap.ScanRangeItemsRoot.transform);
			sectorMapScanRangeItem.Unit = unit;
			sectorMapScanRangeItem.Refresh();
			return sectorMapScanRangeItem;
		}

		public void CreateAndAddGridLine(SectorMapGridLineItem prefab, Vector3 localSectorPosition, SectorMapGridLineItem.OrientationMode orientation)
		{
			SectorMapGridLineItem item = CreateGridLine(prefab, localSectorPosition, orientation);
			SectorMap.AddItem(item);
		}

		public SectorMapGridLineItem CreateGridLine(SectorMapGridLineItem prefab, Vector3 localSectorPosition, SectorMapGridLineItem.OrientationMode orientation)
		{
			if (SectorMap == null)
			{
				throw new Exception("Missing sector map");
			}
			if (SectorMap.Sector == null)
			{
				throw new Exception("Missing sector");
			}
			SectorMapGridLineItem sectorMapGridLineItem = SectorMap.CreateAndActivateItemWidget(prefab, SectorMap.GridLinesItemsRoot);
			sectorMapGridLineItem.WorldPosition = SectorMap.Sector.transform.position + localSectorPosition;
			sectorMapGridLineItem.Orientation = orientation;
			sectorMapGridLineItem.Refresh();
			return sectorMapGridLineItem;
		}

		public void Tick()
		{
			RefreshItemVisibility();
		}

		public void RefreshItemVisibility()
		{
			foreach (SectorMapItem item in SectorMap.Items)
			{
				SectorMapUnitItem sectorMapUnitItem = item as SectorMapUnitItem;
				if (sectorMapUnitItem != null)
				{
					RefreshUnitShowLabel(sectorMapUnitItem);
					RefreshUnitShowSprite(sectorMapUnitItem);
				}
			}
		}

		public void RefreshUnitLabels()
		{
			foreach (SectorMapItem item in SectorMap.Items)
			{
				SectorMapUnitItem sectorMapUnitItem = item as SectorMapUnitItem;
				if (sectorMapUnitItem != null)
				{
					RefreshUnitShowLabel(sectorMapUnitItem);
				}
			}
		}

		private void RefreshUnitShowSprite(SectorMapUnitItem unitItem)
		{
			unitItem.ShowSprite = ShouldShowUnitSprite(unitItem);
		}

		private void RefreshUnitShowLabel(SectorMapUnitItem unitItem)
		{
			unitItem.ShowLabel = ShouldShowUnitLabel(unitItem);
		}

		private bool ShouldShowUnitSprite(SectorMapUnitItem unitItem)
		{
			if (!SectorMap.ShouldShowUnitOnMap(unitItem.Unit, Sector))
			{
				return false;
			}
			if (unitItem.Unit.HasSameRootUnitAsPlayer() || SectorMap.ShowUnitAtCurrentZoomLevel(unitItem.Unit))
			{
				return true;
			}
			if (unitItem.Unit.IsPlayerMissionPathTargetOrFirstWaypoint() || unitItem.Unit.IsPlayerCustomPathTargetOrFirstWaypointOrPathMarker())
			{
				return true;
			}
			return false;
		}

		private bool ShouldShowUnitLabel(SectorMapUnitItem unitItem)
		{
			if (unitItem.Unit == null)
			{
				return false;
			}
			if (!ShowUnitLabels)
			{
				return false;
			}
			if (SectorMap.SectorMapCurrentTargetController != null && unitItem.Unit == SectorMap.SectorMapCurrentTargetController.SelectedUnit)
			{
				return true;
			}
			if (unitItem.Unit.IsPlayerMissionPathTargetOrFirstWaypoint() || unitItem.Unit.IsPlayerCustomPathTargetOrFirstWaypointOrPathMarker())
			{
				return true;
			}
			switch (unitItem.Unit.UnitType)
			{
			case UnitType.Waypoint:
			{
				UnitWaypoint component = unitItem.Unit.GetComponent<UnitWaypoint>();
				if (component != null)
				{
					if (component.PlayerWaypointPath != null && component.PlayerWaypointPath.HasWaypoint)
					{
						return component.PlayerWaypointPath.Waypoint.Value.TargetUnit == null;
					}
					return false;
				}
				break;
			}
			case UnitType.Wormhole:
				return SectorMap.WorldToScreenConversion >= SectorMap.ShowLabelsZoomLevel;
			case UnitType.Station:
				if (unitItem.Unit.IsMajorStation())
				{
					return SectorMap.WorldToScreenConversion >= SectorMap.ShowMajorStationsZoomLevel;
				}
				return SectorMap.WorldToScreenConversion >= SectorMap.ShowEverythingZoomLevel;
			case UnitType.Ship:
				if (unitItem.Unit.IsOwnedByPlayer)
				{
					Fleet fleet = unitItem.Unit.GetFleet();
					if (SectorMap.SectorMapCurrentTargetController != null && fleet != null && fleet == SectorMap.SectorMapCurrentTargetController.SelectedFleet())
					{
						return false;
					}
					if (fleet == null || (fleet.IdleAndNoObjectives && fleet.Ships.Count < 2))
					{
						return false;
					}
					if (IsPartOfPlayerFleet(unitItem.Unit))
					{
						return false;
					}
				}
				return SectorMap.WorldToScreenConversion >= SectorMap.ShowEverythingZoomLevel;
			}
			return false;
		}

		public bool IsPartOfPlayerFleet(Unit unit)
		{
			if (unit.IsOwnedByPlayer)
			{
				Fleet fleet = unit.GetFleet();
				if (fleet != null && fleet.Ships.Count > 1)
				{
					return unit != fleet.LeaderUnit;
				}
			}
			return false;
		}

		private void CreateGridLines()
		{
			CreateAndAddGridLine(MajorGridLinePrefab, Vector3.zero, SectorMapGridLineItem.OrientationMode.Horizontal);
			CreateAndAddGridLine(MajorGridLinePrefab, Vector3.zero, SectorMapGridLineItem.OrientationMode.Vertical);
			float gridLineSpacing = GameController.Instance.GameSettings.SectorMapSettings.GridLineSpacing;
			int num = (int)(SectorMap.GetMapWorldRadius() / gridLineSpacing);
			for (int i = 1; i < num + 1; i++)
			{
				CreateAndAddGridLine(MinorGridLinePrefab, new Vector3(0f, 0f, (float)i * gridLineSpacing), SectorMapGridLineItem.OrientationMode.Horizontal);
				CreateAndAddGridLine(MinorGridLinePrefab, new Vector3(0f, 0f, (float)(-i) * gridLineSpacing), SectorMapGridLineItem.OrientationMode.Horizontal);
				CreateAndAddGridLine(MinorGridLinePrefab, new Vector3((float)i * gridLineSpacing, 0f, 0f), SectorMapGridLineItem.OrientationMode.Vertical);
				CreateAndAddGridLine(MinorGridLinePrefab, new Vector3((float)(-i) * gridLineSpacing, 0f, 0f), SectorMapGridLineItem.OrientationMode.Vertical);
			}
		}
	}
}
