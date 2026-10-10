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
		private static readonly Dictionary<int, float> lastAppliedAt = new Dictionary<int, float>();

		/// <summary>
		/// Seconds since this ship was last written, or a large number.
		/// </summary>
		/// <remarks>
		/// Asked by the authority gate, which would otherwise put the body
		/// straight back to sleep on the very next frame after the applier
		/// woke it and gave it a velocity. The two were cancelling each other
		/// out ten times a second, so the velocity was never acted on and
		/// ships moved in visible ten-hertz steps.
		/// </remarks>
		public static float SecondsSinceApplied(int unitId)
		{
			return lastAppliedAt.TryGetValue(unitId, out float when)
				? Time.unscaledTime - when
				: float.MaxValue;
		}

		private MultiplayerClientService service;
		private AuthorityGate gate;

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
		public int AppliedCount => appliedTotal;

		/// <summary>
		/// How many arrived naming a unit this copy does not have.
		/// </summary>
		/// <remarks>
		/// Not a warning yet, because it is expected right now: the client is
		/// in its own world, and unit ids only line up once it has been given
		/// the host's. It is counted rather than ignored because the day that
		/// number stops being large is the day the two worlds have agreed.
		/// </remarks>
		public int UnknownCount => unknownTotal;

		private void Awake()
		{
			// The gate belongs to this component rather than being installed on
			// its own, because there is exactly one and it has to agree with
			// who is applying the states - two independently installed halves
			// would drift apart on a reconnect.
			gate = gameObject.AddComponent<AuthorityGate>();

			// So does the interpolator, for the same reason and the opposite
			// reason: the gate decides which ships this copy may not move, and
			// the interpolator moves everything else. Installed apart from
			// each other they would disagree about who owns what.
			gameObject.AddComponent<UnitInterpolator>();
		}

		private float nextReportTime;

		private int receivedTotal;
		private int appliedTotal;
		private int unknownTotal;
		private int reportedApplied;
		private int reportedUnknown;

		/// <summary>
		/// What came in and what became of it.
		/// </summary>
		/// <remarks>
		/// The count of units naming something this copy does not have is the
		/// single most useful number in this feature and it was never printed.
		/// Zero means the two worlds agree about ids. A large number means they
		/// have diverged, and nothing else is worth investigating until they
		/// have not.
		/// </remarks>
		private void Report()
		{
			if (Time.unscaledTime < nextReportTime)
			{
				return;
			}

			nextReportTime = Time.unscaledTime + 2f;

			if (MultiplayerClientService.Instance == null || !MultiplayerClientService.Instance.IsConnected)
			{
				return;
			}

			// Counted since the last report, not since the last frame.
			//
			// The network queue is drained in MultiplayerPump.Update, which
			// runs AFTER LateUpdate, so a counter reset here is zeroed and
			// then filled - and the report, reading it immediately, could
			// only ever print zero. Which is exactly what it printed while
			// several hundred messages a second came in. The sync may well
			// have been working the whole time; the instrument could not see it.
			int applied = appliedTotal - reportedApplied;
			int unknown = unknownTotal - reportedUnknown;

			reportedApplied = appliedTotal;
			reportedUnknown = unknownTotal;

			NetLog.Client("applying - " + applied + " applied, "
				+ unknown + " unknown, of " + receivedTotal + " received (totals "
				+ appliedTotal + "/" + unknownTotal + "/" + receivedTotal + ")");

			// What this copy refused to simulate, alongside what it was sent.
			//
			// The refusal count is the one number that says the authority
			// gate is working, and it is here rather than on the host because
			// this is the only side that can be wrong quietly. A host that
			// believes it stopped a client's simulation, when it did not,
			// looks exactly like a host that is working.
			int declined = Authority.TakeDeclinedSinceReport();

			Debug.Log("Multiplayer: applying - " + applied + " applied, "
				+ unknown + " named units this copy does not have, of "
				+ receivedTotal + " received; declined to simulate "
				+ declined + " ship update(s) this period");
		}

		private void LateUpdate()
		{
			Report();

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

			receivedTotal += message.Count;

			// Taken before the walk below, so that a unit this copy does not
			// have is dropped from the samples as well as counted. Keeping
			// samples for a ship that does not exist would grow without
			// anything ever reading them.
			UnitInterpolator.Record(message);

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
					unknownTotal++;
					continue;
				}

				Apply(unit, state);
			}
		}

		/// <summary>
		/// Takes one sample. The drawing is somebody else's job.
		/// </summary>
		/// <remarks>
		/// This used to write the position onto the transform and hand the
		/// rigidbody a velocity, which is how a ship ended up jumping ten
		/// times a second with a physics step carrying it somewhere between
		/// the two most recent messages. Recording instead leaves the
		/// interpolator free to draw a moment in the past, where it knows
		/// where the ship is going as well as where it is.
		///
		/// The unit is still looked up, and still counted if it is missing,
		/// because that number is how a divergence between the two worlds
		/// gets noticed at all.
		/// </remarks>
		private void Apply(Unit unit, UnitState state)
		{
			lastAppliedAt[state.Id] = Time.unscaledTime;
			appliedTotal++;
		}
	}
}
