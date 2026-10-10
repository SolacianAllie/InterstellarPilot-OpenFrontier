using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Describes a ship in one line of text, for the log.
	/// </summary>
	/// <remarks>
	/// This exists because a ship that behaves wrongly and a ship that is
	/// missing its innards look identical from the outside, and every way of
	/// telling them apart by eye involves flying one. The useful trick is to
	/// print a known-good ship on the same line as a suspect one, in the same
	/// run, so the two can be read against each other rather than against an
	/// idea of what a ship ought to look like.
	///
	/// Everything here is read defensively. A diagnostic that throws while
	/// describing a broken ship is worse than no diagnostic, because it
	/// reports a failure that has nothing to do with the one being chased.
	/// </remarks>
	public static class UnitDiagnostics
	{
		/// <summary>One line describing a ship, or why it could not be read.</summary>
		public static string Describe(Unit unit, string role)
		{
			if (unit == null)
			{
				return role + ": <null>";
			}

			// A ship is a prefab with its parts already parented under it.
			// The count is the number the whole rest of the engine hangs off,
			// so a mismatch here is not a rounding error, it is the ship.
			int parts = 0;
			int holderCount = 0;

			try
			{
				parts = unit.transform.childCount;
				holderCount = unit.GetComponentsInChildren<UnitComponentHolder>(true).Length;
			}
			catch (System.Exception e)
			{
				return role + ": id=" + unit.UniqueId + " - could not read parts ("
					+ e.GetType().Name + ": " + e.Message + ")";
			}

			string className = "<none>";
			string activeName = "<none>";

			try
			{
				className = unit.UnitClass == null
					? "<none>"
					: unit.UnitClass.GetClassAndSeriesName();
				activeName = unit.ActiveUnit == null
					? "<none>"
					: unit.ActiveUnit.name;
			}
			catch (System.Exception)
			{
				// Left as the placeholders. A class or an active unit that
				// cannot be read is itself the finding.
			}

			bool hasCargo;
			float mass;

			try
			{
				hasCargo = unit.CargoComponent != null;
				mass = unit.GetComponent<Rigidbody>() == null
					? -1f
					: unit.GetComponent<Rigidbody>().mass;
			}
			catch (System.Exception)
			{
				hasCargo = false;
				mass = -1f;
			}

			return role + ": id=" + unit.UniqueId
				+ " class='" + className + "'"
				+ " active='" + activeName + "'"
				+ " parts=" + parts
				+ " holders=" + holderCount
				+ " cargo=" + (hasCargo ? "yes" : "no")
				+ " mass=" + mass
				+ " seed=" + unit.Seed
				+ " sector=" + (unit.Sector == null ? "<none>" : unit.Sector.Name)
				+ " active=" + unit.gameObject.activeInHierarchy
				+ " enabled=" + unit.enabled;
		}
	}
}