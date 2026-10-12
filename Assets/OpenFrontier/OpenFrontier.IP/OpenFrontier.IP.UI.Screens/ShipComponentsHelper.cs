using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.ComponentTrade;

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
		/// Builds what the bay list actually shows: the bays of one tier, in
		/// order, with a heading above each run of same-typed bays.
		/// </summary>
		/// <remarks>
		/// Takes the output of <see cref="GetUnitVisibleBays"/> as-is, because
		/// the grouping is already there: that list is sorted by
		/// BayType.OrderInTradeUI and then by name, so bays of one type are
		/// contiguous. Nothing has to be sorted a second time - it only has to
		/// be broken up where it changes.
		///
		/// The tier is a hard filter rather than a sort, which means the dropdown
		/// chooses what is on screen rather than rearranging it. Legacy is what
		/// every existing bay is, so the default view of every ship in the game
		/// is unchanged by any of this - the same bays in the same order, now
		/// with headings.
		///
		/// Bay types are compared as values, not by name, because
		/// ComponentBayType is a MonoBehaviour and Unity's overloaded == is what
		/// makes that null-safe. Two bays with a null BayType are one group and
		/// not two, which is what havePrevious is for.
		/// </remarks>
		public static List<object> BuildBayListItems(IEnumerable<ComponentBay> orderedBays, BayTier tier)
		{
			List<object> items = new List<object>();
			ComponentBayType previous = null;
			bool havePrevious = false;

			foreach (ComponentBay bay in orderedBays)
			{
				if (bay == null || bay.Tier != tier)
				{
					continue;
				}

				if (!havePrevious || previous != bay.BayType)
				{
					items.Add(new ComponentBayGroupHeader(GetBayGroupHeading(bay.BayType)));
				}

				items.Add(bay);
				previous = bay.BayType;
				havePrevious = true;
			}

			return items;
		}

		/// <summary>
		/// The text a bay type's heading shows.
		/// </summary>
		/// <remarks>
		/// From the bay type's own FriendlyName, which all 14 of them populate,
		/// so the labels live with the data and can be changed from a bay type
		/// prefab rather than by editing code. Uppercased so a heading reads as
		/// one. The enum name is the fallback for a bay type that has not been
		/// given a FriendlyName, because "Turret" is better than nothing and
		/// better than a null reference in a list row.
		/// </remarks>
		private static string GetBayGroupHeading(ComponentBayType bayType)
		{
			if (bayType == null)
			{
				return "Other";
			}

			string name = string.IsNullOrWhiteSpace(bayType.FriendlyName)
				? bayType.BayType.ToString()
				: bayType.FriendlyName;

			return name.ToUpperInvariant();
		}
	}
}
