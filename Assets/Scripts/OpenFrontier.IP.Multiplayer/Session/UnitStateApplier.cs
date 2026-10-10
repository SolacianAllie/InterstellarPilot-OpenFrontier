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
	/// Applies on top of <see cref="AuthorityGate"/>, which is what stops a
	/// client simulating the units it does not own. Without the gate this
	/// would be two things writing the same position, and the result would be
	/// whichever ran last - ships would arrive roughly right and then swim.
	///
	/// The ordering between them matters. The gate runs in LateUpdate, after
	/// this has written the host's numbers for the frame, and a sleeping
	/// rigidbody is not integrated by the physics step, so what this writes
	/// is what stays.
	/// </summary>
	public sealed class UnitStateApplier : MonoBehaviour
	{
		private readonly Dictionary<int, float> lastAppliedAt = new Dictionary<int, float>();

		private MultiplayerClientService service;
		private AuthorityGate gate;
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

		private void Awake()
		{
			// The gate belongs to this component rather than being installed on
			// its own, because there is exactly one and it has to agree with
			// who is applying the states - two independently installed halves
			// would drift apart on a reconnect.
			gate = gameObject.AddComponent<AuthorityGate>();
		}

		private void LateUpdate()
		{
			appliedLastUpdate = 0;
			unknownLastUpdate = 0;

			if (gate != null)
			{
				// Re-read rather than decided once: the applier goes in when a
				// join starts, and this is what stops the client fighting its
				// own copy of the world.
				// Connected, not merely present. This matters because a
				// client service exists the moment anybody joins, and on one
				// machine both copies host AND join: testing for the service
				// alone made a host that had also joined treat its own world
				// as somebody else's and stand every ship still.
				MultiplayerClientService client = MultiplayerClientService.Instance;
				bool joined = client != null && client.IsConnected;

				gate.IsClient = joined;
				gate.Ownership = joined ? client.OwnershipView : null;
				gate.SuppressUnownedUnits();
			}
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
				// Wake first. AuthorityGate puts unowned bodies to sleep so they
				// stay where the host put them, and a sleeping rigidbody ignores
				// a velocity change - so the first message after waking would be
				// swallowed and the ship would sit still.
				if (unit.RBody.IsSleeping())
				{
					unit.RBody.WakeUp();
				}

				unit.RBody.linearVelocity = new Vector3(
					state.VelocityX, state.VelocityY, state.VelocityZ);
			}
		}
	}
}
