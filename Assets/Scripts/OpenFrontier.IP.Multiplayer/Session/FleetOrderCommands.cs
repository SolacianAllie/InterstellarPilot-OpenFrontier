using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.FleetOrders;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The bridge between a player pressing a button and the world changing.
	/// </summary>
	/// <remarks>
	/// On a host, nothing here is involved: the button calls OrdersHelper and
	/// the order happens, which is how the game has always worked and how it
	/// must keep working.
	///
	/// On a client, the same call is intercepted before anything is changed,
	/// and an order is sent instead. The host then calls the identical
	/// OrdersHelper method a local click would have called, so "what mine
	/// asteroids does" has one implementation rather than two kept in step by
	/// hand. That is the whole argument for this shape: the alternative is
	/// every order existing twice, and the two copies quietly disagreeing the
	/// first time one of them is changed.
	///
	/// Nothing is applied locally and nothing is predicted locally. A client's
	/// fleet starts moving when the host says it does, which is one round trip
	/// of delay on a thing nobody is steering.
	/// </remarks>
	public static class FleetOrderCommands
	{
		/// <summary>
		/// Installs this as the thing fleet orders are offered to.
		/// </summary>
		/// <remarks>
		/// The orders live in the game assembly, which cannot reference this
		/// one - that would be a circle. So the question is asked in the game's
		/// own words and answered here, through a hook the game assembly owns.
		/// The same shape as the authority question and the shared-session
		/// flag, for the same reason.
		/// </remarks>
		public static void Install()
		{
			FleetOrderIntents.Interceptor = new Interceptor();
		}

		private sealed class Interceptor : IFleetOrderInterceptor
		{
			public bool TryHandle(FleetOrderIntent intent)
			{
				return SendIfClient(intent);
			}
		}

		/// <summary>
		/// Sends the order if this copy is a client. True means the caller has
		/// been handled and must not do the work itself.
		/// </summary>
		public static bool SendIfClient(FleetOrderIntent intent)
		{
			if (!Authority.IsThisCopyAClient)
			{
				return false;
			}

			ClientSession session = Session;

			if (session == null || intent.FleetIds == null || intent.FleetIds.Count == 0)
			{
				// Either not connected, or the player selected nothing.
				// Neither is worth a message, and the first must not leave the
				// player's fleet frozen with nothing to show for it.
				if (session == null)
				{
					Debug.LogWarning("Multiplayer: cannot give an order - not connected."
						+ " Nothing changed.");
				}

				return true;
			}

			FleetOrderCommandMessage message = new FleetOrderCommandMessage
			{
				Kind = intent.Kind,
				FleetIds = intent.FleetIds.ToArray(),
				TargetUnitId = intent.TargetUnitId,
				TargetSectorId = intent.TargetSectorId,
				TargetFleetId = intent.TargetFleetId,
				CargoClassId = intent.CargoClassId,
				TargetX = intent.TargetX,
				TargetY = intent.TargetY,
				TargetZ = intent.TargetZ,
				Parameter01 = intent.Parameter01,
				Stack = intent.Stack,
				Loop = intent.Loop,
				Repeat = intent.Repeat
			};

			try
			{
				session.Send(ProtocolVersion.FleetOrderCommandMessageId, message.ToBytes());

				Debug.Log("Multiplayer: asked the host for " + intent.Kind + " on "
					+ intent.FleetIds.Count + " fleet(s)");

				return true;
			}
			catch (System.Exception e)
			{
				// The order does not happen locally either. A player told it
				// went through and then watched nothing move would file that
				// as a broken game rather than a dropped connection.
				Debug.LogWarning("Multiplayer: could not send the order - "
					+ e.Message + ". Nothing changed.");
				return true;
			}
		}

		private static ClientSession Session =>
			MultiplayerClientService.Instance == null
				? null
				: MultiplayerClientService.Instance.Session;

		/// <summary>Stops every ship in a fleet.</summary>
		/// <remarks>
		/// OrderStop takes a unit, so stopping a fleet means stopping every
		/// ship in it - which is what a player who selected a fleet and
		/// pressed stop meant.
		/// </remarks>
		private static void StopEveryShipIn(Fleet fleet)
		{
			if (fleet == null)
			{
				return;
			}

			// Copy first: stopping a ship takes it out of the fleet's own
			// list, so walking that list while stopping from it would skip
			// every other ship.
			var ships = new List<Unit>();

			foreach (UnitComponentHolder holder in fleet.Ships)
			{
				if (holder != null && holder.gameObject != null)
				{
					Unit ship = holder.GetComponent<Unit>();

					if (ship != null)
					{
						ships.Add(ship);
					}
				}
			}

			for (int i = 0; i < ships.Count; i++)
			{
				OpenFrontier.IP.UI.Screens.OrdersHelper.OrderStop(ships[i]);
			}
		}

		/// <summary>
		/// Decodes, checks, and runs an order a player sent. Called on the host.
		/// </summary>
		/// <remarks>
		/// Lives here rather than beside the dispatcher so that the session
		/// code stays free of engine types. That file is the part of the
		/// protocol that can be reasoned about, and tested, without a game.
		/// </remarks>
		public static void RunFor(RemotePlayer player, byte[] body)
		{
			if (player == null || !player.Admitted)
			{
				return;
			}

			if (!FleetOrderCommandMessage.TryFromBytes(body,
				out FleetOrderCommandMessage command, out string error))
			{
				Debug.LogWarning("Multiplayer: refused an order from "
					+ (player.DisplayName ?? player.Identity) + " - " + error);
				return;
			}

			if (!NamesOnlyTheirOwn(player, command))
			{
				return;
			}

			ExecuteOnHost(command);
		}

		/// <summary>
		/// Whether every fleet named belongs to the player who asked.
		/// </summary>
		/// <remarks>
		/// Not anti-cheat - this game has none, and a client may fly its own
		/// ship wherever it likes. This is that the world state names fleets,
		/// and an order naming somebody else's would either silently do nothing
		/// or do something to a player who did not ask for it. Both are worse
		/// than saying no.
		///
		/// All or nothing rather than the valid ones: a selection is one
		/// action, and doing half of it is worse than not doing it, because
		/// the player cannot see which half happened.
		/// </remarks>
		private static bool NamesOnlyTheirOwn(RemotePlayer player, FleetOrderCommandMessage command)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return false;
			}

			int expected = PlayerIdScheme.FactionIdFor(player.Identity);
			Faction theirs = expected < 0 ? null : engine.GetFactionByid(expected);

			for (int i = 0; i < command.Count; i++)
			{
				Fleet fleet = engine.GetFleetByid(command.FleetIds[i]);

				if (fleet != null && theirs != null && fleet.Faction != theirs)
				{
					Debug.LogWarning("Multiplayer: refused an order from "
						+ (player.DisplayName ?? player.Identity) + " for fleet "
						+ command.FleetIds[i] + ", which is not theirs");
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Runs an order the host has been sent. Called on the host only.
		/// </summary>
		/// <remarks>
		/// Every branch calls an OrdersHelper method. That is deliberate and is
		/// the reason this is short: the host is not reimplementing orders, it
		/// is running them, through exactly the code the host's own UI would
		/// have run.
		///
		/// Refused orders are logged and dropped rather than guessed at. An
		/// order naming a fleet that no longer exists, or an object that has
		/// been destroyed since the player clicked, is a normal thing to
		/// happen in the moment between a button and a message - and quietly
		/// substituting something else would be worse than not acting.
		/// </remarks>
		public static void ExecuteOnHost(FleetOrderCommandMessage command)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				Debug.LogWarning("Multiplayer: an order arrived with no world to run it in.");
				return;
			}

			var fleets = new List<Fleet>();

			for (int i = 0; i < command.Count; i++)
			{
				Fleet fleet = engine.GetFleetByid(command.FleetIds[i]);

				if (fleet == null)
				{
					Debug.LogWarning("Multiplayer: an order named fleet "
						+ command.FleetIds[i] + ", which this world does not have."
						+ " It was not carried out.");
					return;
				}

				fleets.Add(fleet);
			}

			var kind = (FleetOrderKind)command.Kind;
			Sector sector = command.TargetSectorId <= 0
				? null
				: engine.GetSectorById(command.TargetSectorId);
			Unit unit = command.TargetUnitId <= 0
				? null
				: engine.GetUnitByid(command.TargetUnitId);

			for (int i = 0; i < fleets.Count; i++)
			{
				Fleet fleet = fleets[i];

				switch (kind)
				{
					case FleetOrderKind.MoveToSectorTarget:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderMoveToSectorTarget(
							fleet,
							new SectorTarget
							{
								Sector = sector,
								TargetUnit = unit,
								SectorPosition = new Vector3(
									command.TargetX, command.TargetY, command.TargetZ)
							},
							command.Stack);
						break;

					case FleetOrderKind.MoveToSector:
						// No single-fleet form of this one exists; the entry
						// point takes a selection. Refused out loud rather
						// than reaching past the helper into whatever it
						// does internally.
						Debug.LogWarning("Multiplayer: order " + kind
							+ " cannot yet be given by a client.");
						break;

					case FleetOrderKind.DockAtTarget:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderDockAtTarget(
							fleet, unit, command.Stack);
						break;

					case FleetOrderKind.ReturnToBase:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderReturnToBase(
							fleet, command.Stack);
						break;

					case FleetOrderKind.EnterWormhole:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderEnterWormhole(
							fleet, unit == null ? null : unit.GetComponent<Wormhole>(),
							command.Stack);
						break;

					case FleetOrderKind.ClaimUnit:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderClaimUnit(
							fleet, unit, command.Stack);
						break;

					case FleetOrderKind.CollectCargo:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderCollectCargo(
							fleet, unit, command.Stack);
						break;

					case FleetOrderKind.Scavenge:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderScavenge(
							fleet, sector, command.Stack);
						break;

					case FleetOrderKind.ExploreSector:
						OpenFrontier.IP.UI.Screens.OrdersHelper.OrderExploreSector(
							fleet, sector, command.Stack);
						break;

					case FleetOrderKind.Stop:
						// OrderStop takes a unit rather than a fleet, so
						// stopping a fleet means stopping every ship in it -
						// which is what a player who selected a fleet and
						// pressed stop meant.
						StopEveryShipIn(fleet);
						break;

					case FleetOrderKind.ProtectTarget:
					case FleetOrderKind.RepairAtNearest:
					case FleetOrderKind.RepairAt:
					case FleetOrderKind.RearmAtNearest:
					case FleetOrderKind.RearmAt:
					case FleetOrderKind.TransportPassengers:
					case FleetOrderKind.MergeWithFleet:
					case FleetOrderKind.ManualTrade:
					case FleetOrderKind.Patrol:
						// No single-fleet form of these yet. Refused out
						// loud, because a player who pressed one and
						// watched nothing happen would have no way to tell
						// it apart from a dropped connection.
						Debug.LogWarning("Multiplayer: order " + kind
							+ " cannot yet be given by a client, and was not carried out.");
						break;

					default:
						// Refused at the codec, so this is unreachable in
						// practice. Said rather than ignored, because a kind
						// arriving here means two builds disagree about the
						// numbering and that is worth knowing about.
						Debug.LogWarning("Multiplayer: order " + kind
							+ " has no handler on this host, and was not carried out.");
						break;
				}
			}

			Debug.Log("Multiplayer: ran " + kind + " for " + fleets.Count + " fleet(s)");
		}
	}
}
