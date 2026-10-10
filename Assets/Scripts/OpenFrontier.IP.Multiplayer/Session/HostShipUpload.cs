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

			for (int i = 0; i < units.Length; i++)
			{
				UnitState state = units[i];

				if (!session.Ownership.IsOwnedBy(state.Id, claimantToken))
				{
					continue;
				}

				Unit unit = engine.GetUnitByid(state.Id);

				if (unit == null || unit.IsDestroyed)
				{
					continue;
				}

				Write(unit, state);
				applied++;
			}

			return applied;
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