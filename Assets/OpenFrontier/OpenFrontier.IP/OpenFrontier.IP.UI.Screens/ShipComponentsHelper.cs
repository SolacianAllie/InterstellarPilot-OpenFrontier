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

		/// <summary>
		/// The bays of one family, in the order the trade screen wants them.
		/// </summary>
		/// <remarks>
		/// A hard filter, not a sort: the dropdown chooses what is on screen
		/// rather than rearranging it, so a family is either there or it is not.
		///
		/// Legacy is what every existing bay in the game is, and the screen opens
		/// on it, so the default view of every ship is unchanged - the same bays,
		/// in the same order, as before any of this. Bays move out of it only as
		/// they are classified, which is also how the number of bays left to do
		/// stays visible.
		///
		/// Order inside the family is the one GetUnitVisibleBays produced, so
		/// like things still sit together.
		/// </remarks>
		public static List<ComponentBay> GetBaysOfTier(IEnumerable<ComponentBay> orderedBays, BayTier tier)
		{
			List<ComponentBay> bays = new List<ComponentBay>();

			foreach (ComponentBay bay in orderedBays)
			{
				if (bay != null && bay.Tier == tier)
				{
					bays.Add(bay);
				}
			}

			return bays;
		}
	}
}