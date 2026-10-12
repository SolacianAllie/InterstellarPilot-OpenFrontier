namespace OpenFrontier.IP.Engine
{
	/// <summary>
	/// Decides which ids the engine must never hand out itself.
	/// </summary>
	/// <remarks>
	/// Multiplayer gives every faction and ship a player made one an id in a
	/// reserved band, so that "was this made by a player" is answerable from
	/// the number alone - in a save, in a log, in a debugger, without a lookup
	/// table that two copies of a world would each have to agree about.
	///
	/// The engine allocates ids from counters that start at 100000 and walk
	/// upward, so the bands near 800 and 900 million are nowhere near being
	/// reached by accident. That was the assumption, and it was wrong in a way
	/// that took a long time to notice: moving a faction into the band used to
	/// drag the counter past it, after which every faction the world generator
	/// went on to create was numbered inside the player band. IsPlayerMade
	/// answered true for ordinary AI, and the corrupted counter was saved and
	/// reloaded, so it survived a restart too.
	///
	/// So the engine asks. An allocator that walks a counter cannot be trusted
	/// to stay far away from a range that only means something to code outside
	/// it, and a bug of that kind stays invisible until something reads the
	/// band and gets the wrong answer.
	///
	/// Null means nothing is reserved, which is the singleplayer case and the
	/// default - so an ordinary game pays one null check per allocation.
	/// </remarks>
	public interface IReservedIdPolicy
	{
		/// <summary>Whether this id belongs to somebody and must not be reused.</summary>
		bool IsReserved(int id);
	}

	/// <summary>
	/// The policy the engine consults when it hands out ids.
	/// </summary>
	/// <remarks>
	/// Static because it is a property of the running process rather than of
	/// any one world: the same answer applies to every world the player loads,
	/// and it outlives all of them.
	///
	/// Owned by the engine and filled in from outside, for the same reason
	/// everything else at this boundary is: the engine cannot reach across to
	/// ask, and the thing that knows the bands is on the other side.
	/// </remarks>
	public static class ReservedIds
	{
		/// <summary>
		/// Set once at startup. Left null in a game that never uses it.
		/// </summary>
		public static IReservedIdPolicy Policy { get; set; }

		/// <summary>
		/// Whether this id must not be handed out.
		/// </summary>
		/// <remarks>
		/// Written as a try, because this is code from outside the engine being
		/// called from inside an id allocation. Failing open here means an id
		/// might be reused in a band something else believes is meaningful,
		/// which is survivable and diagnosable. Failing closed would stop the
		/// world generating factions at all, and would do it silently.
		/// </remarks>
		public static bool IsReserved(int id)
		{
			IReservedIdPolicy policy = Policy;

			if (policy == null)
			{
				return false;
			}

			try
			{
				return policy.IsReserved(id);
			}
			catch (System.Exception)
			{
				return false;
			}
		}
	}
}
