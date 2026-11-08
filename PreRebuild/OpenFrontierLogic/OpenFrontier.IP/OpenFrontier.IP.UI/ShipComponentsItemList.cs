using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens;

namespace OpenFrontier.IP.UI
{
	public class ShipComponentsItemList : ScrollList<ComponentBay>
	{
		public void SetItemsFromUnitComponents(UnitComponentHolder unitComponents)
		{
			IEnumerable<ComponentBay> unitVisibleBays = ShipComponentsHelper.GetUnitVisibleBays(unitComponents.Unit);
			SetItems(unitVisibleBays);
		}
	}
}
