using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class MissileLockItemList : ScrollList<Missile>
	{
		private const int maxVisibleCount = 4;

		private List<Missile> missileLocks = new List<Missile>();

		private float nextUpdate;

		protected override void update()
		{
			base.update();
			if (!(Time.time > nextUpdate))
			{
				return;
			}
			missileLocks.Clear();
			Unit pUnit = Engine.PlayerUnit;
			if (pUnit != null)
			{
				List<Missile> list = Engine.MissileLockController.GetMissileLocks(Engine.PlayerUnit);
				if (list != null)
				{
					foreach (Missile item in list.OrderBy((Missile e) => Vector3.Distance(pUnit.transform.position, e.transform.position)))
					{
						missileLocks.Add(item);
						if (missileLocks.Count == 4)
						{
							break;
						}
					}
				}
			}
			SetItems(missileLocks);
			nextUpdate = Time.time + 1f;
		}
	}
}
