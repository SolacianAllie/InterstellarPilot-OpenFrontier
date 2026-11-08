using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMap : MonoBehaviour
	{
		public float MinTargetSpriteSize = 35f;

		public float SelectedUnitSpriteSizeMultiplier = 1.4f;

		public float SelectedUnitSpriteSizeFudge = 11.4f;

		public Toggle ShowLabelsToggle;

		public Sector Sector;

		public float UnitSizeMultiplierStations = 1f;

		public float UnitSizeMultiplierShips = 1.4f;

		public float UnitSizeMultiplierOther = 1f;

		public float MapAreaBorderDistance = 200f;

		public RectTransform MapAreaTransform;

		private List<SectorMapItem> mapItems = new List<SectorMapItem>();

		public ScrollRect MapScrollRect;

		public float ShowLabelsZoomLevel = 16f;

		public float ShowMajorStationsZoomLevel = 16f;

		public float ShowEverythingZoomLevel = 16f;

		public Transform OverlayRoot;

		public Transform ShipAndStationUnitsRoot;

		public Transform WormholesRoot;

		public Transform OtherUnitsRoot;

		public Transform BackgroundUnitsRoot;

		public Transform ScanRangeItemsRoot;

		public Transform GridLinesItemsRoot;

		public float WorldToScreenConversion = 1f;

		private List<SectorMapItem> itemCache = new List<SectorMapItem>();

		public float MinPlayerUnitDrawSize = 30f;

		public float MinDrawSize = 20f;

		public SectorMapCurrentTargetController SectorMapCurrentTargetController;

		public Func<Unit, bool> CustomUnitDisplayFilter;

		public Func<SectorMapSelectionItem, bool> CustomSelectionFilter;

		public SectorMapSelectionItem? SelectedObject
		{
			get
			{
				if (SectorMapCurrentTargetController != null)
				{
					return SectorMapCurrentTargetController.SelectedObject;
				}
				return null;
			}
			set
			{
				if (SectorMapCurrentTargetController != null)
				{
					SectorMapCurrentTargetController.SelectedObject = value;
					SectorMapCurrentTargetController.Refresh();
				}
			}
		}

		public Unit SelectedUnit
		{
			get
			{
				if (SectorMapCurrentTargetController != null)
				{
					return SectorMapCurrentTargetController.SelectedUnit;
				}
				return null;
			}
			set
			{
				if (SectorMapCurrentTargetController != null)
				{
					SectorMapCurrentTargetController.SelectedUnit = value;
					SectorMapCurrentTargetController.Refresh();
				}
			}
		}

		public IEnumerable<SectorMapItem> Items => mapItems;

		public void OnShown()
		{
			if (SectorMapCurrentTargetController != null)
			{
				SectorMapCurrentTargetController.AutopickHudTarget();
			}
		}

		public bool CanSelectUnit(Unit unit)
		{
			if (!Unit.UnitTypeIsTargettable(unit.UnitType))
			{
				return false;
			}
			if (CustomSelectionFilter != null)
			{
				return CustomSelectionFilter(SectorMapSelectionItem.FromUnit(unit));
			}
			return true;
		}

		public bool CanSelectSectorPosition(Vector3 sectorPosition)
		{
			if (!UnitOutOfBoundsValidator.IsLocalPositionWithinLowerBounds(sectorPosition))
			{
				return false;
			}
			if (CustomSelectionFilter != null)
			{
				return CustomSelectionFilter(SectorMapSelectionItem.FromSectorPosition(Sector, sectorPosition));
			}
			return true;
		}

		public void OnSectorChanged()
		{
			if (SectorMapCurrentTargetController != null)
			{
				SectorMapCurrentTargetController.Sector = Sector;
				SectorMapCurrentTargetController.AutopickHudTarget();
			}
		}

		internal Transform GetTransformTargetForUnit(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Ship:
			case UnitType.Station:
				return ShipAndStationUnitsRoot;
			case UnitType.Cargo:
			case UnitType.Asteroid:
			case UnitType.Debris:
			case UnitType.NavBuoy:
				return OtherUnitsRoot;
			case UnitType.Wormhole:
				return WormholesRoot;
			case UnitType.Waypoint:
				return OverlayRoot;
			default:
				return BackgroundUnitsRoot;
			}
		}

		public void Refresh()
		{
			if (SectorMapCurrentTargetController != null)
			{
				SectorMapCurrentTargetController.Refresh();
			}
		}

		public void ClearItems()
		{
			foreach (SectorMapItem mapItem in mapItems)
			{
				mapItem.SetActivated(active: false);
				itemCache.Add(mapItem);
			}
			mapItems.Clear();
		}

		public void RepositionMapItems()
		{
			foreach (SectorMapItem mapItem in mapItems)
			{
				mapItem.Reposition();
			}
		}

		public void UpdateMapSize()
		{
			float num = GetMapWorldRadius() * 2f * WorldToScreenConversion;
			MapAreaTransform.sizeDelta = new Vector2(num, num);
		}

		public void AddItem(SectorMapItem item)
		{
			mapItems.Add(item);
		}

		public void SetViewWorldPosition(Vector3 worldPosition)
		{
			Vector3 localPosition = ConvertWorldToScreen(worldPosition);
			MapAreaTransform.localPosition = localPosition;
		}

		public Vector3 GetViewWorldPosition()
		{
			Vector3 localPosition = MapAreaTransform.localPosition;
			return ConvertMapAreaLocalPositionToWorld(localPosition);
		}

		public Vector3 ConvertMapAreaLocalSectorPosition(Vector3 screenPosition)
		{
			return new Vector3
			{
				x = screenPosition.x / WorldToScreenConversion,
				y = 0f,
				z = screenPosition.y / WorldToScreenConversion
			};
		}

		public Vector3 ConvertMapAreaLocalPositionToWorld(Vector3 screenPosition)
		{
			return ConvertMapAreaLocalSectorPosition(screenPosition) + Sector.transform.position;
		}

		public Vector3 ConvertWorldToScreen(Vector3 worldPosition)
		{
			worldPosition -= Sector.transform.position;
			return new Vector3
			{
				x = worldPosition.x * WorldToScreenConversion,
				y = worldPosition.z * WorldToScreenConversion
			};
		}

		public T GetNewItemWidget<T>(T prefab, Transform parent) where T : SectorMapItem
		{
			T val = GetMapItemFromCache(prefab);
			if (val == null)
			{
				val = UnityObjectHelper.InstantiateAndGetComponent(prefab);
				val.SectorMap = this;
			}
			val.transform.SetParent(parent, worldPositionStays: false);
			return val;
		}

		public T CreateAndActivateItemWidget<T>(T prefab, Transform parent) where T : SectorMapItem
		{
			T newItemWidget = GetNewItemWidget(prefab, parent);
			newItemWidget.SetActivated(active: true);
			return newItemWidget;
		}

		private T GetMapItemFromCache<T>(T prefab) where T : SectorMapItem
		{
			for (int i = 0; i < itemCache.Count; i++)
			{
				SectorMapItem sectorMapItem = itemCache[i];
				if (sectorMapItem.GetType() == prefab.GetType())
				{
					itemCache.RemoveAt(i);
					return (T)sectorMapItem;
				}
			}
			return null;
		}

		public float GetMapWorldRadius()
		{
			return EngineASX.Instance.GameSettings.UniverseBoundsSettings.MaxUnitDistanceFromOriginUpperBound + MapAreaBorderDistance;
		}

		public void CenterOnLocalUnit()
		{
			CenterOnUnit(EngineASX.Instance.LocalUnit);
		}

		public void CenterOnUnit(Unit unit)
		{
			if (unit != null && unit.Sector == Sector)
			{
				CenterOnSectorPosition(unit.SectorPosition);
			}
		}

		public void SelectAndCenterOnSectorPosition(Vector3 sectorPosition)
		{
			SectorMapSelectionItem item = SectorMapSelectionItem.FromSectorPosition(Sector, sectorPosition);
			SectorMapCurrentTargetController.TrySelect(item);
			CenterOnSectorPosition(sectorPosition);
		}

		public void CenterOnSectorPosition(Vector3 sectorPosition)
		{
			CenterOnWorldPosition(Sector.ToWorldPosition(sectorPosition));
		}

		public void CenterOnWorldPosition(Vector3 worldPosition)
		{
			if (!UnitOutOfBoundsValidator.IsLocalPositionWithinBounds(Sector.ToLocalPosition(worldPosition)))
			{
				Debug.LogWarning($"Cannot center on world position. Position is out of bounds. Sector: {Sector}. Local Position: {worldPosition - Sector.transform.position}");
				return;
			}
			Vector3 vector = ConvertWorldToScreen(worldPosition);
			MapAreaTransform.localPosition = -vector;
			MapScrollRect.velocity = Vector2.zero;
		}

		public bool ShouldShowScanRangeItemForUnit(Unit unit)
		{
			if (unit != null && unit.IsValidAndNotDestroyed && unit.Components != null && unit.Components.ScanRange > 0f)
			{
				return unit.IsPlayerOrAllied();
			}
			return false;
		}

		public bool ShowUnitAtCurrentZoomLevel(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Ship:
				if (unit.Faction != null && unit.Faction != EngineASX.Instance.LocalFaction && unit.Faction.IsHostileToOrAlwaysHostileTo(EngineASX.Instance.LocalFaction))
				{
					return true;
				}
				break;
			case UnitType.Station:
				if (!unit.IsMinorStation())
				{
					return true;
				}
				break;
			case UnitType.Wormhole:
			case UnitType.Asteroid:
			case UnitType.GasCloud:
			case UnitType.Waypoint:
				return true;
			}
			if (unit == SelectedUnit)
			{
				return true;
			}
			return WorldToScreenConversion >= GameController.Instance.GameSettings.SectorMapSettings.UnimportantZoomLevel;
		}

		public float GetImageDrawSize(float objectRadius, float sizeMultiplier = 1f)
		{
			return objectRadius * 2f * sizeMultiplier;
		}

		private float GetUnitSizeMultiplier(UnitType unitType)
		{
			return unitType switch
			{
				UnitType.Station => UnitSizeMultiplierStations, 
				UnitType.Ship => UnitSizeMultiplierShips, 
				_ => UnitSizeMultiplierOther, 
			};
		}

		public Vector2 GetUnitDrawSizes(Unit unit)
		{
			float unitSizeMultiplier = GetUnitSizeMultiplier(unit.UnitType);
			float imageDrawSize = GetImageDrawSize(unit.Radius, unitSizeMultiplier);
			return new Vector2(imageDrawSize, imageDrawSize);
		}

		private bool CanShowUnitType(UnitType unitType)
		{
			switch (unitType)
			{
			case UnitType.Ship:
			case UnitType.Station:
			case UnitType.Cargo:
			case UnitType.Wormhole:
			case UnitType.Asteroid:
			case UnitType.GasCloud:
			case UnitType.Waypoint:
				return true;
			default:
				return false;
			}
		}

		public bool ShouldShowUnitOnMap(Unit unit, Sector sector)
		{
			if (unit == null || !unit.IsValidAndNotDestroyed || unit.Sector != sector || unit.IsDocked)
			{
				return false;
			}
			if (!CanShowUnitType(unit.UnitType))
			{
				return false;
			}
			if (CustomUnitDisplayFilter != null && !CustomUnitDisplayFilter(unit))
			{
				return false;
			}
			if (unit.IsPlayerOrAllied() || unit.HasSameRootUnitAsPlayer())
			{
				return true;
			}
			if (unit.IsDiscoverableType)
			{
				return EngineASX.Instance.LocalFaction.Intel.IsUnitDiscoveredOrOwned(unit, GetTargetPersistTime());
			}
			return true;
		}

		public float GetTargetPersistTime()
		{
			if (Sector.IsActive)
			{
				return EngineASX.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTimeActiveSector;
			}
			return EngineASX.Instance.GameSettings.IntelSettings.NonStaticTargetPersistTime;
		}

		public static bool AlwaysShowUnitOnMap(Unit unit)
		{
			switch (unit.UnitType)
			{
			case UnitType.Wormhole:
			case UnitType.Asteroid:
			case UnitType.GasCloud:
			case UnitType.Waypoint:
				return true;
			default:
				return false;
			}
		}

		public static bool PreferToShowUnitOnMap(Unit unit, NewOrderTarget newOrderTarget)
		{
			if (AlwaysShowUnitOnMap(unit))
			{
				return true;
			}
			if (unit.IsMajorStation())
			{
				return true;
			}
			return newOrderTarget.IsUnitOrdered(unit);
		}

		public float GetUnitBracketsDrawSize(Unit unit)
		{
			float num = 5f;
			if (unit != null)
			{
				num = SelectedUnitSpriteSizeFudge + GetImageDrawSize(unit.UnitClass.ShieldRingRadius, SelectedUnitSpriteSizeMultiplier);
			}
			num *= WorldToScreenConversion;
			return Mathf.Max(MinTargetSpriteSize, num);
		}
	}
}
