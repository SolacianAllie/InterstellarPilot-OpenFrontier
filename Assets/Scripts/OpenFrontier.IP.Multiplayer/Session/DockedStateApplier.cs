using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Puts every copy's ships where the host says they are.
	/// </summary>
	/// <remarks>
	/// The one thing in this file that a copy is not supposed to do is decide,
	/// and it decides nothing: the host says which hangar and which bay, and
	/// this applies it or says why it could not. It is here rather than in
	/// UnitStateApplier because docked-ness is a discrete fact about the world
	/// rather than a transform, and the applier's whole design is that it
	/// counts and leaves movement to the interpolator - a ship must not arrive
	/// somewhere by being told to rather than by being flown there.
	///
	/// Applied on every copy, not only the one that asked. Two players
	/// watching the same ship must see it in the same hangar, and a joiner who
	/// was not in the session when the dock happened gets it from the snapshot
	/// instead.
	///
	/// A failure is said out loud rather than swallowed. The host decided and
	/// this copy could not carry it out, which means the two disagree about
	/// where a ship is, and the only useful moment to find that out is now.
	/// </remarks>
	public sealed class DockedStateApplier : MonoBehaviour
	{
		public const string ObjectName = "Multiplayer Docked State";

		private int appliedTotal;
		private int unknownTotal;

		private ClientSession session;

		private static DockedStateApplier instance;

		public static DockedStateApplier Instance => instance;

		private void Awake()
		{
			instance = this;
		}

		private void OnEnable()
		{
			// Subscribed rather than pushed to, because this and the session
			// are installed at different times and either may already be up
			// when the other appears - same arrangement as the faction applier.
			Attach();
			MultiplayerPump.AddedPumpTick += Attach;
		}

		private void OnDisable()
		{
			if (session != null)
			{
				session.DockedStateReceived -= Apply;
			}

			MultiplayerPump.AddedPumpTick -= Attach;
		}

		private void Attach()
		{
			ClientSession current = MultiplayerClientService.Instance == null
				? null
				: MultiplayerClientService.Instance.Session;

			if (current == null || current == session)
			{
				return;
			}

			if (session != null)
			{
				session.DockedStateReceived -= Apply;
			}

			session = current;
			session.DockedStateReceived += Apply;
		}

		private void Apply(DockedStateMessage message)
		{
			EngineASX engine = EngineASX.Instance;
			Unit ship = engine == null ? null : engine.GetUnitByid(message.ShipId);

			if (ship == null || ship.Components == null)
			{
				unknownTotal++;

				if (unknownTotal == 1 || unknownTotal % 50 == 0)
				{
					Debug.LogWarning("Multiplayer: " + unknownTotal
						+ " docked-state message(s) so far have named a ship this copy"
						+ " does not have - the latest was " + message.ShipId
						+ ". Usually this means the world has not finished arriving.");
				}

				return;
			}

			if (!message.IsDocked)
			{
				ship.Components.UndockIfDocked();
				appliedTotal++;

				Debug.Log("Multiplayer: ship " + message.ShipId + " is out of its hangar");

				return;
			}

			Unit dock = engine.GetUnitByid(message.DockUnitId);

			if (dock == null)
			{
				Debug.LogWarning("Multiplayer: the host says ship " + message.ShipId
					+ " is docked at unit " + message.DockUnitId
					+ ", which this copy does not have. It is not docked here, and"
					+ " the two copies now disagree about where it is.");

				return;
			}

			UnitHangar hangar = dock.GetHangar();

			if (hangar == null)
			{
				Debug.LogWarning("Multiplayer: the host says ship " + message.ShipId
					+ " is docked at unit " + message.DockUnitId
					+ ", which has no hangar on this copy.");

				return;
			}

			// Already in the right place is success, not a second dock.
			if (ship.Components.IsDocked
				&& ship.Components.DockedInHangarBay != null
				&& ship.Components.DockedInHangarBay.BayId == message.BayId)
			{
				appliedTotal++;

				return;
			}

			// Somewhere else, so out first. The host's word is the whole of
			// what is known about where this ship should be.
			ship.Components.UndockIfDocked();

			if (!ship.Components.TryDockInHangar(hangar, message.BayId))
			{
				Debug.LogWarning("Multiplayer: the host says ship " + message.ShipId
					+ " is docked at unit " + message.DockUnitId + ", bay "
					+ message.BayId + ", and this copy cannot put it there - the bay"
					+ " is occupied, too small, or gone. The two copies now disagree"
					+ " about where that ship is.");

				return;
			}

			appliedTotal++;

			Debug.Log("Multiplayer: ship " + message.ShipId + " is docked at station "
				+ message.DockUnitId + ", bay " + message.BayId);
		}
	}
}