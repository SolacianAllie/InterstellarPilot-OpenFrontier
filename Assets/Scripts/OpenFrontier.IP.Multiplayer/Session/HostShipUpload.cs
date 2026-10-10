using OpenFrontier.IP.Engine;
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

			Count(applied, notOurs, unknown, claimantToken);

			return applied;
		}

		private static int reportedApplied = -1;
		private static int reportedNotOurs;
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
		private static void Count(int applied, int notOurs, int unknown, string token)
		{
			if (Time.unscaledTime < nextReportTime)
			{
				return;
			}

			nextReportTime = Time.unscaledTime + 2f;

			// Only when it changed. The same wrong number every two seconds
			// for the length of a session is how a log stops being read.
			if (applied == reportedApplied && notOurs == reportedNotOurs)
			{
				return;
			}

			reportedApplied = applied;
			reportedNotOurs = notOurs;

			string what = applied > 0
				? "applied"
				: notOurs > 0
					? "NOTHING APPLIED - the host does not think this player owns that ship"
					: "nothing to apply";

			Debug.Log("Multiplayer: ship upload from "
				+ (string.IsNullOrEmpty(token) ? "a player" : token) + ": "
				+ applied + " applied, " + notOurs + " not this player's, "
				+ unknown + " not in this world - " + what);
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

			if (unit == null || unit.Faction == null)
			{
				return true;
			}

			return unit.Faction != engine.LocalFaction;
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
		}
	}
}