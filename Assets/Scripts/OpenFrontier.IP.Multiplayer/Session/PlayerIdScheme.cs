using System;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Which ids belong to things a player made, and which do not.
	/// </summary>
	/// <remarks>
	/// The engine allocates every id from one monotonic counter starting at
	/// 100000 - factions, ships, stations, fleets, all of it - and a counter
	/// knows nothing about who made the thing it is numbering.
	///
	/// So anything made for a joining player is numbered from a band far above
	/// that counter instead. Three things follow, and the first two are why it
	/// is worth doing:
	///
	/// "Was this made by a player?" becomes a property of the number. No
	/// lookup, no flag to keep in step, and it survives a save and a reload
	/// for free because ids are stored verbatim.
	///
	/// Player-made things and engine-made things cannot collide, however many
	/// of either a world happens to contain, because they are drawn from
	/// different places entirely. Faction ids can therefore be derived
	/// rather than allocated - see <see cref="FactionIdFor"/> - which is what
	/// makes a joiner able to work out its own id without being told.
	///
	/// And a stale id from another world is harmless rather than
	/// catastrophic: it still names a player's faction, just not one that
	/// exists here, which is a much easier thing to notice and refuse.
	///
	/// Bands, not prefixes: these are ints. The spacing is generous because
	/// the counter is monotonic and nothing in the engine would notice a gap,
	/// but the real requirement is only that the two ranges do not overlap.
	/// </remarks>
	public static class PlayerIdScheme
	{
		/// <summary>First id reserved for player-made factions.</summary>
		/// <remarks>
		/// Far above the engine's own counter, which starts at 100000 and is
		/// shared with everything the world generator makes.
		/// </remarks>
		public const int FactionIdBase = 900000000;

		/// <summary>How many faction ids are reserved.</summary>
		public const int FactionIdBand = 1000000;

		/// <summary>First id reserved for player-made units.</summary>
		/// <remarks>
		/// A band of its own rather than shared with factions, so a ship is
		/// never mistaken for a faction by anything that reads an id without
		/// knowing what kind of thing it is naming.
		/// </remarks>
		public const int UnitIdBase = 800000000;

		/// <summary>How many unit ids are reserved.</summary>
		public const int UnitIdBand = 1000000;

		/// <summary>
		/// The faction id a given player's faction always has.
		/// </summary>
		/// <remarks>
		/// Derived from the token, so it is stable across restarts and across
		/// every world that player plays in. That is what lets it be derived
		/// rather than allocated, and why two players cannot collide however
		/// many factions either of their worlds happens to have.
		/// </remarks>
		public static int FactionIdFor(string playerToken)
		{
			if (string.IsNullOrEmpty(playerToken))
			{
				return -1;
			}

			return FactionIdBase + (int)(StableHash(playerToken) % (uint)FactionIdBand);
		}

		/// <summary>
		/// The next free player unit id, allocated on the host.
		/// </summary>
		/// <remarks>
		/// Sequential within the band rather than derived, because one player
		/// owns many ships and every one of them needs its own id. Only the
		/// host allocates, so the two copies agree without any negotiation.
		/// </remarks>
		public static int NextUnitId()
		{
			for (int i = UnitIdBase; i < UnitIdBase + UnitIdBand; i++)
			{
				if (!Reserved.Contains(i))
				{
					Reserved.Add(i);
					return i;
				}
			}

			// A million player units in one world is not a thing that will
			// happen, and running out should be obvious rather than silent.
			return -1;
		}

		/// <summary>Whether an id was reserved for something a player made.</summary>
		public static bool IsPlayerMade(int id)
		{
			return (id >= FactionIdBase && id < FactionIdBase + FactionIdBand)
				|| (id >= UnitIdBase && id < UnitIdBase + UnitIdBand);
		}

		/// <summary>Ids handed out this session, so nothing is issued twice.</summary>
		private static readonly System.Collections.Generic.HashSet<int> Reserved =
			new System.Collections.Generic.HashSet<int>();

		/// <summary>
		/// FNV-1a. A stable hash is the whole requirement; anything better is
		/// a thing to change later, in one place.
		/// </summary>
		private static uint StableHash(string value)
		{
			uint hash = 2166136261;

			for (int i = 0; i < value.Length; i++)
			{
				hash = (hash ^ value[i]) * 16777619;
			}

			return hash;
		}
	}
}