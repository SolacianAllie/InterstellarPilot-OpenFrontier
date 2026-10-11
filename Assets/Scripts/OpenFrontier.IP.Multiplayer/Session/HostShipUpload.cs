using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Takes a joining player's ship state and writes it onto the ship.
	/// </summary>
	/// <remarks>
	/// This is how a remote player flies at all. The client simulates its own
	/// ship so the stick feels like a stick, and uploads where the ship ended
	/// up; the host puts it there and re-broadcasts on its own schedule to
	/// everyone else. From here it is indistinguishable from the AI having
	/// flown it, which is exactly the intent.
	///
	/// Kept out of HostSession deliberately. That class is the handshake, and
	/// the handshake has no business touching the engine - it is the one piece
	/// of this system that can be tested without Unity, and folding engine
	/// types into it took that away for the sake of fifteen lines.
	///
	/// The safety property is that a message is believed only where the
	/// ownership registry says this player has that ship. A client could
	/// otherwise claim the whole world by sending every unit in its copy, so
	/// the check is on what the host decided, never on what the message says.
	/// </remarks>
	public static class HostShipUpload
	{
		/// <summary>
		/// Applies a player's upload, keeping only the ships they own.
		/// </summary>
		/// <returns>How many units were written.</returns>
		public static int Apply(HostSession session, string claimantToken, byte[] payload)
		{
			if (session == null || payload == null)
			{
				return 0;
			}

			if (!UnitStateMessage.TryFromBytes(payload, out UnitStateMessage states, out string error))
			{
				Debug.LogWarning("Multiplayer: unreadable ship upload - " + error);
				return 0;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return 0;
			}

			UnitState[] units = states.Units;
			int applied = 0;
			int notOurs = 0;
			int unknown = 0;

			// Done before the walk below so that a ship moved into a new
			// sector is written at its new local position rather than at one
			// that belonged to the sector it just left.
			int moved = MoveToSector(engine, session, claimantToken, states);

			for (int i = 0; i < units.Length; i++)
			{
				UnitState state = units[i];

				if (!session.Ownership.IsOwnedBy(state.Id, claimantToken))
				{
					notOurs++;
					continue;
				}

				Unit unit = engine.GetUnitByid(state.Id);

				if (unit == null || unit.IsDestroyed)
				{
					unknown++;
					continue;
				}

				Write(unit, state);
				applied++;
			}

			Count(applied, notOurs, unknown, claimantToken, moved);

			return applied;
		}

		/// <summary>
		/// Puts the player's ships in the sector they said they were in.
		/// </summary>
		/// <param name="session">Decides what this player owns.</param>
		/// <param name="claimantToken">Who is claiming these ships.</param>
		/// <param name="states">The upload, carrying the sector on its batch.</param>
		/// <remarks>
		/// Only ever for ships the ownership registry already says this
		/// player has, which is the same safety property as the position
		/// above: believed where the host decided, never where the message
		/// says. A client cannot move anything it does not already own.
		///
		/// Zero means the host was not told, which is what a build from
		/// before this field sends, and everything is left where it is.
		///
		/// This is the whole of sector travel. Without it the host's copy of
		/// a player's ship stays in the sector it left, so the host goes on
		/// working out which ships to send that player from a place they left
		/// minutes ago - correct arithmetic on a world that stopped being
		/// true some time ago, which is the hardest kind of wrong to notice.
		/// </remarks>
		private static int MoveToSector(
			EngineASX engine,
			HostSession session,
			string claimantToken,
			UnitStateMessage states)
		{
			int wanted = states.SectorId;

			if (wanted <= 0)
			{
				return 0;
			}

			Sector target = engine.GetSectorById(wanted);

			if (target == null)
			{
				// Said, because it is a disagreement between two copies of the
				// world about which sectors exist, and nothing else reports it.
				Debug.LogWarning("Multiplayer: a player says their ship is in sector "
					+ wanted + ", which this world does not have. Left where it is.");

				return 0;
			}

			UnitState[] units = states.Units;
			int moved = 0;

			for (int i = 0; i < units.Length; i++)
			{
				UnitState state = units[i];

				if (!session.Ownership.IsOwnedBy(state.Id, claimantToken))
				{
					continue;
				}

				Unit ship = engine.GetUnitByid(state.Id);

				if (ship == null || ship.IsDestroyed || ship.Sector == target)
				{
					continue;
				}

				ship.Sector = target;
				moved++;
			}

			if (moved > 0)
			{
				Debug.Log("Multiplayer: moved " + moved + " ship(s) into sector "
					+ wanted + ", as the player flying them said");
			}

			return moved;
		}

		private static int reportedApplied = -1;
		private static int reportedNotOurs;
		private static int reportedMoved = -1;
		private static float nextReportTime;

		/// <summary>
		/// What the host did with what a client told it about its ship.
		/// </summary>
		/// <remarks>
		/// There was no way to tell, and the symptom is the same from either
		/// side: a client whose uploads are all being dropped sees its ship
		/// move locally and nowhere else, and a host that is dropping them
		/// sees a ship that does not move. Both look like a broken connection
		/// and neither had anything in its log to say so.
		///
		/// The three counts are reported apart rather than as one total,
		/// because "nothing applied" has three quite different causes - not
		/// owned, no such ship here, or no upload arriving at all - and only
		/// one of them is about ownership.
		/// </remarks>
		private static void Count(int applied, int notOurs, int unknown, string token, int moved)
		{
			if (Time.unscaledTime < nextReportTime)
			{
				return;
			}

			nextReportTime = Time.unscaledTime + 2f;

			// Only when it changed. The same wrong number every two seconds
			// for the length of a session is how a log stops being read.
			// Sector moves are part of the comparison because they are the
			// rare, interesting case - a ship crossing a wormhole happens
			// twice in a session and the surrounding noise is every frame.
			if (applied == reportedApplied && notOurs == reportedNotOurs
				&& moved == reportedMoved)
			{
				return;
			}

			reportedApplied = applied;
			reportedNotOurs = notOurs;
			reportedMoved = moved;

			string what = applied > 0
				? "applied"
				: notOurs > 0
					? "NOTHING APPLIED - the host does not think this player owns that ship"
					: "nothing to apply";

			Debug.Log("Multiplayer: ship upload from "
				+ (string.IsNullOrEmpty(token) ? "a player" : token) + ": "
				+ applied + " applied, " + notOurs + " not this player's, "
				+ unknown + " not in this world, " + moved + " moved sector - " + what);
		}

		/// <summary>
		/// Whether a ship may be claimed by a given player.
		/// </summary>
		/// <remarks>
		/// Anything of the host's own faction, which is every ship the local
		/// player owns. The ownership registry refuses a ship another PLAYER
		/// holds but had no answer for the host's own, which carry no owner
		/// entry - so a client pointed at one could claim it, and the host
		/// would then apply that client's position to its own ship every
		/// frame. It was seen doing exactly that.
		///
		/// Unknown ships are refused too: a message naming a ship nobody has
		/// is either a stale client or a hostile one.
		///
		/// Shared factions would want this narrower, and the ownership record
		/// is what makes that a change to this one test rather than a
		/// rewrite - which is the reason it was built the way it was.
		/// </remarks>
		public static bool MayBeClaimed(int unitId, string claimantToken)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return true;
			}

			Unit unit = engine.GetUnitByid(unitId);

			if (unit == null)
			{
				// Refused, and this used to be permitted.
				//
				// Ownership is a claim about an object both copies have. If
				// the host has never heard of this ship then there is nothing
				// to own: TryClaim succeeds because nobody holds it, the host
				// broadcasts to every client that this player owns it, and the
				// registry holds that forever. HostShipUpload.Apply then
				// counts it unknown and drops it, so the ship is inert on the
				// host while the client flies it - a ship that exists for one
				// player and for nobody else, with a permanent entry in the
				// ownership table pointing at it.
				//
				// Almost always this is the buy-a-ship bug wearing a disguise.
				// A client presses Buy, ShipBuyScreen.SpawnUnit makes a real
				// Unit in that copy alone, buy-and-enter flies it, and the
				// client asks for the new id. The id is an engine counter
				// value around 100500 rather than a player-band one, so it
				// will not even pass the IsPlayerMade test.
				//
				// So this refusal is doing two jobs: it stops the phantom
				// entry, and it is the loudest symptom of an unrelayed world
				// mutation. Said in those words, because HostSession answers
				// every refusal from here with "it belongs to the host", which
				// is not what happened and would send whoever reads it looking
				// at the wrong thing.
				Debug.LogWarning("Multiplayer: refusing ship " + unitId
					+ " to " + claimantToken + " - this world has no such ship."
					+ " Usually this means the client built something in its own"
					+ " copy and asked to own it, which no message can carry yet.");

				return false;
			}

			if (unit.Faction == null)
			{
				// Unowned, which means claimable - the opposite of what this
				// used to say.
				//
				// Refusing was right about the case it was written for and
				// wrong about the case it actually hit. It went in to stop a
				// client claiming a ship the host had never heard of, but that
				// is the null-unit branch above, and this branch is a ship that
				// exists and belongs to nobody. Abandoning a ship does exactly
				// that - unit.Faction = null, and the ship floats on free for
				// anybody to take - so refusing here made the one ship in the
				// game that is meant to be claimable the one ship nobody could
				// claim.
				//
				// Said out loud, because "claimed a wreck" and "claimed
				// somebody's ship" look identical in the ownership table
				// afterwards and only one of them is a bug.
				Debug.Log("Multiplayer: ship " + unitId + " belongs to nobody, so "
					+ claimantToken + " may have it");

				return true;
			}

			if (unit.Faction != engine.LocalFaction)
			{
				return true;
			}

			// Said with what it compared, because "it belongs to the host" on
			// its own is a conclusion and this is the check that reached it.
			// The refusal that started this was correct - the ship's faction
			// really was the host's - and there was no way to tell that from
			// a player clicking on somebody else's ship, which would need no
			// fix at all. Those two are identical from the log and opposite
			// to act on.
			Faction host = engine.LocalFaction;

			Debug.LogWarning("Multiplayer: ship " + unitId + " is in faction "
				+ host.UniqueId + " ('" + host.Name + "'), which is this host's own"
				+ " faction, so it cannot be claimed by a player."
				+ " The ship was made in the wrong faction - the two are the same"
				+ " object, not merely the same number.");

			return false;
		}

		private static void Write(Unit unit, UnitState state)
		{
			Transform transform = unit.transform;

			transform.localPosition = new Vector3(state.PositionX, state.PositionY, state.PositionZ);
			transform.localRotation = new Quaternion(
				state.RotationX, state.RotationY, state.RotationZ, state.RotationW);

			if (unit.RBody != null)
			{
				if (unit.RBody.IsSleeping())
				{
					// A held unit is asleep, and a sleeping rigidbody ignores
					// a velocity change - so without this the first upload
					// after the gate put the ship to sleep would be swallowed
					// and the ship would sit still until it happened to move
					// enough to matter.
					unit.RBody.WakeUp();
				}

				unit.RBody.linearVelocity = new Vector3(
					state.VelocityX, state.VelocityY, state.VelocityZ);
			}

			// The throttle, which the client sends and this used to drop.
			//
			// Position, rotation and velocity all said where the ship is and
			// how it is moving, and none of them said whether the engines were
			// lit. So a remote player's ship arrived at the host gliding with
			// its thrusters cold, on a straight line, at whatever velocity the
			// last physics step produced - a ship being flown, visibly not.
			//
			// This copy simulates that ship, so its throttle is the client's
			// to give, exactly as its position is. The host's own physics will
			// overwrite it next step, and that is fine: the client's number is
			// the one that came from the stick.
			if (unit.Components != null)
			{
				unit.Components.EngineThrottle = state.Throttle;
			}
		}
	}
}