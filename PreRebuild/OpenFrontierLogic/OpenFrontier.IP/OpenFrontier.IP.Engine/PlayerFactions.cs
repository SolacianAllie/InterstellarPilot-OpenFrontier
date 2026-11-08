namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Decides whether a faction belongs to a person at a keyboard.
	/// </summary>
	/// <remarks>
	/// Separate from <see cref="IReservedIdPolicy"/> even though the answer is
	/// the same question, because they answer different things. That one is
	/// the engine asking "may I hand this number out"; this one is the game
	/// asking "whose is this". An allocator that is told a band is reserved
	/// has learned nothing about whether a faction in it is a player's - only
	/// that the engine will not choose one.
	///
	/// Null means there are no other players, which is the singleplayer case
	/// and the default, so an ordinary game pays one null check per faction
	/// drawn.
	/// </remarks>
	public interface IPlayerFactionTest
	{
		/// <summary>Whether this faction id belongs to a human player.</summary>
		bool IsPlayerFaction(int factionId);
	}

	/// <summary>
	/// The test the game asks about factions, filled in from outside.
	/// </summary>
	/// <remarks>
	/// Owned by the engine and answered from the other side, for the same
	/// reason everything else at this boundary is: the engine cannot reach
	/// across to ask, and the thing that knows which numbers mean a person is
	/// on the other side of the asmdef.
	///
	/// Static because it is a property of the running process rather than of
	/// any one world. The answer does not change between worlds, and the
	/// alternative - asking each world - would make a screen draw depend on
	/// which world happened to be loaded.
	/// </remarks>
	public static class PlayerFactions
	{
		/// <summary>
		/// Set once at startup. Left null in a game that never uses it.
		/// </summary>
		public static IPlayerFactionTest Test { get; set; }

		/// <summary>
		/// Whether this faction belongs to somebody playing.
		/// </summary>
		/// <remarks>
		/// Failing open, as everywhere else at this boundary. An exception
		/// from outside the engine is answered "no", which draws the faction
		/// the way an AI's is drawn - the ordinary appearance, in the ordinary
		/// game, and a cosmetic difference rather than a wrong one. Failing
		/// closed would paint every faction in the world as a player's, which
		/// is a thing a player would report as the game being broken.
		/// </remarks>
		public static bool IsPlayerFaction(int factionId)
		{
			IPlayerFactionTest test = Test;

			if (test == null)
			{
				return false;
			}

			try
			{
				return test.IsPlayerFaction(factionId);
			}
			catch (System.Exception)
			{
				return false;
			}
		}
	}
}
