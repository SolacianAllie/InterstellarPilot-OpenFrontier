using System;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Which faction id a given player's faction has.
	/// </summary>
	/// <remarks>
	/// Its own file and no engine types, because the property that matters -
	/// that it does not vary - is worth being able to test directly.
	/// </remarks>
	public static class PlayerFactionIds
	{
		/// <summary>
		/// Where multiplayer player factions live in the id space.
		/// </summary>
		/// <remarks>
		/// The engine hands out faction ids from one monotonic counter shared
		/// with everything else, starting at 100000. That makes a joiner's
		/// id depend entirely on how many factions that particular world
		/// happens to have - which is why an id carried across worlds named a
		/// different faction entirely, and why fixing that meant clearing a
		/// cache instead of not caring.
		///
		/// Reserving a band well above anything the counter reaches, and
		/// deriving the id from the player's token rather than from a
		/// counter, makes the id mean the same thing in every world. A stale
		/// one is then harmless rather than catastrophic, and two players can
		/// never collide however many factions either of their worlds has.
		/// </remarks>
		public const int PlayerFactionIdBase = 900000000;

		/// <summary>How many ids the band holds.</summary>
		public const int PlayerFactionIdBand = 1000000;

		/// <summary>
		/// The id a given player's faction always has.
		/// </summary>
		/// <remarks>
		/// From the token, so it is stable across restarts and across every
		/// world they play in - which also means the joiner can work out its
		/// own id without being told, and a collision cannot be resolved by
		/// asking who got there first.
		/// </remarks>
		public static int For(string playerToken)
		{
			if (string.IsNullOrEmpty(playerToken))
			{
				return -1;
			}

			// FNV-1a, because a stable hash is the whole requirement and
			// anything better is a thing to change later in one place.
			uint hash = 2166136261;

			for (int i = 0; i < playerToken.Length; i++)
			{
				hash = (hash ^ playerToken[i]) * 16777619;
			}

			return PlayerFactionIdBase + (int)(hash % PlayerFactionIdBand);
		}

	}
}
