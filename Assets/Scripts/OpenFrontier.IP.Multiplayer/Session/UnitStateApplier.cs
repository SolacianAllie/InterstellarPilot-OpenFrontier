using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Puts a client where the host says things are.
	///
	/// The other half of <see cref="UnitStateBroadcaster"/>. Every unit state
	/// that arrives is looked up by id and written onto the transform.
	///
	/// IMPORTANT, and the reason this does not yet look smooth: a client is
	/// still simulating every unit in its copy of the world, including the
	/// ones the host is telling it about. Applying a position on top of a
	/// local simulation is two things writing the same field, so the result is
	/// whichever ran last. Ships arrive in roughly the right place and then
	/// swim.
	///
	/// That is deliberate sequencing, not an oversight: the fix is the
	/// authority gate - the client stops simulating anything the host owns -
	/// and shipping the message first means the gate has something to switch
	/// off. It is the same reason the two halves are built in this order.
	/// </summary>
	public sealed class UnitStateApplier : MonoBehaviour
	{
		private readonly Dictionary<int, float> lastAppliedAt = new Dictionary<int, float>();

		private MultiplayerClientService service;
		private int appliedLastUpdate;
		private int unknownLastUpdate;

		private void OnEnable()
		{
			// Subscribed rather than pushed to, and re-checked each frame,
			// because the applier and the service are installed at different
			// times and either may already be up when the other appears.
			// Whoever arrives second does the connecting.
			Attach();

			MultiplayerPump.AddedPumpTick += Attach;
		}

		private void OnDisable()
		{
			if (service != null)
			{
				service.UnitStateReceived -= Apply;
			}

			MultiplayerPump.AddedPumpTick -= Attach;
		}

		private void Attach()
		{
			MultiplayerClientService current = MultiplayerClientService.Instance;

			if (current == null || current == service)
			{
				return;
			}

			if (service != null)
			{
				service.UnitStateReceived -= Apply;
			}

			service = current;
			service.UnitStateReceived += Apply;
		}

		/// <summary>How many unit states were written since the last frame.</summary>
		public int AppliedCount => appliedLastUpdate;

		/// <summary>
		/// How many arrived naming a unit this copy does not have.
		/// </summary>
		/// <remarks>
		/// Not a warning yet, because it is expected right now: the client is
		/// in its own world, and unit ids only line up once it has been given
		/// the host's. It is counted rather than ignored because the day that
		/// number stops being large is the day the two worlds have agreed.
		/// </remarks>
		public int UnknownCount => unknownLastUpdate;

		private void LateUpdate()
		{
			appliedLastUpdate = 0;
			unknownLastUpdate = 0;
		}

		/// <summary>Applies one batch, ignoring any unit this copy does not have.</summary>
		public void Apply(UnitStateMessage message)
		{
			if (message.Count == 0)
			{
				return;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			UnitState[] units = message.Units;

			for (int i = 0; i < units.Length; i++)
			{
				UnitState state = units[i];
				Unit unit = engine.GetUnitByid(state.Id);

				if (unit == null || unit.IsDestroyed || unit.transform == null)
				{
					// A unit the host has and this copy does not. Skipped
					// rather than created: inventing one would need a prefab,
					// a sector and a faction, and guessing would be worse than
					// missing until the two worlds are actually the same.
					unknownLastUpdate++;
					continue;
				}

				Apply(unit, state);
			}
		}

		private void Apply(Unit unit, UnitState state)
		{
			Transform transform = unit.transform;

			transform.localPosition = new Vector3(state.PositionX, state.PositionY, state.PositionZ);
			transform.localRotation = new Quaternion(
				state.RotationX, state.RotationY, state.RotationZ, state.RotationW);

			lastAppliedAt[state.Id] = Time.unscaledTime;
			appliedLastUpdate++;

			// Velocity is handed to the rigidbody rather than written onto the
			// transform, because the next physics step will read it and carry
			// the ship on its own. Setting linearVelocity directly is what the
			// save importer does for the same field, and doing it this way
			// means a client already simulating the ship ends up agreeing with
			// the host rather than fighting it.
			if (unit.RBody != null)
			{
				unit.RBody.linearVelocity = new Vector3(
					state.VelocityX, state.VelocityY, state.VelocityZ);
			}
		}
	}
}
