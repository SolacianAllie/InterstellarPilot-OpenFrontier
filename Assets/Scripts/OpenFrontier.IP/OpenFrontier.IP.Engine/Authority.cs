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

		/// <summary>Actions already refused, so each is only said once.</summary>
		private static readonly System.Collections.Generic.HashSet<string> RefusalsAlreadySaid =
			new System.Collections.Generic.HashSet<string>();

		/// <summary>
		/// Refuses a mutation this copy is not allowed to make, by name.
		/// </summary>
		/// <remarks>
		/// For the consult points that are about changing the world rather
		/// than about who simulates it. <see cref="Declined()"/> counts those
		/// without saying which, which is right for a per-frame simulation
		/// gate and useless for a button: "3 declines" does not tell a player
		/// why their ship just did not move, and it does not tell us which of
		/// forty unwired actions somebody pressed.
		///
		/// Returns true when it refused, so a caller reads
		/// <c>if (Authority.Refused(...)) { return; }</c> and is otherwise
		/// untouched. False in a game with nobody else in it, and false on the
		/// host - which is the whole point of putting it here rather than at
		/// each call site: singleplayer keeps working by construction instead
		/// of by forty separate pieces of care.
		///
		/// Said once per action rather than every time, because several of
		/// these sit under Update and a refusal per frame per faction would
		/// bury everything else in the log - which is how the log got to
		/// saying "ran Patrol" directly underneath "Patrol was refused" in the
		/// first place. The count still rises, so the leak is still visible in
		/// <see cref="AuthorityModeReport"/>, and it says which one it was.
		///
		/// The alternative to this is silence: the click mutates the player's
		/// own copy, the host never hears, and the two drift apart with
		/// nothing on screen or in either log to say so. A player who is told
		/// no can wait for it to work. A player who is quietly wrong cannot.
		/// </remarks>
		public static bool Refused(string whatThePlayerWasTryingToDo, string whatItWouldHaveChanged)
		{
			if (!IsThisCopyAClient)
			{
				return false;
			}

			Declined();

			if (RefusalsAlreadySaid.Add(whatThePlayerWasTryingToDo))
			{
				UnityEngine.Debug.LogWarning("Multiplayer: refused - "
					+ whatThePlayerWasTryingToDo
					+ ". Nothing changed. It would have " + whatItWouldHaveChanged
					+ ", which only the host may decide. This action is not"
					+ " relayed to the host yet - see Docs/multiplayer/coverage-plan.md"
					+ " for whether it is planned.");
			}

			return true;
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

		/// <summary>
		/// Derived values recomputed on a copy that is not simulating.
		/// </summary>
		/// <remarks>
		/// Not declines. A decline is a simulation this copy declined to run,
		/// and the count is how a leak is spotted. This is the opposite: a
		/// value a copy computed for itself because both copies hold the same
		/// inputs and must reach the same answer.
		///
		/// Counted because "a remote ship sees nothing" and "a remote ship's
		/// sensor range is right" look identical from the outside - a dark
		/// screen either way - and only one of them is a bug. A number that
		/// says how many ships had their range recomputed on a copy that is
		/// not flying them is what tells the two apart.
		/// </remarks>
		public static int DerivedRefreshesSinceReport { get; private set; }

		/// <summary>Notes a derived value computed off the simulation path.</summary>
		public static void NoteDerivedValueRefreshed()
		{
			DerivedRefreshesSinceReport++;
		}

		/// <summary>Returns what has been recomputed and starts again.</summary>
		public static int TakeDerivedRefreshesSinceReport()
		{
			int taken = DerivedRefreshesSinceReport;
			DerivedRefreshesSinceReport = 0;
			return taken;
		}

		/// <summary>
		/// Asks the host to dock a ship. Returns true if it was asked.
		/// </summary>
		/// <remarks>
		/// A seam, in the same shape as <see cref="Provider"/> and for the same
		/// reason: the game assembly cannot reference the multiplayer one, and
		/// that dependency must not be created. So the engine asks here, the
		/// multiplayer side answers, and nothing in between has to know which
		/// of them exists.
		///
		/// False when nobody is listening, which is the whole singleplayer
		/// game and every host. A caller treats it as "carry on and dock it
		/// yourself", so the ordinary path is untouched and the relay is
		/// additive rather than a branch everybody has to remember.
		///
		/// This is the general shape every remaining player action will take,
		/// and the reason the command channel is one envelope rather than
		/// forty message types: the engine needs one door, not forty.
		/// </remarks>
		public static System.Func<int, int, bool> DockRequest { get; set; }

		/// <summary>
		/// Asks the host to undock a ship. Returns true if it was asked.
		/// </summary>
		/// <remarks>
		/// Only the host knows what is in the hangar, so only the host can
		/// take a ship out of it - which is the mirror of DockRequest and
		/// exists because a dock with no way back out is not a feature.
		/// </remarks>
		public static System.Func<int, bool> UndockRequest { get; set; }

		/// <summary>
		/// Asks the host to abandon a ship. Returns true if it was asked.
		/// </summary>
		/// <remarks>
		/// Abandoning hands a ship over: it stops belonging to anybody and
		/// becomes free for either side to take. That is a change to the world
		/// and only the host may make one, so a client asks.
		///
		/// The mirror of the claim button and never true at the same time as
		/// it - a ship can be claimed because it belongs to nobody, and
		/// abandoned because it belongs to you.
		/// </remarks>
		public static System.Func<int, bool> AbandonShipRequest { get; set; }

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
