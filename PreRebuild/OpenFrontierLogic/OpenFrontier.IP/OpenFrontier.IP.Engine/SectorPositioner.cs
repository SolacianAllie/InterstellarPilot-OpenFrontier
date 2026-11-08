using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class SectorPositioner
	{
		public static void PositionSectors(IEnumerable<Sector> sectors, UniverseBoundsSettings boundsSettings)
		{
			if (sectors.Count() <= 0)
			{
				return;
			}
			int num = -4;
			int num2 = -4;
			foreach (Sector item in sectors.OrderBy((Sector e) => e.UniqueId))
			{
				if (item.IsActive)
				{
					item.transform.position = Vector3.zero;
					continue;
				}
				if (num == 0 && num2 == 0)
				{
					num++;
				}
				item.transform.position = new Vector3((float)num * ((float)boundsSettings.SectorSize / 2f), 0f, (float)num2 * ((float)boundsSettings.SectorSize / 2f));
				num++;
				if (num >= Mathf.Abs(-4))
				{
					num = -4;
					num2++;
				}
			}
		}
	}
}
