using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitCargoLoadout : MonoBehaviour
	{
		public bool IgnoreBayCapacity = true;

		public List<UnitCargoLoadoutItem> Items = new List<UnitCargoLoadoutItem>();

		public void ApplyRandomQuantity(Unit unit, float minRandomQuantityPercentage = 1f, float maxRandomQuantityPercentage = 1f)
		{
			if (unit != null)
			{
				if (unit.CargoBayComponent != null)
				{
					foreach (UnitCargoLoadoutItem item in Items)
					{
						if (item.CargoClass != null)
						{
							int num = Mathf.CeilToInt((float)item.Quantity * Random.Range(minRandomQuantityPercentage, maxRandomQuantityPercentage));
							if (num > 0)
							{
								unit.CargoBayComponent.AddToCargo(item.CargoClass, num, IgnoreBayCapacity);
							}
						}
					}
					return;
				}
				Debug.LogWarning($"{this}: Not attached to a unit with a cargo bay");
			}
			else
			{
				Debug.LogWarning($"{this}: Not attached to a gameobject with a unit");
			}
		}

		public void Apply(Unit unit)
		{
			if (unit != null)
			{
				if (unit.CargoBayComponent != null)
				{
					foreach (UnitCargoLoadoutItem item in Items)
					{
						if (item.CargoClass != null && item.Quantity > 0)
						{
							unit.CargoBayComponent.AddToCargo(item.CargoClass, item.Quantity, IgnoreBayCapacity);
						}
					}
					return;
				}
				Debug.LogWarning($"{this}: Not attached to a unit with a cargo bay");
			}
			else
			{
				Debug.LogWarning($"{this}: Not attached to a gameobject with a unit");
			}
		}
	}
}
