using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.Engine
{
	public struct HudTarget
	{
		private Unit unit;

		public Vector3 TargetWorldPosition;

		private bool isUnit;

		public float? ExpiryTime;

		public Unit Unit
		{
			get
			{
				return unit;
			}
			set
			{
				if (unit != value)
				{
					unit = value;
				}
			}
		}

		public bool IsUnit => isUnit;

		public static HudTarget CreateFromUnit(Unit unit)
		{
			return new HudTarget
			{
				isUnit = true,
				unit = unit
			};
		}

		public static HudTarget Position(Vector3 position, float? expiryTime = null)
		{
			return new HudTarget
			{
				TargetWorldPosition = position,
				ExpiryTime = expiryTime
			};
		}

		public Vector3 GetWorldPosition()
		{
			if (Unit != null)
			{
				return Unit.transform.position;
			}
			return TargetWorldPosition;
		}

		public Vector3 GetSectorPosition()
		{
			if (Unit != null)
			{
				return Unit.SectorPosition;
			}
			return TargetWorldPosition - EngineASX.Instance.ActiveSector.transform.position;
		}
	}
}
