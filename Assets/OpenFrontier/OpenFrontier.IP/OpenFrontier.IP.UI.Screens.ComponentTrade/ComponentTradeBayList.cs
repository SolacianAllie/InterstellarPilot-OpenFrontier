using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	/// <summary>
	/// The list of bays, with headings mixed in.
	/// </summary>
	/// <remarks>
	/// Typed to object rather than ComponentBay because the list holds two
	/// kinds of thing: bays, which can be picked, and
	/// <see cref="ComponentBayGroupHeader"/>, which cannot. This is a
	/// specialization of ScrollList&lt;T&gt;, not a change to ScrollList&lt;T&gt;
	/// itself, so every other list in the game is untouched.
	///
	/// The alternative - keeping ScrollList&lt;ComponentBay&gt; and passing null
	/// for a heading - was rejected because it turns every read of Item in
	/// every row into a null check and makes "select a bay that does not exist"
	/// a thing that can happen. An item here is never null.
	/// </remarks>
	public class ComponentTradeBayList : ScrollList<object>
	{
	}
}