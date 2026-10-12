using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	/// <summary>
	/// A non-selectable heading in the bay list, naming the group below it.
	/// </summary>
	/// <remarks>
	/// A real object rather than a null standing in for a missing row. The bay
	/// list is a ScrollList&lt;ComponentBay&gt; normally, and the tempting way to
	/// add a heading is to pass null and have the row draw something else -
	/// which means every read of Item in every row has to become a null check,
	/// and a null that slips through selects a bay that does not exist. Passing
	/// an instance of this instead means an item is never null, a row handed a
	/// heading knows it has one, and the cast back to ComponentBay either works
	/// or returns null in exactly one place.
	///
	/// The text comes from the bay type's own FriendlyName - all 14 of them
	/// have one - uppercased, so no label lives in code and a bay type can be
	/// relabelled from its own prefab. Not pluralised, because the names are
	/// singular already and some of them do not pluralise sensibly:
	/// PowerGeneratorBayType is "Power", and "Powers" would be wrong.
	/// </remarks>
	public class ComponentBayGroupHeader
	{
		public ComponentBayGroupHeader(string text)
		{
			Text = text;
		}

		/// <summary>
		/// What the heading says.
		/// </summary>
		public string Text { get; private set; }
	}
}