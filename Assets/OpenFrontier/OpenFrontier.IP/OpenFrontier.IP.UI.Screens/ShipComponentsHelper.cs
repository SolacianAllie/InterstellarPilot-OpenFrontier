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
		/// Picks out the bays that begin a new group in an already-ordered list.
		/// </summary>
		/// <remarks>
		/// Takes the output of <see cref="GetUnitVisibleBays"/> as-is, because
		/// the grouping is already there: the list is sorted by
		/// BayType.OrderInTradeUI and then by name, so bays of one type are
		/// contiguous. Nothing has to be sorted a second time - it only has to
		/// be marked where it changes.
		///
		/// Comparing the bay type as a value rather than by name, because
		/// ComponentBayType is a MonoBehaviour and Unity's overloaded == is what
		/// makes this null-safe. Two bays with a null BayType are one group,
		/// not two - hence havePrevious, so a null type does not make every
		/// row look like the start of something.
		/// </remarks>
		public static HashSet<ComponentBay> GetGroupStartBays(IEnumerable<ComponentBay> orderedBays)
		{
			HashSet<ComponentBay> starts = new HashSet<ComponentBay>();
			ComponentBayType previous = null;
			bool havePrevious = false;

			foreach (ComponentBay bay in orderedBays)
			{
				if (bay == null)
				{
					continue;
				}

				if (!havePrevious || previous != bay.BayType)
				{
					starts.Add(bay);
				}

				previous = bay.BayType;
				havePrevious = true;
			}

			return starts;
		}
	}
}
