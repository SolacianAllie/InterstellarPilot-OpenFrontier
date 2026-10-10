using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Decides who is allowed to move a ship on this copy of the world.
	/// </summary>
	/// <remarks>
	/// Host-authoritative, with one exception, and the exception is the whole
	/// point: a player flies their own ship. Everything else - the rest of
	/// their fleet, every other faction, the AI - belongs to the host, because
	/// the host is the one running it.
	///
	/// That is decided by whether the client keeps simulating a unit at all.
	/// Two things writing a position is the failure this exists to prevent:
	/// applying the host's state on top of a local simulation means the result
	/// is whichever ran last, so ships arrive roughly right and then swim. Not
	/// simulating is what lets the host's numbers be the only numbers.
	///
	/// The gate is a brake, not a takeover. It does not switch units off -
	/// the host's messages still arrive and are still applied, and a unit
	/// coming back under local control resumes immediately with no unwiring.
	///
	/// Deliberately not authority-aware beyond that single rule yet. The
	/// intended next step is handover - autopilot hands a ship to the host,
	/// taking the stick hands it back - and that needs a per-unit notion of
	/// ownership that has no reason to exist until the basic split works.
	/// </remarks>
	public sealed class AuthorityGate : MonoBehaviour
	{
		/// <summary>Whether this copy is a client in someone else's session.</summary>
		public bool IsClient { get; set; }

		/// <summary>Units held still this frame, for the host's log to read.</summary>
		public int SuppressedCount { get; private set; }

		private int suppressedLastUpdate;

		private void LateUpdate()
		{
			SuppressedCount = suppressedLastUpdate;
			suppressedLastUpdate = 0;
		}

		/// <summary>
		/// Holds still every unit this copy does not own.
		/// </summary>
		/// <remarks>
		/// Zeroing the turn and clearing the forces rather than disabling the
		/// component, deliberately. Disabling is the obvious move and it
		/// leaves the ship exactly where it was, still holding last frame's
		/// velocity, so it coasts on through the next few physics steps and
		/// the joiner sees ships drifting away from where the host put them.
		/// Zeroing both means the body has nothing left to act on.
		///
		/// The rigidbody is put to sleep for the same reason. A sleeping body
		/// is not integrated, so the position the host wrote stays put until
		/// the next message says otherwise. Waking it on every arriving state
		/// keeps ordinary movement working.
		/// </remarks>
		public void SuppressUnownedUnits()
		{
			suppressedLastUpdate = 0;

			if (!IsClient)
			{
				return;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			List<Unit> units = engine.UpdatableUnits;

			if (units == null)
			{
				return;
			}

			for (int i = 0; i < units.Count; i++)
			{
				Unit unit = units[i];

				if (unit == null || unit.IsDestroyed || unit.IsActiveInEngine)
				{
					// Active units are the ones being flown, and those are the
					// ones this player owns.
					continue;
				}

				Hold(unit);
				suppressedLastUpdate++;
			}
		}

		/// <summary>
		/// Whether the local player is flying this unit, and so owns it.
		/// </summary>
		/// <remarks>
		/// True only for a unit in the engine AND of the local faction. Both
		/// halves are needed: a unit in the engine can be an NPC the player
		/// is watching, and a unit of the local faction sitting in a hangar is
		/// not being flown at all.
		/// </remarks>
		public static bool IsLocallyOwned(EngineASX engine, Unit unit)
		{
			if (engine == null || unit == null)
			{
				return false;
			}

			return unit.IsActiveInEngine && unit.Faction == engine.LocalFaction;
		}

		private static void Hold(Unit unit)
		{
			ActiveUnit active = unit.GetComponent<ActiveUnit>();

			if (active != null && active.ActiveUnitShip != null)
			{
				// Zeroing the turn stops ApplyTurn writing a rotation next
				// frame; clearing the forces removes the thrust that would
				// otherwise keep accelerating the body.
				active.ActiveUnitShip.CurrentTurn = 0f;
				active.ActiveUnitShip.DesiredTurn = 0f;
			}

			if (unit.RBody != null)
			{
				unit.RBody.linearVelocity = Vector3.zero;
				unit.RBody.angularVelocity = Vector3.zero;

				if (unit.RBody.IsSleeping() == false)
				{
					unit.RBody.Sleep();
				}
			}
		}
	}
}