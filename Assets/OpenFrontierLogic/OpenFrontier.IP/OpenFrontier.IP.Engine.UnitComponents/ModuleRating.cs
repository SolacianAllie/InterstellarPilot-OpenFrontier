namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// How well made an internal module is, from E up to A.
	/// </summary>
	/// <remarks>
	/// A is the best and E the cheapest and worst, as module ratings have
	/// always gone. Read with <see cref="ModuleClass"/> rather than instead of
	/// it: a class 5 A module and a class 5 E module are the same size of thing
	/// at different quality.
	///
	/// Runs E, D, C, B, A rather than alphabetically, so that zero - which is
	/// what every existing component reads as - is the *worst* rating rather
	/// than the best. Every prefab that predates this field would otherwise
	/// come out of it as A-rated, which is a flattering lie sitting in 133
	/// assets that nobody chose. E is the honest default: it claims nothing.
	///
	/// Not enforced yet, for the reason given on <see cref="ModuleClass"/>.
	///
	/// F and G exist because the Elite weapon tables run past A. They are
	/// appended, never inserted: A is still 4, so the 133 assets that serialize
	/// a Quality keep the rating they were authored with.
	/// </remarks>
	public enum ModuleRating
	{
		E = 0,
		D = 1,
		C = 2,
		B = 3,
		A = 4,
		F = 5,
		G = 6
	}
}