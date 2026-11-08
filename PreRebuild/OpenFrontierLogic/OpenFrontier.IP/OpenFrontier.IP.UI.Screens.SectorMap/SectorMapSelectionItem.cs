using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public struct SectorMapSelectionItem
	{
		public Unit Unit;

		public Sector Sector;

		public Vector3 SectorPosition;

		public bool HadUnit;

		public Vector3 ActualWorldPosition
		{
			get
			{
				if (Unit != null)
				{
					return Unit.transform.position;
				}
				return Sector.ToWorldPosition(SectorPosition);
			}
		}

		public SectorMapSelectionItem(Unit unit, Sector sector, Vector3 sectorPosition)
		{
			this = default;
			Unit = unit;
			Sector = sector;
			SectorPosition = sectorPosition;
			HadUnit = Unit != null;
		}

		public static SectorMapSelectionItem FromUnit(Unit unit)
		{
			return new SectorMapSelectionItem(unit, unit.Sector, unit.SectorPosition);
		}

		public static SectorMapSelectionItem FromSectorPosition(Sector sector, Vector3 sectorPosition)
		{
			return new SectorMapSelectionItem(null, sector, sectorPosition);
		}

		public SectorTarget ToSectorTargetPosition()
		{
			return SectorTarget.FromSectorPosition(Sector, SectorPosition);
		}
	}
}
