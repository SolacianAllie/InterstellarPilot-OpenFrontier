using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells each client where the ships it can see are.
	///
	/// Runs on the host only. It samples ships and sends each client the
	/// batch, because the host is the only copy of the world that is
	/// simulated rather than told.
	///
	/// Two things decide what goes out, and both of them are the engine's
	/// decisions rather than ours:
	///
	/// **Whose sector.** Each client is sent the ships in the sector its own
	/// ship is in. This is what fixed a bug the obvious approach created. It
	/// used to sample the host's active sector for everybody, so a client two
	/// jumps away was sent nothing at all; the next attempt fixed that by
	/// sending the whole galaxy to everybody, which worked and cost most of
	/// the bandwidth doing it. Asking per recipient is the version that was
	/// wanted the first time - the client that was being starved now gets
	/// their own sector, and everyone else stops paying for the ones they
	/// cannot see.
	///
	/// **Whether it moved.** A ship is only sent when its position or facing
	/// has actually changed since that player was last told. This is what
	/// tiers the traffic, and it tiers it for free: the engine already ticks
	/// ships in unvisited sectors once every
	/// <c>PreferredTimeBetweenUnitUpdates</c> seconds rather than every frame,
	/// so a ship nobody is near does not change and is not sent, while a ship
	/// somebody is flying changes every frame and is sent every frame. No
	/// schedule is invented here. The host's own simulation decides who is
	/// fast.
	///
	/// Ten a second is the ceiling, not the plan. A client that gets a
	/// position and a velocity every 100ms can draw the gap between them
	/// smoothly, so it never has to arrive at 60Hz to look smooth - and it
	/// means a session of four people costs a few kilobytes a second rather
	/// than a few hundred.
	/// </summary>
	public sealed class UnitStateBroadcaster : MonoBehaviour
	{
		/// <summary>How many updates a second clients are told about.</summary>
		/// <remarks>
		/// Unscaled, so a paused game still tells anyone watching where the
		/// ships are - a paused world that cannot be looked at is a paused world
		/// nobody can tell apart from a frozen one.
		/// </remarks>
		public const float UpdatesPerSecond = 10f;

		/// <summary>
		/// How far a ship may drift before it counts as having moved.
		/// </summary>
		/// <remarks>
		/// Not zero. Floating point noise in a transform that is not being
		/// written to is not motion, and a comparison at zero would put every
		/// stationary ship back on the wire forever. A centimetre is well
		/// below anything visible at ship scale and comfortably above the
		/// noise floor.
		/// </remarks>
		private const float MovedEpsilon = 0.01f;

		/// <summary>
		/// How far a ship may turn before it counts as having turned.
		/// </summary>
		/// <remarks>
		/// Half a degree. Generous enough that a ship sitting in a rotating
		/// fleet formation does not count as moving, tight enough that a ship
		/// actually turning is not mistaken for one that is not. Quantities
		/// differ because a rotation is an angle in degrees and a position
		/// is metres, and one epsilon for both would be wrong for whichever
		/// it was not sized for.
		/// </remarks>
		private const float FacingEpsilon = 0.5f;

		/// <summary>What one player was last told about one ship.</summary>
		private struct Remembered
		{
			public Vector3 Position;
			public Quaternion Rotation;
			public bool Known;
		}

		private static readonly Dictionary<string, Dictionary<int, Remembered>> remembered =
			new Dictionary<string, Dictionary<int, Remembered>>();

		private static readonly List<UnitState> batch = new List<UnitState>(UnitStateMessage.MaxUnits * 4);
		private static readonly HashSet<int> seen = new HashSet<int>();

		private float nextUpdateTime;

		/// <summary>The session this broadcaster is currently listening to.</summary>
		private HostSession listening;

		private void Update()
		{
			HostSession session = MultiplayerHostService.Session;

			// Listens for players leaving rather than being told to forget
			// them. HostSession is deliberately free of any reference to the
			// engine or to this class - it is the part of the protocol that
			// can be reasoned about without a game - and reaching into it
			// from here to drop a dictionary entry would break that for the
			// sake of one line. It already raises the event; this listens.
			Listen(session);

			if (session == null || session.Players.Count == 0)
			{
				return;
			}

			if (Time.unscaledTime < nextUpdateTime)
			{
				return;
			}

			nextUpdateTime = Time.unscaledTime + (1f / UpdatesPerSecond);

			// Before sampling, and every update rather than on an event,
			// because the engine's tick rate for a sector is the thing that
			// makes a distant player see anything at all. Saying it after
			// sampling would mean broadcasting a sector that is still being
			// simulated at a third of a second per ship.
			WatchedSectorWatcher.Instance.Refresh();

			Broadcast(session);
		}

		private void Listen(HostSession session)
		{
			if (session == listening)
			{
				return;
			}

			if (listening != null)
			{
				listening.PlayerLeft -= OnPlayerLeft;
			}

			listening = session;

			if (listening != null)
			{
				listening.PlayerLeft += OnPlayerLeft;
			}
		}

		private void OnDestroy()
		{
			if (listening != null)
			{
				listening.PlayerLeft -= OnPlayerLeft;
				listening = null;
			}
		}

		/// <summary>
		/// Drops what was remembered about a player who has gone.
		/// </summary>
		/// <remarks>
		/// Without this the record - one entry per ship in their sector -
		/// would outlive them, so a session people join and leave would grow
		/// a copy of every ship each of them was ever shown.
		/// </remarks>
		private static void OnPlayerLeft(RemotePlayer player, string reason)
		{
			Forget(player == null ? null : player.Identity);
		}

		/// <summary>Players whose sends have already failed once.</summary>
		private static readonly HashSet<string> failedPlayers = new HashSet<string>();

		private static string Short(RemotePlayer player)
		{
			string identity = player.DisplayName ?? player.Identity;
			return string.IsNullOrEmpty(identity) ? "a player" : identity;
		}

		/// <summary>Forgets what was sent to a player who has left.</summary>
		public static void Forget(string token)
		{
			if (!string.IsNullOrEmpty(token))
			{
				remembered.Remove(token);
			}
		}

		private static float nextReportTime;
		private static int lastSampled;
		private static int lastSent;
		private static int lastPlayers;

		/// <summary>
		/// What is going out, every couple of seconds.
		/// </summary>
		/// <remarks>
		/// Reported before the bail-outs below, not after. A sender that is
		/// silent and a sender that is sending nothing look identical from
		/// outside, and that ambiguity is the whole reason this exists - the
		/// first version sat after the early return and so stayed quiet in
		/// precisely the case it was written to explain.
		/// </remarks>
		private static void Report(EngineASX engine)
		{
			if (Time.unscaledTime < nextReportTime)
			{
				return;
			}

			nextReportTime = Time.unscaledTime + 2f;

			if (engine == null)
			{
				Debug.Log("Multiplayer: broadcasting nothing - no engine yet");
				return;
			}

			if (engine.World == null || !engine.World.HasInitialised)
			{
				Debug.Log("Multiplayer: broadcasting nothing - no world yet, or it has not finished initialising");
				return;
			}

			if (engine.ActiveSector == null)
			{
				Debug.Log("Multiplayer: broadcasting nothing - no active sector");
				return;
			}

			NetLog.Host("sent " + lastSent + " of " + lastSampled + " sampled ship(s) to "
				+ lastPlayers + " player(s) - " + lastSampled
				+ (lastSampled > 0 ? ", " : "") + Percent(lastSent, lastSampled)
				+ " was motion");

			Debug.Log("Multiplayer: sent " + lastSent + " of " + lastSampled
				+ " sampled ship(s) to " + lastPlayers + " player(s) over 2s");

			lastSampled = 0;
			lastSent = 0;
			lastPlayers = 0;
		}

		private static string Percent(int part, int whole)
		{
			return whole > 0 ? ((100 * part / whole) + "%") : "n/a";
		}

		private static void Broadcast(HostSession session)
		{
			EngineASX engine = EngineASX.Instance;

			Report(engine);

			// Nothing is sent rather than something incomplete. A client that
			// receives some units and not others cannot tell "that one is
			// missing" from "that one is stationary", so a partial frame is
			// worse than no frame.
			if (engine == null || engine.World == null || !engine.World.HasInitialised)
			{
				return;
			}

			for (int p = 0; p < session.Players.Count; p++)
			{
				RemotePlayer player = session.Players[p];

				// Admitted is not the same as arrived. A joiner that has
				// completed the handshake but has not yet finished loading
				// the snapshot cannot apply any of this, so sending to it
				// spends bandwidth on nothing.
				if (player == null || !player.Admitted || !player.Ready)
				{
					continue;
				}

				lastPlayers++;

				Send(engine, session, player);
			}
		}

		/// <summary>One player's worth of ships: their sector, and only what moved.</summary>
		private static void Send(EngineASX engine, HostSession session, RemotePlayer player)
		{
			string token = player.Identity ?? string.Empty;

			// Their own sector, not the host's. A client two jumps away from
			// the host used to be sent the host's neighbourhood, which is a
			// sector it cannot see and is not in.
			Sector sector = SectorFor(engine, session, token);

			if (sector == null)
			{
				return;
			}

			List<Unit> inSector = sector.GetUnitsByType(UnitType.Ship);

			if (inSector == null || inSector.Count == 0)
			{
				return;
			}

			Dictionary<int, Remembered> told = remembered.TryGetValue(token, out Dictionary<int, Remembered> found)
				? found
				: new Dictionary<int, Remembered>();

			remembered[token] = told;

			batch.Clear();
			seen.Clear();

			for (int i = 0; i < inSector.Count; i++)
			{
				Unit ship = inSector[i];

				if (ship == null || ship.IsDestroyed || ship.Sector != sector)
				{
					continue;
				}

				// The ship they are flying. They are simulating it; sending
				// them the host's copy would fight their own stick.
				if (session.Ownership.IsOwnedBy(ship.UniqueId, token))
				{
					continue;
				}

				lastSampled++;
				seen.Add(ship.UniqueId);

				UnitState? sampled = Sample(ship);

				if (!sampled.HasValue)
				{
					continue;
				}

				UnitState value = sampled.Value;
				Vector3 position = ship.transform.localPosition;
				Quaternion rotation = ship.transform.localRotation;

				// The tiering. A ship whose transform has not changed since
				// this player was last told is not sent again, which means a
				// docked ship costs nothing and a ship in a sector the engine
				// only ticks every few seconds costs almost nothing.
				if (told.TryGetValue(ship.UniqueId, out Remembered was) && was.Known
					&& (was.Position - position).sqrMagnitude < MovedEpsilon * MovedEpsilon
					&& Quaternion.Angle(was.Rotation, rotation) < FacingEpsilon)
				{
					continue;
				}

				told[ship.UniqueId] = new Remembered
				{
					Position = position,
					Rotation = rotation,
					Known = true
				};

				batch.Add(value);
				lastSent++;
			}

			// Ships that have left, been destroyed, or docked out of this
			// sector are dropped, so the memory of what was sent does not
			// grow for the whole life of the session.
			Prune(told);

			if (batch.Count == 0)
			{
				return;
			}

			List<UnitStateMessage> messages = UnitStateBatch.Pack(batch, Time.unscaledTime);

			for (int m = 0; m < messages.Count; m++)
			{
				try
				{
					player.Channel.Send(
						FramedMessage.Create(ProtocolVersion.UnitStateMessageId, messages[m].ToBytes()));
				}
				catch (System.Exception e)
				{
					// Said once per player, because swallowing this silently
					// is what made an entire session impossible to diagnose:
					// the host reported broadcasting sixty ships and the
					// client reported receiving none, and the gap between
					// those two facts had no name.
					if (failedPlayers.Add(token))
					{
						Debug.LogWarning("Multiplayer: a send to " + Short(player)
							+ " failed and will not be retried - " + e.Message);
					}
				}
			}
		}

		/// <summary>Drops what was sent about ships this player can no longer see.</summary>
		/// <remarks>
		/// Otherwise the record of what each player was told grows for the
		/// whole session, holding entries for ships destroyed hours ago and
		/// for sectors they flew through last week.
		///
		/// Only runs when the record is the larger of the two. It is
		/// otherwise wasted work, and the usual case is a record that exactly
		/// matches the sector.
		/// </remarks>
		private static void Prune(Dictionary<int, Remembered> told)
		{
			if (told.Count == 0 || told.Count <= seen.Count)
			{
				return;
			}

			List<int> gone = new List<int>();

			foreach (KeyValuePair<int, Remembered> entry in told)
			{
				if (!seen.Contains(entry.Key))
				{
					gone.Add(entry.Key);
				}
			}

			for (int i = 0; i < gone.Count; i++)
			{
				told.Remove(gone[i]);
			}
		}

		/// <summary>The sector this player's ship is in, or the host's if they have none.</summary>
		private static Sector SectorFor(EngineASX engine, HostSession session, string token)
		{
			foreach (int unitId in session.Ownership.UnitsOwnedBy(token))
			{
				Unit ship = engine.GetUnitByid(unitId);

				if (ship != null && ship.Sector != null)
				{
					return ship.Sector;
				}
			}

			// A player with no ship yet has no sector of their own. The host's
			// is the best guess: it is where they will be put.
			return engine.ActiveSector;
		}

		/// <summary>Reads one unit's state, or nothing if it is not simulable yet.</summary>
		private static UnitState? Sample(Unit unit)
		{
			if (unit == null || unit.transform == null)
			{
				return null;
			}

			Vector3 position = unit.transform.localPosition;
			Quaternion rotation = unit.transform.localRotation;

			// Velocity and turn only exist once the unit is live in the engine:
			// the rigidbody is created and destroyed with the active sector, so
			// asking a unit in another sector for one is asking for a null.
			Vector3 velocity = Vector3.zero;
			float currentTurn = 0f;

			ActiveUnit active = unit.ActiveUnit;

			if (active != null && active.ActiveUnitShip != null)
			{
				ActiveUnitShip ship = active.ActiveUnitShip;
				currentTurn = ship.CurrentTurn;

				if (unit.RBody != null)
				{
					velocity = unit.RBody.linearVelocity;
				}
			}

			return new UnitState
			{
				Id = unit.UniqueId,
				PositionX = position.x,
				PositionY = position.y,
				PositionZ = position.z,
				RotationX = rotation.x,
				RotationY = rotation.y,
				RotationZ = rotation.z,
				RotationW = rotation.w,
				VelocityX = velocity.x,
				VelocityY = velocity.y,
				VelocityZ = velocity.z,
				CurrentTurn = currentTurn
			};
		}
	}
}