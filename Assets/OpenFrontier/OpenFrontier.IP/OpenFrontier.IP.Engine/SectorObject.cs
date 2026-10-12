using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class SectorObject : MonoBehaviour
	{
		private bool isInActiveSector;

		private Sector sector;

		public Sector Sector
		{
			get
			{
				return sector;
			}
			set
			{
				SetSector(value, updateGameObjectParent: true);
			}
		}

		public virtual bool IsStatic => false;

		public bool IsInActiveSector
		{
			get
			{
				return isInActiveSector;
			}
			private set
			{
				isInActiveSector = value;
			}
		}

		public virtual Vector3 SectorPosition => transform.localPosition;

		public virtual bool IsValid => sector != null;

		public virtual bool ShouldZeroPositionY => false;

		public virtual float ObjectRadius => 0f;

		public virtual Vector3 Velocity => Vector3.zero;

		public void SetSector(Sector value, bool updateGameObjectParent)
		{
			if (sector != value)
			{
				Sector oldSector = sector;
				sector = value;
				RefreshIsInActiveSector();
				if (updateGameObjectParent)
				{
					OnUpdateParent();
				}
				OnSectorChanged(oldSector);
			}
		}

		public virtual float GetSpeed()
		{
			return 0f;
		}

		protected virtual void OnSectorChanged(Sector oldSector)
		{
		}

		protected virtual void OnUpdateParent()
		{
			if (this != null)
			{
				if (sector != null)
				{
					transform.SetParent(sector.transform, worldPositionStays: true);
				}
				else
				{
					transform.SetParent(null, worldPositionStays: true);
				}
			}
		}

		public void RefreshIsInActiveSector()
		{
			isInActiveSector = sector != null && sector == EngineASX.Instance.ActiveSector;
		}

		public void ZeroPositionY()
		{
			Vector3 localPosition = transform.localPosition;
			localPosition.y = 0f;
			transform.localPosition = localPosition;
		}

		public virtual bool ShouldTreatAtStatic()
		{
			return IsStatic;
		}
	}
}
