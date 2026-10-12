using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.Misc
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class FudgeSectorLightDirectionSeeder : MonoBehaviour
	{
		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Setting up nicer sector light directions", this, 1);
			}
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				if (!(sector.LightRotation != Quaternion.identity))
				{
					continue;
				}
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Planet);
				if (unitsByType != null)
				{
					float num = 0.75f;
					Vector3 lhs = Vector3.Normalize(unitsByType.FirstOrDefault((Unit e) => e.GetComponent<Moon>() == null).transform.localPosition);
					Vector3 eulerAngles = sector.LightRotation.eulerAngles;
					eulerAngles.x = 0f;
					if (Mathf.Abs(Vector3.Dot(lhs, Quaternion.Euler(eulerAngles) * Vector3.forward)) > num)
					{
						sector.LightRotation *= Quaternion.Euler(0f, 90f, 0f);
					}
				}
			}
		}
	}
}
