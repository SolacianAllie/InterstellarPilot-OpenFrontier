using System.Collections.Generic;
using System.Text;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Multiplayer.UI;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Tells each client what its own faction owns, whenever that changes.
	///
	/// Runs on the host only. Without this a client shows a balance it has
	/// already spent and a hold that has already been emptied, which is the
	/// exact point of having a client: it renders the host's arithmetic and
	/// must be told what the arithmetic came to.
	///
	/// Detected by comparing against what was last sent rather than by
	/// listening for change events, because every event-based version of this
	/// is one that silently misses a code path - a purchase made through a
	/// dialog nobody hooked, a cargo transfer that bypassed the bay, a
	/// balance edited by a mission. A missed event is invisible: the client is
	/// simply wrong, and there is nothing in the log to say why. Comparing is
	/// a few integer comparisons per player per frame, which is not a cost,
	/// and it cannot be wrong in that particular way.
	///
	/// It is still a push, not a poll: the moment the number differs it goes
	/// out in that frame, rather than waiting for a tick.
	/// </summary>
	public sealed class FactionStatePusher : MonoBehaviour
	{
		/// <summary>The name of the object this installs itself on.</summary>
		public const string ObjectName = "Multiplayer Faction State Pusher";

		/// <summary>What was last sent to one player.</summary>
		private sealed class Sent
		{
			public int Credits = int.MinValue;

			/// <summary>Fingerprint of the hold, as "classId:quantity" pairs.</summary>
			public string Cargo;

			public int ShipId = int.MinValue;
		}

		private static readonly Dictionary<string, Sent> sent = new Dictionary<string, Sent>();
		private static readonly StringBuilder fingerprint = new StringBuilder();

		/// <summary>Installs the pusher if it is not already there.</summary>
		public static void EnsureInstalled()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			host.AddComponent<FactionStatePusher>();
		}

		public static FactionStatePusher Instance { get; private set; }

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		private void Update()
		{
			HostSession session = MultiplayerHostService.Session;

			if (session == null || session.Players.Count == 0)
			{
				return;
			}

			for (int p = 0; p < session.Players.Count; p++)
			{
				RemotePlayer player = session.Players[p];

				if (player == null || !player.Admitted || !player.Ready)
				{
					continue;
				}

				string token = player.Identity ?? string.Empty;

				Faction faction = FindFaction(token);
				Unit ship = FindShip(token);

				if (faction == null)
				{
					continue;
				}

				string cargo = DescribeCargo(ship);
				int shipId = ship == null ? -1 : ship.UniqueId;

				if (!sent.TryGetValue(token, out Sent previous))
				{
					previous = new Sent();
					sent[token] = previous;
				}

				// Compared, not scheduled. See the note on the class.
				if (previous.Credits == faction.Credits
					&& previous.ShipId == shipId
					&& string.Equals(previous.Cargo, cargo, System.StringComparison.Ordinal))
				{
					continue;
				}

				previous.Credits = faction.Credits;
				previous.ShipId = shipId;
				previous.Cargo = cargo;

				Send(player, faction, ship, shipId, cargo);
			}
		}

		private static void Send(
			RemotePlayer player, Faction faction, Unit ship, int shipId, string fingerprint)
		{
			List<CargoStack> stacks = new List<CargoStack>();

			CargoBayComponent bay = ship == null ? null : ship.CargoBayComponent;

			if (bay != null)
			{
				foreach (KeyValuePair<CargoClass, int> entry in bay.Cargos)
				{
					if (entry.Key == null || entry.Value <= 0)
					{
						continue;
					}

					stacks.Add(new CargoStack
					{
						ClassId = entry.Key.UniqueId,
						Quantity = entry.Value
					});
				}
			}

			FactionStateMessage message = new FactionStateMessage
			{
				FactionId = faction.UniqueId,
				Credits = faction.Credits,
				ShipId = shipId,
				Cargo = stacks.ToArray()
			};

			try
			{
				player.Channel.Send(
					FramedMessage.Create(ProtocolVersion.FactionStateMessageId, message.ToBytes()));

				NetLog.Host("faction " + faction.UniqueId + " for " + player.Identity
					+ ": " + faction.Credits + " credits, " + stacks.Count + " cargo stack(s)");
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("Multiplayer: could not send faction state to "
					+ (player.Identity ?? "?") + " - " + e.Message);
			}
		}

		/// <summary>
		/// The hold as one comparable string, or an empty one if there is no
		/// ship to hold anything.
		/// </summary>
		private static string DescribeCargo(Unit ship)
		{
			fingerprint.Length = 0;

			CargoBayComponent bay = ship == null ? null : ship.CargoBayComponent;

			if (bay == null)
			{
				return string.Empty;
			}

			foreach (KeyValuePair<CargoClass, int> entry in bay.Cargos)
			{
				if (entry.Key == null)
				{
					continue;
				}

				fingerprint.Append(entry.Key.UniqueId);
				fingerprint.Append(':');
				fingerprint.Append(entry.Value);
				fingerprint.Append(',');
			}

			return fingerprint.ToString();
		}

		/// <summary>
		/// This player's faction, derived from their token.
		/// </summary>
		/// <remarks>
		/// Derived rather than stored, which is the whole reason a player's
		/// faction number is a hash of who they are: the host can work out
		/// whose faction this is without having been told, and a client can
		/// check that the faction it was given is the one it should have
		/// arrived in. A registry of who owns what would be a second source
		/// of truth that could disagree with the ids.
		/// </remarks>
		private static Faction FindFaction(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return null;
			}

			int factionId = PlayerIdScheme.FactionIdFor(token);

			return factionId < 0 ? null : EngineASX.Instance?.GetFactionByid(factionId);
		}

		/// <summary>
		/// This player's ship: the one they hold rather than the host.
		/// </summary>
		/// <remarks>
		/// Asked of the ownership registry rather than of the faction, because
		/// a player owns exactly one ship and a faction owns whatever it has
		/// built. When a player owns more than one - a second ship bought from
		/// a shipyard - the lowest id wins, and it is stated here rather than
		/// left to chance: the client flies one ship at a time and the other
		/// is scenery for the purposes of this message.
		/// </remarks>
		private static Unit FindShip(string token)
		{
			UnitOwnershipRegistry ownership = MultiplayerHostService.Session == null
				? null
				: MultiplayerHostService.Session.Ownership;

			if (ownership == null || string.IsNullOrEmpty(token))
			{
				return null;
			}

			foreach (int unitId in ownership.UnitsOwnedBy(token))
			{
				Unit unit = EngineASX.Instance?.GetUnitByid(unitId);

				if (unit != null)
				{
					return unit;
				}
			}

			return null;
		}
	}
}