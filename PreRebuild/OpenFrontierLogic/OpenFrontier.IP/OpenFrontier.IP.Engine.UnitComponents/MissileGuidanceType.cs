namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// How a missile finds its target, ordered worst to best.
	/// </summary>
	/// <remarks>
	/// The order is the order, not an accident of numbering: Optical is the
	/// worst and ARH the best, so a higher value always wins when two missiles
	/// are locked on at once. That is what makes the warning more than a
	/// countdown - it tells you what is coming.
	///
	/// Optical is deliberately 0 and therefore the default, because a
	/// ProjectileClass that predates this field reads as 0 rather than as the
	/// best thing in the game. Every missile in the base game is unguided or
	/// untyped, so nothing changes until a missile says otherwise.
	///
	/// Every type has its own warning sounds, optical included - they are just
	/// the quietest, and they sit at the bottom of the order. When several
	/// missiles are locked on at once, the highest type is the one that sounds,
	/// so an ARH in the mix drowns out an optical one because it matters more.
	/// </remarks>
	public enum MissileGuidanceType
	{
		/// <summary>Unguided or optically sighted. No warning tone.</summary>
		Optical = 0,

		/// <summary>Infrared. Sees a target by its own heat.</summary>
		IR = 1,

		/// <summary>Semi-active radar. Needs the target to light it up.</summary>
		SARH = 2,

		/// <summary>Active radar. Finds a target on its own.</summary>
		ARH = 3
	}
}