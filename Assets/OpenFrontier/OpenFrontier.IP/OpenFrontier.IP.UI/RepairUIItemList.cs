using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI
{
	public class RepairUIItemList : ScrollList<RepairItem>
	{
		private RepairUI repairUI;

		protected override void awake()
		{
			base.awake();
			repairUI = UnityObjectHelper.FindInParentsOrSelf<RepairUI>(gameObject);
		}

		protected override void OnRefreshing()
		{
			base.OnRefreshing();
			List<RepairItem> list = new List<RepairItem>();
			Unit unit = repairUI.Unit;
			if (unit != null)
			{
				if (unit.Destructable.CurrentHealth < unit.UnitClass.maxHealth)
				{
					list.Add(new RepairItem());
				}
				for (int i = 0; i < unit.Components.UnitComponents.Count; i++)
				{
					ComponentBase componentBase = unit.Components.UnitComponents[i];
					if (componentBase.HealthNormalized < 1f)
					{
						list.Add(new RepairItem
						{
							Component = componentBase
						});
					}
				}
			}
			SetItems(list);
		}
	}
}
