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
		/// order, under a single heading naming that tier.
		/// </summary>
		/// <remarks>
		/// One heading, not one per kind of slot. The dropdown already says which
		/// family is on screen, so labelling each run of turrets and each run of
		/// shields underneath it says the same thing twice over and buries the
		/// answer the player is actually looking for. An earlier version did
		/// exactly that and read as TURRET / ENGINE / POWER GENERATOR / SHIELD
		/// stacked up, which is not a category list - it is the flat list with
		/// louder headings.
		///
		/// The order inside the section is still the one
		/// <see cref="GetUnitVisibleBays"/> produced - sorted by
		/// BayType.OrderInTradeUI then name - so like things sit together, which
		/// is useful without being labelled.
		///
		/// The tier is a hard filter rather than a sort, so the dropdown chooses
		/// what is on screen rather than rearranging it. Legacy is what every
		/// existing bay is, so the default view of every ship in the game is
		/// unchanged by any of this.
		/// </remarks>
		public static List<object> BuildBayListItems(IEnumerable<ComponentBay> orderedBays, BayTier tier)
		{
			List<object> bays = new List<object>();

			foreach (ComponentBay bay in orderedBays)
			{
				if (bay != null && bay.Tier == tier)
				{
					bays.Add(bay);
				}
			}

			if (bays.Count == 0)
			{
				// No heading over nothing. ScrollList's own "No bays found"
				// label is the better answer than a header followed by an
				// empty panel, and it is already wired.
				return bays;
			}

			bays.Insert(0, new ComponentBayGroupHeader(GetBayTierLabel(tier)));

			return bays;
		}

		/// <summary>
		/// What a tier is called, in the dropdown and in the heading alike.
		/// </summary>
		/// <remarks>
		/// One function for both, deliberately. A heading that said "CORE
		/// INTERNALS" under a dropdown that said "CoreInternal" would be two
		/// spellings of one thing, and the second would drift.
		///
		/// Spelled out by hand because the enum names are not words:
		/// "OptionalInternal" is not something a player reads, and "UtilityMount"
		/// reads as a file format.
		/// </remarks>
		public static string GetBayTierLabel(BayTier tier)
		{
			switch (tier)
			{
				case BayTier.Legacy:
					return "LEGACY";

				case BayTier.Hardpoint:
					return "Hardpoints";

				case BayTier.CoreInternal:
					return "Core Internals";

				case BayTier.OptionalInternal:
					return "Optional Internals";

				case BayTier.UtilityMount:
					return "Utility Mounts";

				default:
					return tier.ToString();
			}
		}
	}
}
