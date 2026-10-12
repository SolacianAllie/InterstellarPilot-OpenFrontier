namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// How big a slot is, and therefore how big a thing may go in it.
	/// </summary>
	/// <remarks>
	/// Small, medium, large, huge - four sizes, as hardpoints have always had.
	///
	/// Starts at zero deliberately, and that is not a style choice. A new field
	/// on a MonoBehaviour is read as zero from every prefab that predates it,
	/// so making Small the zero means all 141 existing components and every
	/// ship that carries them are already small with nothing to migrate and
	/// nothing to remember to come back and fix. Starting this at one instead
	/// would leave every existing asset on an undefined value and quietly make
	/// every fit fail.
	///
	/// The number after each name is what appears in the editor, so a Small
	/// bay really does read as 1 rather than as whatever the enum happens to
	/// be worth internally.
	/// </remarks>
	public enum HardpointSize
	{
		Small = 0,
		Medium = 1,
		Large = 2,
		Huge = 3
	}
}