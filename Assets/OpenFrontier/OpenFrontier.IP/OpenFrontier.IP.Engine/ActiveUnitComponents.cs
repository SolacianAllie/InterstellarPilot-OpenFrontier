using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveUnitComponents : MonoBehaviour
	{
		private List<ActiveTurret> activeTurrets = new List<ActiveTurret>();

		private UnitComponentHolder unitComponents;

		public List<ActiveTurret> ActiveTurrets => activeTurrets;

		public UnitComponentHolder UnitComponents
		{
			get
			{
				return unitComponents;
			}
			set
			{
				if (unitComponents != value)
				{
					unitComponents = value;
					if (unitComponents != null)
					{
						unitComponents.ActiveUnitComponents = this;
					}
				}
			}
		}

		public Unit Unit
		{
			get
			{
				if (unitComponents != null)
				{
					return unitComponents.Unit;
				}
				return null;
			}
		}

		private void OnDestroy()
		{
			for (int i = 0; i < activeTurrets.Count; i++)
			{
				if ((bool)activeTurrets[i])
				{
					Object.Destroy(activeTurrets[i].gameObject);
				}
			}
		}
	}
}
