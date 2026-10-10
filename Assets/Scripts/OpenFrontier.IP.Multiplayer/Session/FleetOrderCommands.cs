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
			int fleetCount = intent.FleetIds == null ? 0 : intent.FleetIds.Count;
			int looseCount = intent.LooseUnitIds == null ? 0 : intent.LooseUnitIds.Count;

			if (session == null || (fleetCount == 0 && looseCount == 0))
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
				FleetIds = intent.FleetIds == null
					? new int[0]
					: intent.FleetIds.ToArray(),
				LooseUnitIds = intent.LooseUnitIds == null
					? new int[0]
					: intent.LooseUnitIds.ToArray(),
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
					+ fleetCount + " fleet(s) and " + looseCount + " loose ship(es). "
					+ "This copy selected: "
					+ WhatThisCopyThinksItSelected(message.FleetIds, message.LooseUnitIds));

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

		/// <summary>
		/// Says what this copy believes it selected, in the same words the
		/// host will use to check it.
		/// </summary>
		/// <remarks>
		/// Written because the two sides were disagreeing and neither log said
		/// what it thought. The host could see a fleet and say whose it was;
		/// the client could see its own selection and say nothing about it.
		/// So a refusal could be read but never checked.
		///
		/// With the faction each belongs to on THIS machine, because that is
		/// the whole question: whether the two copies agree. If they do not,
		/// the faction named here is not the one the host will find, and that
		/// is the answer.
		/// </remarks>
		private static string WhatThisCopyThinksItSelected(int[] fleetIds, int[] looseUnitIds)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return " (this copy cannot read its own world)";
			}

			System.Text.StringBuilder said = new System.Text.StringBuilder();

			if (fleetIds != null)
			{
				for (int i = 0; i < fleetIds.Length; i++)
				{
					Fleet fleet = engine.GetFleetByid(fleetIds[i]);

					if (fleet == null)
					{
						Said(said, "fleet " + fleetIds[i] + " (this copy has none)");
					}
					else
					{
						Said(said, "fleet " + fleet.UniqueId + " of "
							+ Whose(fleet.Faction) + " holding " + fleet.Ships.Count + " ship(s)");
					}
				}
			}

			if (looseUnitIds != null)
			{
				for (int i = 0; i < looseUnitIds.Length; i++)
				{
					Unit ship = engine.GetUnitByid(looseUnitIds[i]);

					if (ship == null)
					{
						Said(said, "ship " + looseUnitIds[i] + " (this copy has none)");
					}
					else
					{
						Fleet inOne = ship.GetFleet();

						Said(said, "ship " + ship.UniqueId + " of " + Whose(ship.Faction)
							+ ", in fleet " + (inOne == null ? "none" : inOne.UniqueId.ToString()));
					}
				}
			}

			return said.Length == 0 ? "nothing" : said.ToString();
		}

		private static void Said(System.Text.StringBuilder said, string what)
		{
			if (said.Length > 0)
			{
				said.Append("; ");
			}

			said.Append(what);
		}

		private static string Whose(Faction faction)
		{
			if (faction == null)
			{
				return "nobody in particular";
			}

			return "'" + faction.Name + "' (" + faction.UniqueId + ")";
		}

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

			// Loose ships become fleets here, on the world that owns them,
			// through the same call a local click would have made. The copy
			// that sent the order deliberately did not build them: a fleet it
			// built would be an id the host has never issued.
			command = WithFleetsForLooseShips(player, command);

			ExecuteOnHost(command);

			// Give the ship back when this fleet has nothing left to do.
			//
			// The handover is only half of a handover. The client let go of
			// its ship because it was given an order, and will not take it
			// back on its own initiative - it cannot know when the order
			// finished, because the order was never in its copy. Without this
			// the player watches a ship they cannot fly for the rest of the
			// session.
			//
			// Keyed on the fleet rather than on a timer, because
			// AllObjectivesRemoved is what the game itself uses for "this
			// fleet has finished", so the ship comes back when the thing it
			// was given to do is done, and not a moment before.
			ReturnWhenTheFleetIsDone(MultiplayerHostService.Session, command);
		}

		/// <summary>
		/// Hands back every ship of this order that a player was flying.
		/// </summary>
		/// <remarks>
		/// Only ships that really were somebody's to fly when the order
		/// arrived. A ship the host was already flying is left alone, and a
		/// fleet with no remote ship in it is not subscribed to anything -
		/// which is the common case, every time an AI fleet gets an order.
		/// </remarks>
		private static void ReturnWhenTheFleetIsDone(HostSession session, FleetOrderCommandMessage command)
		{
			if (command.FleetIds == null || command.FleetIds.Length == 0)
			{
				return;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return;
			}

			for (int f = 0; f < command.FleetIds.Length; f++)
			{
				Fleet fleet = engine.GetFleetByid(command.FleetIds[f]);

				if (fleet == null)
				{
					continue;
				}

				List<int> ids = new List<int>();
				List<string> owners = new List<string>();

				foreach (UnitComponentHolder holder in fleet.Ships)
				{
					Unit ship = holder == null ? null : holder.GetComponent<Unit>();

					if (ship == null || ship.IsDestroyed)
					{
						continue;
					}

					string owner = session.Ownership.OwnerOf(ship.UniqueId);

					if (string.IsNullOrEmpty(owner))
					{
						continue;
					}

					ids.Add(ship.UniqueId);
					owners.Add(owner);

					// "Autopilot, sorry - I'm taking over."
					//
					// Named as the host's own, which is what an empty owner
					// means everywhere else in this protocol, and is why the
					// client stops simulating it, stops uploading it, and stops
					// asking for it back until this fleet is finished.
					//
					// Said after the order was carried out rather than before,
					// so the client only lets go of a ship the host is really
					// flying. A client that handed over on sending would be
					// stranded if the order were refused, and the player would
					// be watching a ship nobody was flying with no way to take
					// it back.
					session.Ownership.Release(ship.UniqueId);
					session.SendToEveryone(ship.UniqueId, string.Empty);
				}

				if (ids.Count == 0)
				{
					continue;
				}

				Debug.Log("Multiplayer: taking " + ids.Count + " ship(s) off the"
					+ " player to fly them - they were given an order");

				// What the host now has to work with, because a ship that is
				// turned but does not thrust is a ship with a pilot and no
				// engine, and nothing else reports which of those it is.
				for (int i = 0; i < ids.Count; i++)
				{
					Unit ship = engine.GetUnitByid(ids[i]);

					if (ship == null)
					{
						continue;
					}

					NpcPilot pilot = ship.NpcPilot;

					Debug.Log("DiagShip: taking ship " + ship.UniqueId + " ("
						+ (ship.UnitClass == null ? "?" : ship.UnitClass.GetSeriesName())
						+ "): fleet " + fleet.UniqueId
						+ " with " + fleet.Ships.Count + " ship(s)"
						+ ", fleet order " + (fleet.ActiveOrder == null
							? "NONE ACTIVE"
							: fleet.ActiveOrder.FleetOrder.OrderType.ToString())
						+ ", queued " + fleet.OrderQueue.Count
						+ ", ship throttle " + ship.Components.EngineThrottle
						+ ", ship pilot " + (pilot == null ? "NONE" : "yes")
						+ ", nav target " + (fleet.NavTarget.IsActive ? "set" : "CLEAR"));
				}

				List<int> ships = ids;
				List<string> tokens = owners;
				Fleet watched = fleet;

				watched.AllObjectivesRemoved += delegate
				{
					for (int i = 0; i < ships.Count; i++)
					{
						session.Ownership.TryClaim(ships[i], tokens[i], out _);
						session.SendToEveryone(ships[i], tokens[i]);
					}

					Debug.Log("Multiplayer: gave " + ships.Count + " ship(s) back to"
						+ " the player who was flying them - the fleet they were put"
						+ " in has nothing left to do");
				};
			}
		}

		/// <summary>
		/// Turns the ships an order named without fleets into fleets, on the
		/// host.
		/// </summary>
		/// <remarks>
		/// All or nothing, like the ownership check. A selection is one
		/// action, and leaving half of it undone is worse than leaving all of
		/// it - the player sees some of their fleet move and cannot tell that
		/// the rest was refused.
		///
		/// A ship that has acquired a fleet since the order was sent is left
		/// out, because the player selected a ship and the ship is in a fleet;
		/// the fleet was never the point.
		/// </remarks>
		private static FleetOrderCommandMessage WithFleetsForLooseShips(
			RemotePlayer player, FleetOrderCommandMessage command)
		{
			if (command.LooseUnitIds == null || command.LooseUnitIds.Length == 0)
			{
				return command;
			}

			EngineASX engine = EngineASX.Instance;

			if (engine == null)
			{
				return command;
			}

			List<int> fleetIds = new List<int>();

			if (command.FleetIds != null)
			{
				fleetIds.AddRange(command.FleetIds);
			}

			int built = 0;
			int alreadyInOne = 0;

			for (int i = 0; i < command.LooseUnitIds.Length; i++)
			{
				Unit ship = engine.GetUnitByid(command.LooseUnitIds[i]);

				if (ship == null || ship.IsDestroyed)
				{
					Debug.LogWarning("Multiplayer: an order from "
						+ (player.DisplayName ?? player.Identity) + " named ship "
						+ command.LooseUnitIds[i] + ", which this world does not"
						+ " have. Nothing was carried out.");

					return command;
				}

				Fleet existing = ship.GetFleet();

				if (existing != null)
				{
					if (!fleetIds.Contains(existing.UniqueId))
					{
						fleetIds.Add(existing.UniqueId);

						alreadyInOne++;
					}

					continue;
				}

				Fleet fleet = OpenFrontier.IP.UI.Screens.OrdersHelper
					.FindOrCreateNpcAndFleetAndPilotShip(ship);

				if (fleet == null)
				{
					Debug.LogWarning("Multiplayer: could not put ship "
						+ ship.UniqueId + " into a fleet. Nothing was carried out.");

					return command;
				}

				if (!fleetIds.Contains(fleet.UniqueId))
				{
					fleetIds.Add(fleet.UniqueId);

					built++;
				}
			}

			if (built > 0 || alreadyInOne > 0)
			{
				Debug.Log("Multiplayer: put "
					+ (player.DisplayName ?? player.Identity) + "'s order on "
					+ built + " new fleet(s) and " + alreadyInOne
					+ " fleet(s) their loose ships had joined");
			}

			command.FleetIds = fleetIds.ToArray();

			return command;
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

				if (fleet == null)
				{
					// A fleet this world does not have. A client's idea of
					// which ships are in which fleet is a copy, and a copy can
					// disagree - so this is said rather than treated as a
					// refusal, because the order did not ask for anything
					// anybody else owns.
					Debug.LogWarning("Multiplayer: an order from "
						+ (player.DisplayName ?? player.Identity) + " named fleet "
						+ command.FleetIds[i] + ", which this world does not have."
						+ " Nothing was carried out.");
					return false;
				}

				// A fleet is theirs if it holds one of their ships.
				//
				// It used to be checked by faction, which is wrong twice over.
				// A player owns their own ships wherever those ships are, and
				// the two copies of a world do not necessarily agree about
				// which fleet a ship is in - a client that docked created a
				// fleet locally before orders existed, and the host never did.
				// The player is asking about a ship of theirs, so the question
				// is whether the fleet holds a ship of theirs.
				if (theirs != null && fleet.Faction == theirs)
				{
					continue;
				}

				if (HoldsAShipOf(fleet, theirs))
				{
					continue;
				}

				// Said with what it saw. The first version of this said only
				// "not theirs", which is a conclusion and not an observation,
				// and left the next person - or the next run - with nothing to
				// go on.
				Debug.LogWarning("Multiplayer: refused an order from "
					+ (player.DisplayName ?? player.Identity) + " for fleet "
					+ command.FleetIds[i] + ", which holds none of their ships."
					+ " The fleet belongs to "
					+ (fleet.Faction == null
						? "nobody in particular"
						: "'" + fleet.Faction.Name + "' (" + fleet.Faction.UniqueId + ")")
					+ " and theirs is "
					+ (theirs == null ? "missing" : "'" + theirs.Name + "' ("
						+ theirs.UniqueId + ")"));

				// What the player does own, beside what they asked for. The
				// refusal says what was wrong; this says what was available,
				// which is the half that tells you whether the two copies
				// disagree about the world or the player clicked the wrong
				// thing.
				Debug.LogWarning("Multiplayer: their own ships are "
					+ TheirOwnShips(engine, theirs));

				return false;
			}

			// Loose ships are checked on their own account. They were not in
			// a fleet when the order left, so the fleet loop above never saw
			// them, and the one below builds a fleet around whatever it is
			// given.
			for (int i = 0; command.LooseUnitIds != null && i < command.LooseUnitIds.Length; i++)
			{
				Unit ship = engine.GetUnitByid(command.LooseUnitIds[i]);

				if (ship == null || ship.IsDestroyed)
				{
					Debug.LogWarning("Multiplayer: refused an order from "
						+ (player.DisplayName ?? player.Identity) + " for ship "
						+ command.LooseUnitIds[i] + ", which this world does not"
						+ " have. Nothing was carried out.");

					return false;
				}

				if (theirs == null || ship.Faction != theirs)
				{
					Debug.LogWarning("Multiplayer: refused an order from "
						+ (player.DisplayName ?? player.Identity) + " for ship "
						+ ship.UniqueId + ", which belongs to "
						+ (ship.Faction == null
							? "nobody in particular"
							: "'" + ship.Faction.Name + "' (" + ship.Faction.UniqueId + ")")
						+ " and theirs is "
						+ (theirs == null ? "missing" : "'" + theirs.Name + "' ("
							+ theirs.UniqueId + ")"));

					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Every ship of a faction on this machine, and the fleets they are in.
		/// </summary>
		/// <remarks>
		/// The other half of a refusal. A refusal that names only the fleet
		/// that was refused leaves two possibilities indistinguishable - the
		/// player asked for the wrong thing, or the two copies disagree about
		/// the world. This says which: if the player's ship is listed here in
		/// a fleet the client also named, the copies agree and the click was
		/// wrong. If the player's ship is in a fleet nobody named, the client
		/// is looking at a different world.
		/// </remarks>
		private static string TheirOwnShips(EngineASX engine, Faction theirs)
		{
			if (theirs == null || engine == null)
			{
				return "unknown - this world has no faction for them";
			}

			System.Text.StringBuilder said = new System.Text.StringBuilder();
			int counted = 0;

			foreach (Unit ship in engine.UpdatableUnits)
			{
				if (ship == null || ship.IsDestroyed || ship.Faction != theirs)
				{
					continue;
				}

				if (counted >= 12)
				{
					Said(said, "and more");

					break;
				}

				Fleet inOne = ship.GetFleet();

				Said(said, ship.UniqueId + " in "
					+ (inOne == null ? "no fleet" : "fleet " + inOne.UniqueId));

				counted++;
			}

			if (counted == 0)
			{
				return "none - faction '" + theirs.Name + "' (" + theirs.UniqueId
					+ ") has no ships here";
			}

			return said.ToString();
		}

		/// <summary>Whether any ship in a fleet belongs to a faction.</summary>
		private static bool HoldsAShipOf(Fleet fleet, Faction faction)		{
			if (fleet == null || faction == null)
			{
				return false;
			}

			try
			{
				foreach (UnitComponentHolder holder in fleet.Ships)
				{
					if (holder == null)
					{
						continue;
					}

					Unit ship = holder.GetComponent<Unit>();

					if (ship != null && ship.Faction == faction)
					{
						return true;
					}
				}
			}
			catch (System.Exception)
			{
				// Answered no. A fleet that cannot be read is a fleet this
				// cannot prove belongs to the player, and refusing is the
				// direction that is merely unhelpful rather than wrong.
			}

			return false;
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
