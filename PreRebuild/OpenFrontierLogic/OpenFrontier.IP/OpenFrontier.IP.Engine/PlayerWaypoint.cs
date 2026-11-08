using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public struct PlayerWaypoint
	{
		public bool HadSceneObject { get; set; }

		public Sector Sector { get; set; }

		public Vector3 SectorPosition { get; set; }

		public Unit TargetUnit { get; set; }

		public bool IsMovingTarget
		{
			get
			{
				if (TargetUnit != null)
				{
					return !TargetUnit.IsStatic;
				}
				return false;
			}
		}

		public static PlayerWaypoint FromUnit(Unit unit)
		{
			return new PlayerWaypoint
			{
				TargetUnit = unit,
				HadSceneObject = true
			};
		}

		public static PlayerWaypoint FromSectorPosition(Sector sector, Vector3 sectorPosition)
		{
			return new PlayerWaypoint
			{
				Sector = sector,
				SectorPosition = sectorPosition
			};
		}

		public static PlayerWaypoint FromWorldNavpoint(WorldNavpoint navpoint)
		{
			if (navpoint.TargetSectorObject != null)
			{
				return FromUnit((Unit)navpoint.TargetSectorObject);
			}
			return FromSectorPosition(navpoint.GetTargetSector(), navpoint.GetTargetSectorPosition());
		}

		public Vector3 GetTargetWorldPosition()
		{
			return GetTargetSector().ToWorldPosition(GetTargetSectorPosition());
		}

		public Vector3 GetTargetSectorPosition()
		{
			if (TargetUnit != null)
			{
				return TargetUnit.SectorPosition;
			}
			return SectorPosition;
		}

		public Sector GetTargetSector()
		{
			if (TargetUnit != null)
			{
				return TargetUnit.Sector;
			}
			return Sector;
		}

		public bool IsValid()
		{
			if (GetTargetSector() != null)
			{
				if (HadSceneObject)
				{
					return TargetUnit != null;
				}
				return true;
			}
			return false;
		}

		public SectorTarget CreateSectorTarget()
		{
			if (TargetUnit != null)
			{
				return SectorTarget.FromUnit(TargetUnit);
			}
			return SectorTarget.FromSectorPosition(Sector, SectorPosition);
		}
	}
}
