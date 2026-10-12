using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitHangarBay : MonoBehaviour
	{
		public int BayId;

		public ShipHullType MaxHullType = ShipHullType.Cruiser;

		private UnitComponentHolder dockedUnit;

		private UnitHangar hangar;

		public UnitHangar Hangar
		{
			get
			{
				return hangar;
			}
			set
			{
				if (hangar != value)
				{
					hangar = value;
				}
			}
		}

		public UnitComponentHolder DockedUnit
		{
			get
			{
				return dockedUnit;
			}
			set
			{
				if (dockedUnit != value)
				{
					UnitComponentHolder unitComponentHolder = dockedUnit;
					dockedUnit = value;
					if (unitComponentHolder != null && unitComponentHolder.DockedInHangarBay == this)
					{
						unitComponentHolder.DockedInHangarBay = null;
					}
					if (dockedUnit != null)
					{
						dockedUnit.DockedInHangarBay = this;
					}
					hangar.NotifyBayUnitChanged(this, unitComponentHolder, dockedUnit);
				}
			}
		}

		public void Init()
		{
			Hangar = FindHangar();
			if (hangar == null)
			{
				Debug.LogError("Hangar bay does not have a parent hangar", this);
			}
		}

		public UnitHangar FindHangar()
		{
			return UnityObjectHelper.FindInParentsOrSelf<UnitHangar>(gameObject);
		}

		public bool CanUnitFit(UnitComponentHolder unit)
		{
			return unit.UnitClass.HullType <= hangar.LargestDockableHullType;
		}

		[ContextMenu("Auto Name GameObject")]
		public void AutoNameGameObject()
		{
			name = $"Bay_{BayId}_{Enum.GetName(typeof(ShipHullType), MaxHullType)}";
		}
	}
}
