using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public struct WorldNavpoint
	{
		public Sector Sector;

		public Vector3 SectorPosition;

		public SectorObject TargetSectorObject;

		public WorldNavpointTargetType TargetType;

		public float ArrivalThreshold;

		public Unit TargetRootSceneObject
		{
			get
			{
				if (TargetSectorObject != null)
				{
					Unit unit = TargetSectorObject as Unit;
					if (unit != null)
					{
						return unit.GetRootUnit();
					}
				}
				return null;
			}
		}

		public static WorldNavpoint FromUnit(Unit unit)
		{
			return new WorldNavpoint
			{
				TargetSectorObject = unit,
				Sector = unit.Sector,
				SectorPosition = unit.SectorPosition
			};
		}

		public static WorldNavpoint FromSectorPosition(Sector sector, Vector3 sectorPosition)
		{
			return new WorldNavpoint
			{
				Sector = sector,
				SectorPosition = sectorPosition
			};
		}

		public Vector3 GetTargetWorldPosition()
		{
			return GetTargetSector().ToWorldPosition(GetTargetSectorPosition());
		}

		public Vector3 GetTargetSectorPosition()
		{
			return SectorPosition;
		}

		public Sector GetTargetSector()
		{
			return Sector;
		}

		public bool IsStaticTarget()
		{
			if (!(TargetSectorObject == null))
			{
				return TargetSectorObject.IsStatic;
			}
			return true;
		}
	}
}
