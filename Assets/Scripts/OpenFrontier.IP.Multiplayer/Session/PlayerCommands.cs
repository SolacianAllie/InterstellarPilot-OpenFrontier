using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The general channel: a client asks, the host decides, everybody is told.
	/// </summary>
	/// <remarks>
	/// The same shape as FleetOrderCommands, for the forty-odd player actions
	/// that are not fleet orders. A UI helper either mutates locally - which is
	/// what happens on a host and in singleplayer - or sends a command and
	/// returns, and the host runs the identical code on the authoritative
	/// copy. So the two copies cannot disagree about what "dock" means, which
	/// is the only thing that makes a pair of them trustworthy.
	///
	/// Every handler validates before it mutates. A command names ids, and ids
	/// are the one thing a client fully controls: "dock my ship" is a request
	/// about a ship, and nothing about the request itself says the ship is
	/// theirs. So each one asks the ownership registry, which is the only
	/// thing that knows.
	///
	/// Ships are named and never fleet ids, for the reason orders are: a
	/// client may build a fleet in its own world and the host has never issued
	/// that id, so an action about a fleet names the ships in it.
	/// </remarks>
	public static class PlayerCommands
	{
		/// <summary>Dock a ship at a station or carrier.</summary>
		public const byte DockShip = 1;

		/// <summary>Take a ship back out of a hangar.</summary>
		public const byte UndockShip = 2;

		private const int ArgShipId = 0;
		private const int ArgDockUnitId = 1;

		private sealed class Entry
		{
			public string Name;
			public Action<RemotePlayer, PlayerCommandMessage> Run;
		}

		private static readonly Dictionary<byte, Entry> Handlers = new Dictionary<byte, Entry>();

		public static void Install()
		{
			Handlers.Clear();
			Register(DockShip, "dock", Dock);
			Register(UndockShip, "undock", Undock);

			// The engine's door. Left installed on a host as well as a client:
			// SendIfClient answers false when this copy is not a client, so a
			// host's button docks the ship itself, which is what it always did.
			Authority.DockRequest = (shipId, dockUnitId) =>
				SendIfClient(DockShip, new[] { shipId, dockUnitId });

			Authority.UndockRequest = shipId =>
				SendIfClient(UndockShip, new[] { shipId });

			if (MultiplayerHostService.Session != null)
			{
				MultiplayerHostService.Session.PlayerCommandRequested -= RunFor;
				MultiplayerHostService.Session.PlayerCommandRequested += RunFor;
			}
		}

		private static void Register(byte commandId, string name, Action<RemotePlayer, PlayerCommandMessage> run)
		{
			Handlers[commandId] = new Entry { Name = name, Run = run };
		}

		/// <summary>
		/// Asks the host to do something. True means this copy has been dealt
		/// with and must not do it itself.
		/// </summary>
		public static bool SendIfClient(byte commandId, int[] intArgs, string[] textArgs = null)
		{
			if (!Authority.IsThisCopyAClient)
			{
				return false;
			}

			ClientSession session = MultiplayerClientService.Instance == null
				? null
				: MultiplayerClientService.Instance.Session;

			if (session == null)
			{
				Debug.LogWarning("Multiplayer: cannot ask the host to do that -"
					+ " not connected. Nothing changed.");

				return true;
			}

			PlayerCommandMessage message = PlayerCommandMessage.Of(commandId, intArgs, textArgs);

			try
			{
				session.Send(ProtocolVersion.PlayerCommandMessageId, message.ToBytes());

				Debug.Log("Multiplayer: asked the host to " + NameOf(commandId)
					+ " ship " + message.IntAt(ArgShipId)
					+ " at " + message.IntAt(ArgDockUnitId));

				return true;
			}
			catch (Exception e)
			{
				Debug.LogWarning("Multiplayer: could not send the request - "
					+ e.Message + ". Nothing changed.");

				return true;
			}
		}

		private static string NameOf(byte commandId)
		{
			return Handlers.TryGetValue(commandId, out Entry entry) ? entry.Name : "unknown";
		}

		/// <summary>The host's end of the channel.</summary>
		public static void RunFor(RemotePlayer player, byte[] body)
		{
			if (!PlayerCommandMessage.TryFromBytes(body, out PlayerCommandMessage command, out string error))
			{
				Debug.LogWarning("Multiplayer: refused a request from "
					+ (player.DisplayName ?? player.Identity) + " - " + error);

				return;
			}

			if (!Handlers.TryGetValue(command.CommandId, out Entry entry))
			{
				// Said out loud, because two builds disagreeing about which
				// commands exist is exactly the thing that is otherwise
				// invisible: the player pressed something, nothing happened,
				// and no amount of reading the other side's log would show why.
				Debug.LogWarning("Multiplayer: " + (player.DisplayName ?? player.Identity)
					+ " asked for command " + command.CommandId
					+ ", which this host has never heard of. It was not carried out."
					+ " (This host knows: "
					+ string.Join(", ", NamesOfEverything()) + ")");

				return;
			}

			try
			{
				entry.Run(player, command);
			}
			catch (Exception e)
			{
				// A handler that throws must not take the session with it, and
				// must not leave the player believing it worked.
				Debug.LogWarning("Multiplayer: " + entry.Name + " failed for "
					+ (player.DisplayName ?? player.Identity) + " - " + e.Message
					+ ". Nothing changed.");
			}
		}

		private static IEnumerable<string> NamesOfEverything()
		{
			foreach (KeyValuePair<byte, Entry> pair in Handlers)
			{
				yield return pair.Key + "=" + pair.Value.Name;
			}
		}

		/// <summary>
		/// Finds a ship this player is actually allowed to move.
		/// </summary>
		/// <remarks>
		/// The same question MayBeClaimed answers, asked again rather than
		/// reached through, because a command that named somebody else's ship
		/// would be a player flying a ship they do not have - and the registry
		/// is the only thing that knows whose it is.
		/// </remarks>
		private static Unit TheirShip(RemotePlayer player, PlayerCommandMessage command)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return null;
			}

			int shipId = command.IntAt(ArgShipId);
			Unit ship = engine.GetUnitByid(shipId);

			if (ship == null)
			{
				Debug.LogWarning("Multiplayer: " + (player.DisplayName ?? player.Identity)
					+ " asked about ship " + shipId + ", which this world does not have.");

				return null;
			}

			if (!HostShipUpload.MayBeClaimed(shipId, player.Identity))
			{
				Debug.LogWarning("Multiplayer: refused "
					+ (player.DisplayName ?? player.Identity) + " - ship " + shipId
					+ " is not theirs to move.");

				return null;
			}

			return ship;
		}

		private static void Dock(RemotePlayer player, PlayerCommandMessage command)
		{
			HostSession session = MultiplayerHostService.Session;

			Unit ship = TheirShip(player, command);

			if (ship == null || session == null)
			{
				return;
			}

			int dockId = command.IntAt(ArgDockUnitId);
			Unit dock = EngineASX.Instance.GetUnitByid(dockId);

			if (dock == null)
			{
				Debug.LogWarning("Multiplayer: refused docking ship " + ship.UniqueId
					+ " - there is no unit " + dockId + " to dock at.");

				return;
			}

			if (!ship.Components.TryDockInUnit(dock))
			{
				// Told to everybody, not just the player who asked. A dock that
				// failed on the host must not linger in the requester's copy,
				// and the only way to be sure it does not is to say so
				// everywhere.
				Debug.LogWarning("Multiplayer: could not dock ship " + ship.UniqueId
					+ " at " + dockId + " - it does not fit, or the hangar is full.");

				session.Broadcast(ProtocolVersion.DockedStateMessageId,
					DockedStateMessage.Undocked(ship.UniqueId).ToBytes());

				return;
			}

			UnitHangarBay bay = ship.Components.DockedInHangarBay;

			Debug.Log("Multiplayer: docked ship " + ship.UniqueId + " at station "
				+ dockId + ", bay " + (bay == null ? -1 : bay.BayId));

			// The bay is named as well as the dock, because both copies pick
			// their own bay when a dock happens and there is no reason to
			// believe they would pick the same one.
			session.Broadcast(ProtocolVersion.DockedStateMessageId,
				DockedStateMessage.Docked(ship.UniqueId, dockId,
					bay == null ? -1 : bay.BayId).ToBytes());
		}

		private static void Undock(RemotePlayer player, PlayerCommandMessage command)
		{
			HostSession session = MultiplayerHostService.Session;

			Unit ship = TheirShip(player, command);

			if (ship == null || session == null)
			{
				return;
			}

			if (!ship.Components.UndockIfDocked())
			{
				// Nothing to undo. Said, because the player pressed something
				// and is owed an answer either way.
				Debug.LogWarning("Multiplayer: ship " + ship.UniqueId
					+ " is not docked, so there was nothing to undock.");

				session.Broadcast(ProtocolVersion.DockedStateMessageId,
					DockedStateMessage.Undocked(ship.UniqueId).ToBytes());

				return;
			}

			Debug.Log("Multiplayer: undocked ship " + ship.UniqueId);

			session.Broadcast(ProtocolVersion.DockedStateMessageId,
				DockedStateMessage.Undocked(ship.UniqueId).ToBytes());
		}
	}
}