using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI.Screens
{
	public static class ShipComponentsHelper
	{
		public static IEnumerable<ComponentBay> GetUnitVisibleBays(Unit unit)
		{
			return from e in unit.Components.Bays
				where !e.BayType.IgnoreInTradeUI
				orderby e.BayType.OrderInTradeUI, e.name
				select e;
		}
	}
}
