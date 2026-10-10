using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Multiplayer.UI;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells the engine which sectors somebody is watching, so it simulates
	/// those at full rate.
	///
	/// The engine ticks the sector on screen every frame and round-robins the
	/// rest at about one unit every three seconds, on the reasonable-enough
	/// assumption that a sector nobody is looking at has nothing visible
	/// happening in it. In a session that assumption is false: a player two
	/// jumps away is looking at theirs.
	///
	/// So the host, which is the only machine simulating anything, says which
	/// sectors have someone in them. It is the only party that can: only it
	/// knows where every player's ship is, and only it runs a world.
	///
	/// Claims are held per player and released when they leave or move, so a
	/// sector nobody is in goes back to ticking slowly - the performance win
	/// the engine was built for is only wasted while it is actually being
	/// spent.
	/// </summary>
	public sealed class WatchedSectorWatcher
	{
		private static WatchedSectorWatcher instance;

		/// <summary>The one watcher, since there is one host per session.</summary>
		public static WatchedSectorWatcher Instance =>
			instance ?? (instance = new WatchedSectorWatcher());

		/// <summary>What each player currently has claimed.</summary>
		private readonly Dictionary<string, Sector> claims = new Dictionary<string, Sector>();

		private readonly List<string> stale = new List<string>();

		/// <summary>Sectors being simulated at full rate, for the host's log.</summary>
		public int WatchedCount => claims.Count;

		/// <summary>
		/// Works out who is where and updates the engine to match.
		/// </summary>
		/// <remarks>
		/// Recomputed from the session every call rather than driven by
		/// events, because the question is not "did something happen" but
		/// "who is in what sector right now", and the answer to that is only
		/// ever obtained by asking. A player who crosses a wormhole changes
		/// sector without raising anything, and an event-driven version would
		/// leave the old sector ticking at full rate for the rest of the
		/// session.
		///
		/// Cheap: one dictionary lookup per player, and only the sectors that
		/// actually changed are told to the engine.
		/// </remarks>
		public void Refresh()
		{
			HostSession session = MultiplayerHostService.Session;
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			if (session == null)
			{
				ReleaseEverything(engine);
				return;
			}

			for (int i = 0; i < session.Players.Count; i++)
			{
				RemotePlayer player = session.Players[i];

				if (player == null || !player.Admitted)
				{
					continue;
				}

				string token = player.Identity ?? string.Empty;

				Sector where = SectorOf(engine, session, token);

				if (claims.TryGetValue(token, out Sector was) && ReferenceEquals(was, where))
				{
					continue;
				}

				if (was != null)
				{
					engine.SetSectorWatched(was, false);
				}

				if (where != null)
				{
					engine.SetSectorWatched(where, true);
				}

				claims[token] = where;
			}

			ForgetAnyoneWhoLeft(engine, session);
		}

		private void ForgetAnyoneWhoLeft(EngineASX engine, HostSession session)
		{
			stale.Clear();

			foreach (KeyValuePair<string, Sector> entry in claims)
			{
				bool known = false;

				for (int i = 0; i < session.Players.Count; i++)
				{
					RemotePlayer player = session.Players[i];

					if (player != null
						&& (player.Identity ?? string.Empty) == entry.Key)
					{
						known = true;
						break;
					}
				}

				if (!known)
				{
					stale.Add(entry.Key);
				}
			}

			for (int i = 0; i < stale.Count; i++)
			{
				Release(engine, stale[i]);
			}
		}

		private void Release(EngineASX engine, string token)
		{
			if (claims.TryGetValue(token, out Sector was) && was != null)
			{
				engine.SetSectorWatched(was, false);
			}

			claims.Remove(token);
		}

		private void ReleaseEverything(EngineASX engine)
		{
			foreach (KeyValuePair<string, Sector> entry in claims)
			{
				if (entry.Value != null)
				{
					engine.SetSectorWatched(entry.Value, false);
				}
			}

			claims.Clear();
		}

		/// <summary>
		/// Where this player's ship is, or null if they have none yet.
		/// </summary>
		/// <remarks>
		/// Null rather than the host's active sector, unlike the broadcaster's
		/// fallback. A player who has not been placed yet is in nowhere in
		/// particular, and claiming the host's sector on their behalf would
		/// hold a sector at full rate for someone who is not in it. The
		/// broadcaster has to guess because it has to send something; this one
		/// does not, because doing nothing is correct.
		/// </remarks>
		private static Sector SectorOf(EngineASX engine, HostSession session, string token)
		{
			foreach (int unitId in session.Ownership.UnitsOwnedBy(token))
			{
				Unit ship = engine.GetUnitByid(unitId);

				if (ship != null && ship.Sector != null)
				{
					return ship.Sector;
				}
			}

			return null;
		}

		/// <summary>
		/// Releases every claim. For when a session ends and the world it was
		/// simulating goes back to being one player's alone.
		/// </summary>
		public void Forget()
		{
			ReleaseEverything(EngineASX.Instance);
		}
	}
}