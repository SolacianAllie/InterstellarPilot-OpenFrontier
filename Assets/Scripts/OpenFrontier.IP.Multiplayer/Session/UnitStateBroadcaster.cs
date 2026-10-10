using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells the session's clients where everything is.
	///
	/// Runs on the host only. It samples the ships in the active sector ten
	/// times a second and sends each client the same batch, because the host
	/// is the only copy of the world that is simulated rather than told.
	///
	/// Ten a second is a deliberate choice rather than a comfortable one. A
	/// client that gets a position and a velocity every 100ms can draw the
	/// gap between them smoothly, so it never has to arrive at 60Hz to look
	/// smooth - and it means a session of four people costs a few kilobytes a
	/// second rather than a few hundred. Slower than the eye and cheaper than
	/// the alternative.
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

		private static readonly List<UnitState> scratch = new List<UnitState>(UnitStateMessage.MaxUnits * 4);

		private float nextUpdateTime;

		private void Update()
		{
			HostSession session = MultiplayerHostService.Session;

			if (session == null || session.Players.Count == 0)
			{
				return;
			}

			if (Time.unscaledTime < nextUpdateTime)
			{
				return;
			}

			nextUpdateTime = Time.unscaledTime + (1f / UpdatesPerSecond);

			Broadcast(session);
		}

		private static void Broadcast(HostSession session)
		{
			EngineASX engine = EngineASX.Instance;
			Sector sector = engine?.ActiveSector;

			// Nothing is sent rather than something incomplete. A client that
			// receives some units and not others cannot tell "that one is
			// missing" from "that one is stationary", so a partial frame is
			// worse than no frame.
			if (engine == null || sector == null || engine.World == null || !engine.World.HasInitialised)
			{
				return;
			}

			List<Unit> ships = sector.GetUnitsByType(UnitType.Ship);

			if (ships == null || ships.Count == 0)
			{
				return;
			}

			scratch.Clear();

			for (int i = 0; i < ships.Count; i++)
			{
				Unit ship = ships[i];

				if (ship == null || ship.IsDestroyed)
				{
					continue;
				}

				UnitState? state = Sample(ship);

				if (state.HasValue)
				{
					scratch.Add(state.Value);
				}
			}

			if (scratch.Count == 0)
			{
				return;
			}

			List<UnitStateMessage> messages = UnitStateBatch.Pack(scratch);

			for (int p = 0; p < session.Players.Count; p++)
			{
				RemotePlayer player = session.Players[p];

				if (player == null || !player.Admitted)
				{
					continue;
				}

				for (int m = 0; m < messages.Count; m++)
				{
					try
					{
						player.Channel.Send(
							FramedMessage.Create(ProtocolVersion.UnitStateMessageId, messages[m].ToBytes()));
					}
					catch (System.Exception)
					{
						// A player who has gone away mid-send is not this loop's
						// problem: they will be noticed on their own close, and
						// abandoning the rest of the batch over one bad socket
						// would starve everyone else.
					}
				}
			}
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
