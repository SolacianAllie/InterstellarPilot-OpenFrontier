using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells the engine what this copy is allowed to decide.
	/// </summary>
	/// <remarks>
	/// Installed at startup and never removed, which is safe only because of
	/// one decision: the answer is read live from the connection rather than
	/// remembered. A provider left over from a session that has ended says
	/// "not a client", so the engine is told it may simulate everything and a
	/// solo player is unaffected.
	///
	/// The alternative - unregistering on disconnect - is one forgotten
	/// path away from a game where nothing moves and nothing says why. That
	/// failure mode is silent, it looks like a pause, and it is the single
	/// worst thing that could happen to someone's save. A provider that is
	/// always installed and always answers correctly cannot do it.
	///
	/// The rule it expresses: on a client, the only thing this copy simulates
	/// is the ship the player is flying by hand.
	/// </remarks>
	public sealed class ClientAuthorityProvider : IAuthorityProvider
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Install()
		{
			Authority.Provider = Instance;

			// Same hook shape as the authority provider, for the same reason:
			// the orders live in the game assembly and cannot reach across.
			FleetOrderCommands.Install();
		}

		/// <summary>The one provider, since there is one copy of the world per process.</summary>
		public static readonly ClientAuthorityProvider Instance = new ClientAuthorityProvider();

		public bool IsThisCopyAClient
		{
			get
			{
				// Live, every time. Never cached, never remembered.
				MultiplayerClientService client = MultiplayerClientService.Instance;

				return client != null && client.IsConnected;
			}
		}

		public bool OwnsUnit(Unit unit)
		{
			if (unit == null)
			{
				return true;
			}

			EngineASX engine = EngineASX.Instance;

			// No engine means no world to be authoritative about. Answering
			// yes is the same reasoning as everywhere else: two copies that
			// briefly disagree are visible, a world that has stopped is not.
			if (engine == null)
			{
				return true;
			}

			// Exactly one unit: the one the player is flying by hand. Not every
			// unit of their faction, and not their own station - a station in
			// a world with other people in it belongs to the host like
			// everything else, and it is where an earlier version of this was
			// far too generous.
			return unit.IsPlayerCurrentUnit;
		}

		public bool OwnsFaction(Faction faction)
		{
			// No faction at all. A client does not run an economy, a reputation
			// drift, or an expansion search; it is told what those did.
			return false;
		}

		public bool OwnsWorld()
		{
			return false;
		}
	}
}
