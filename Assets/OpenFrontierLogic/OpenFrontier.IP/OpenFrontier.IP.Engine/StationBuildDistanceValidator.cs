using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class StationBuildDistanceValidator : MonoBehaviour
	{
		[ContextMenu("Validate")]
		public void Validate()
		{
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (!(item != null) || !item.IsValidAndNotDestroyed)
					{
						continue;
					}
					Collider component = item.GetComponent<Collider>();
					if (!(component != null) || component.isTrigger)
					{
						continue;
					}
					int num = Physics.OverlapSphereNonAlloc(item.transform.position, GameController.Instance.GameSettings.MinDistanceBetweenStations, EngineASX.ColliderCache, GameController.Instance.StationsMask, QueryTriggerInteraction.Ignore);
					for (int i = 0; i < num; i++)
					{
						Unit component2 = EngineASX.ColliderCache[i].GetComponent<Unit>();
						if (component2 != null && component2.IsValidAndNotDestroyed && component2 != item)
						{
							float num2 = Vector3.Distance(component2.transform.position, item.transform.position);
							Debug.LogWarningFormat(item, "Station {0} too close to {1}. Distance: {2}", item, component2, num2);
						}
					}
				}
			}
		}
	}
}
