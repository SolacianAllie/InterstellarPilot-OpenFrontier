using System.Collections.Generic;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	public class ComponentTradeBayList : ScrollList<ComponentBay>
	{
		private HashSet<ComponentBay> groupStarts = new HashSet<ComponentBay>();

		/// <summary>
		/// Records which bays begin a new group of same-typed bays.
		/// </summary>
		/// <remarks>
		/// Held by the list rather than by each row because rows are pooled: a
		/// row GameObject gets reused for a different bay as the list scrolls,
		/// so anything stashed on the row itself would be stale by the time it
		/// was looked at. The list is the one thing that outlives the rows.
		///
		/// Deliberately not an extra row in the list. ScrollList&lt;T&gt; is typed
		/// to ComponentBay and every row reads Item.GetFriendlyName(), so a
		/// header row would need a union type or a null-Item code path through
		/// a control shared by every list in the game. Drawing the rule inside
		/// the row that starts the group gets the same reading without that.
		/// </remarks>
		public void SetGroupStarts(HashSet<ComponentBay> starts)
		{
			groupStarts = starts ?? new HashSet<ComponentBay>();
		}

		public bool StartsGroup(ComponentBay bay)
		{
			return bay != null && groupStarts.Contains(bay);
		}
	}
}