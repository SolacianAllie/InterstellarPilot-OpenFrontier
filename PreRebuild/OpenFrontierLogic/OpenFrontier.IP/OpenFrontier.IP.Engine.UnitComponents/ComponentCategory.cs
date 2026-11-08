namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// Which family of hardware this is - a slot, or a part that fits one -
	/// and so what may be fitted to it and what happens when that is taken
	/// back out.
	/// </summary>
	/// <remarks>
	/// The four families are meant to be answers to different questions, which
	/// is why they are separate from BayType. BayType says what the slot IS -
	/// a shield bay, an engine bay. This says what the slot is FOR and what the
	/// rules around it are:
	///
	/// Hardpoint - a weapon. Gated by size and by arc, and the only family
	/// where firing arc and range mean anything.
	///
	/// CoreInternal - the ship works without this, but not comfortably, and it
	/// is not allowed to simply not be there. Intended to be class-pinned: a
	/// core set to class 5 only accepts class 5, and uninstalling one buys the
	/// smallest module of that class rather than leaving a hole.
	///
	/// OptionalInternal - fit anything the bay is big enough for, current size
	/// or smaller. May be sold or left empty.
	///
	/// UtilityMount - no class at all, only a quality rating.
	///
	/// Legacy is zero on purpose, and it is not a placeholder. A new field on a
	/// MonoBehaviour reads as zero from every prefab that predates it, so zero
	/// is what all 1,237 existing bays would get no matter what this enum was
	/// ordered. Rather than pretend that is one of the four families - which
	/// would silently put every engine and shield in the game into whatever
	/// happened to be first - it gets its own name, and the trade screen opens
	/// on it, so old content keeps working and the number of bays still to
	/// classify is visible rather than hidden. When it reaches zero this can
	/// go, along with the option in the dropdown.
	/// </remarks>
	public enum ComponentCategory
	{
		/// <summary>
		/// Not classified yet. What every bay in the game is today.
		/// </summary>
		Legacy = 0,

		/// <summary>
		/// A weapon slot.
		/// </summary>
		Hardpoint = 1,

		/// <summary>
		/// Cannot be left empty. Uninstalling one replaces it with the
		/// smallest module of its class.
		/// </summary>
		CoreInternal = 2,

		/// <summary>
		/// Takes anything the bay is big enough for, current size or smaller.
		/// </summary>
		OptionalInternal = 3,

		/// <summary>
		/// No class. Quality rating only.
		/// </summary>
		UtilityMount = 4
	}
}