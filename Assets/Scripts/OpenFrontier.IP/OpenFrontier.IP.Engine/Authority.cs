using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Answers "may this copy of the game decide that?", for the parts of the
	/// engine that decide things.
	/// </summary>
	/// <remarks>
	/// Implemented on the multiplayer side and installed by it. The engine
	/// cannot reach across to ask - that dependency does not exist and must
	/// not - and the thing that knows whether anybody is in a session is
	/// over there.
	///
	/// A copy that has nobody in a session with it owns everything, and says
	/// so, so that a singleplayer game is not a special case that has to be
	/// remembered at every call site.
	/// </remarks>
	public interface IAuthorityProvider
	{
		/// <summary>
		/// Whether this copy is following somebody else's game.
		/// </summary>
		/// <remarks>
		/// The first question, and the one that decides whether any of the
		/// others is worth asking. A game with nobody in it is authoritative
		/// for everything no matter what the provider is, so consult points
		/// check this before they check anything per-unit - which means an
		/// ordinary game never reaches the expensive part.
		/// </remarks>
		bool IsThisCopyAClient { get; }

		/// <summary>
		/// Whether this copy simulates the given unit.
		/// </summary>
		/// <remarks>
		/// True for exactly one unit on a client: the ship the player is
		/// flying by hand. Everything else - including other ships of their
		/// own faction, including their own stations, including their own ship
		/// while it is on autopilot - is the host's.
		/// </remarks>
		bool OwnsUnit(Unit unit);

		/// <summary>
		/// Whether this copy simulates the given faction.
		/// </summary>
		/// <remarks>
		/// False for every faction on a client, including the player's own.
		/// A client does not run an economy; it is told what the economy did.
		/// </remarks>
		bool OwnsFaction(Faction faction);

		/// <summary>
		/// Whether this copy simulates world-level things: missions, spawning,
		/// daily income, the opinions between factions.
		/// </summary>
		bool OwnsWorld();
	}

	/// <summary>
	/// The question every part of the engine asks before it decides anything.
	/// </summary>
	/// <remarks>
	/// Null provider means authoritative for everything. That is the
	/// singleplayer case, and it is structural rather than conditional: there
	/// is no path by which a missing provider can produce "no", so a game that
	/// has never heard of multiplayer cannot be held still by accident.
	///
	/// The measure of this working is that a player who never opens the
	/// multiplayer menu cannot tell any of it happened. Every consult point is
	/// written so the answer for them is yes before anything else is asked.
	/// </remarks>
	public static class Authority
	{
		/// <summary>
		/// Installed by the multiplayer layer. Left null in a game that has
		/// nothing to do with it.
		/// </summary>
		public static IAuthorityProvider Provider { get; set; }

		/// <summary>
		/// Whether this copy is following somebody else's game.
		/// </summary>
		public static bool IsThisCopyAClient
		{
			get
			{
				IAuthorityProvider provider = Provider;

				return provider != null && provider.IsThisCopyAClient;
			}
		}

		/// <summary>
		/// Whether this copy simulates the given unit.
		/// </summary>
		/// <remarks>
		/// Any failure at all - no provider, a provider that throws, a unit
		/// that has already gone - answers yes.
		///
		/// That direction is not a convenience. Answering no means this copy
		/// stops simulating, and a world that has stopped simulating does not
		/// look broken so much as it looks paused: ships hold still, nothing
		/// errors, and nothing anywhere says why. Answering yes means two
		/// copies may briefly disagree, which is visible, diagnosable, and
		/// exactly what the rest of this exists to fix.
		/// </remarks>
		public static bool OwnsUnit(Unit unit)
		{
			IAuthorityProvider provider = Provider;

			// IsThisCopyAClient first, and not as an optimisation the caller
			// is trusted to remember.
			//
			// It was left to the caller, and the one caller that did not - the
			// report that says which mode this copy is in - was told by a host
			// that it would decline every single ship in the galaxy. The
			// simulation itself was fine, because the consult point does check
			// first. Everything else that asked was one forgotten check away
			// from the same wrong answer.
			if (!IsThisCopyAClient || unit == null)
			{
				return true;
			}

			try
			{
				return provider.OwnsUnit(unit);
			}
			catch (System.Exception)
			{
				return true;
			}
		}

		/// <summary>Whether this copy simulates the given faction.</summary>
		public static bool OwnsFaction(Faction faction)
		{
			IAuthorityProvider provider = Provider;

			if (!IsThisCopyAClient || faction == null)
			{
				return true;
			}

			try
			{
				return provider.OwnsFaction(faction);
			}
			catch (System.Exception)
			{
				return true;
			}
		}

		/// <summary>
		/// What this copy has declined to simulate because the host owns it.
		/// </summary>
		/// <remarks>
		/// Counted rather than logged. The point is to have an answer that does
		/// not depend on watching ships, because "does anything on this client
		/// move that the host did not move" is a question the eye answers
		/// slowly and wrongly - a ship that should have been still is the one
		/// thing a player is looking straight at, and a ship that should not
		/// have been still is the one they are not.
		///
		/// Zero in a singleplayer frame is the test that nothing has leaked.
		/// Non-zero on a client is the test that something has.
		/// </remarks>
		public static int DeclinedCount { get; private set; }

		/// <summary>Declined since the last <see cref="Report"/>.</summary>
		public static int DeclinedSinceReport { get; private set; }

		/// <summary>
		/// Called by a consult point that has decided not to simulate.
		/// </summary>
		public static void Declined()
		{
			DeclinedCount++;
			DeclinedSinceReport++;
		}

		/// <summary>
		/// Returns what has been declined and starts again.
		/// </summary>
		/// <remarks>
		/// Taken rather than read, so that a value printed twice does not
		/// print the same thing twice - which is how an instrument ends up
		/// showing a stale reading that looks like a live one.
		/// </remarks>
		public static int TakeDeclinedSinceReport()
		{
			int taken = DeclinedSinceReport;
			DeclinedSinceReport = 0;
			return taken;
		}

		/// <summary>Whether this copy simulates world-level things.</summary>
		public static bool OwnsWorld()
		{
			IAuthorityProvider provider = Provider;

			if (!IsThisCopyAClient)
			{
				return true;
			}

			try
			{
				return provider.OwnsWorld();
			}
			catch (System.Exception)
			{
				return true;
			}
		}
	}
}
