namespace OpenFrontier.IP.Engine.UnitComponents
{
	/// <summary>
	/// How capable an internal module is, in eight steps.
	/// </summary>
	/// <remarks>
	/// The class ladder for things that go inside a ship rather than on it:
	/// capacitors, generators, shields, engines, drives. Class 1 is the
	/// smallest and class 8 the largest.
	///
	/// Starts at zero so every existing component reads as class 1 without being
	/// touched. That mattered most while Size still existed as a second ceiling
	/// beside this one; now class is the only size axis there is.
	///
	/// Not enforced yet. Nothing refuses a class 8 module because it went into
	/// a class 2 slot, and that is deliberate: the numbers that would decide
	/// it do not exist yet. Which class a shield of 250 units belongs in is a
	/// balance question, and guessing at 141 of them would be inventing one
	/// quietly rather than asking. It is here so the values can be filled in
	/// at a deliberate pace, and read back out.
	/// </remarks>
	public enum ModuleClass
	{
		Class1 = 0,
		Class2 = 1,
		Class3 = 2,
		Class4 = 3,
		Class5 = 4,
		Class6 = 5,
		Class7 = 6,
		Class8 = 7
	}
}