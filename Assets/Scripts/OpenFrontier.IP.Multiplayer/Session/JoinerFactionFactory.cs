using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Makes a player for somebody who has just joined.
	/// </summary>
	/// <remarks>
	/// Runs on the host, and that is not a detail. The save models exactly one
	/// local player, so a second player cannot be carried inside a snapshot
	/// and appear by being loaded - it has to exist before the snapshot is
	/// taken. Creating it here rather than on the client is what makes the
	/// two copies agree without any reconciliation: the ids come from the
	/// host's own counter, so both worlds number their ships identically.
	///
	/// A client that invented its own faction would break that, and would also
	/// be a hole straight through the whole host-authoritative design - the
	/// host would end up simulating a faction it never made.
	///
	/// Everything here follows the shape the game already uses for a player
	/// faction, taken from how a new game sets one up: instantiate the player
	/// faction prefab, initialise it, name it, then spawn a ship into it.
	///
	/// One shuttle, deliberately. A joiner's first ship is a fixed point: it
	/// costs nothing to agree on, it cannot be a competitive advantage, and it
	/// keeps this down to one unit per joiner until there is a reason for
	/// more.
	/// </remarks>
	public static class JoinerFactionFactory
	{
		/// <summary>
		/// Creates a faction and a shuttle for a joining player.
		/// </summary>
		/// <param name="pilotName">What the joining player wants to be called.</param>
		/// <param name="pilotTitle">Their rank.</param>
		/// <param name="factionName">Their faction's name.</param>
		/// <param name="factionShortName">Their faction's short name.</param>
		/// <param name="ship">The shuttle created, or null if one could not be.</param>
		/// <returns>The faction, or null with a reason.</returns>
		public static Faction Create(EngineASX engine, string playerToken, string pilotName, string pilotTitle,
			string factionName, string factionShortName, out Unit ship, out string error)
		{
			ship = null;
			error = null;

			if (engine == null || engine.World == null)
			{
				error = "There is no world to add a player to.";
				return null;
			}

			if (GameController.Instance.PlayerFactionPrefab == null)
			{
				error = "The game has no player faction prefab.";
				return null;
			}

			// The same primitive a new game uses, so a joiner's faction is not
			// a subtly different kind of thing from the host's own.
			Faction faction = Object.Instantiate(GameController.Instance.PlayerFactionPrefab);

			if (faction == null)
			{
				error = "The player faction prefab could not be created.";
				return null;
			}

			// Set BEFORE Init, which only assigns an id when there is none.
			// After it would be overwritten by the engine's own counter.
			int wanted = PlayerIdScheme.FactionIdFor(playerToken);

			faction.UniqueId = wanted >= 0 && !IsTaken(engine, wanted) ? wanted : -1;

			faction.Init();

			// Init does NOT register a faction with the engine, and the save
			// exporter walks engine.Factions rather than the scene. Without
			// this the faction exists as an object, the shuttle points at it,
			// and neither appears in the snapshot - so the joiner's world
			// arrives with no faction of its own and silently keeps being the
			// host's pilot. Which is exactly what it did.
			engine.RegisterFaction(faction);

			// Says so out loud, because a faction that is not in engine.Factions
			// is not in the save, and the joiner then loads a world without
			// it and quietly stays the host's pilot. That failure is invisible
			// from the joiner's side: it loads successfully, it is just the
			// wrong player.
			Debug.Log("Multiplayer: registered faction " + faction.UniqueId
				+ " (" + factionName + "); the engine now holds "
				+ engine.Factions.Count + " factions");

			// Assigned here rather than left to the prefab's own values, which
			// would be the host's defaults and would name every joiner after
			// the host.
			// Both copies of the game start their player faction with the
			// same default name, so a joiner's faction arrives called
			// "Frontier Corporation" alongside the host's own one called
			// exactly that. Every list then shows two identical entries, or
			// one, and neither can be told apart by anything a player can
			// see - which is what made it look like the faction was never
			// created at all.
			faction.Name = MakeDistinct(factionName, engine);
			faction.ShortName = factionShortName;

			if (!string.IsNullOrWhiteSpace(pilotName))
			{
				Person person = CreateLeader(engine, faction, pilotName, pilotTitle);

				if (person != null)
				{
					faction.LeaderPerson = person;
				}
			}

			ship = CreateShuttle(engine, faction, out string shipError);

			if (ship == null)
			{
				error = shipError;
			}

			return faction;
		}

		/// <summary>
		/// The name, with a suffix if this world already has one like it.
		/// </summary>
		/// <remarks>
		/// Suffix rather than replacement. The joiner asked for that name by
		/// name - it is their own faction setting, and overriding it with
		/// something invented would be worse than an ambiguous list. Two
		/// factions called the same thing is a cosmetic problem; being called
		/// something you did not choose is a different one.
		/// </remarks>
		private static string MakeDistinct(string wanted, EngineASX engine)
		{
			if (string.IsNullOrWhiteSpace(wanted) || engine == null)
			{
				return wanted;
			}

			for (int attempt = 2; attempt < 20; attempt++)
			{
				if (!NameIsTaken(engine, wanted))
				{
					return wanted;
				}

				wanted = wanted + " " + attempt;
			}

			return wanted;
		}

		private static bool IsTaken(EngineASX engine, int factionId)
		{
			return factionId >= 0 && engine != null && engine.ContainsFactionById(factionId);
		}

		private static bool NameIsTaken(EngineASX engine, string name)
		{
			IList<Faction> factions = engine.Factions;

			if (factions == null)
			{
				return false;
			}

			for (int i = 0; i < factions.Count; i++)
			{
				if (factions[i] != null
					&& string.Equals(factions[i].Name, name, System.StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static Person CreateLeader(EngineASX engine, Faction faction, string pilotName, string pilotTitle)
		{
			Person person = UnityObjectHelper.NewGameObject<Person>();

			if (person == null)
			{
				return null;
			}

			person.transform.SetParent(faction.transform, false);
			person.Init();

			person.Faction = faction;
			person.CustomName = pilotName;
			person.CustomShortName = pilotName;

			// Only filled in when blank, matching how the game treats its own
			// default title - a player who chose a rank keeps it.
			if (string.IsNullOrWhiteSpace(person.CustomTitle))
			{
				person.CustomTitle = pilotTitle;
			}

			return person;
		}

		/// <summary>
		/// Puts one shuttle in the faction, in the host's current sector.
		/// </summary>
		private static Unit CreateShuttle(EngineASX engine, Faction faction, out string error)
		{
			error = null;

			UnitClass shuttle = FindShuttle();

			if (shuttle == null)
			{
				error = "The game has no shuttle unit class installed.";
				return null;
			}

			Sector sector = engine.ActiveSector;

			if (sector == null)
			{
				error = "There is no sector to put a shuttle in.";
				return null;
			}

			try
			{
				Unit unit = SaveGameUtilities.CreateUnit(engine, -1, shuttle.UniqueID, sector.UniqueId);

				if (unit == null)
				{
					error = "The shuttle could not be created.";
					return null;
				}

				unit.transform.SetParent(sector.transform, false);
				PlaceWhereTheScenarioWould(sector, unit);
				unit.Faction = faction;

				// From the reserved band rather than the engine's counter, so
				// that a ship a player was given is recognisable by its id
				// alone - in a save, in a log, in a debugger, without a lookup.
				// Only the host allocates, which is what keeps the two copies
				// agreeing without either of them negotiating.
				int id = PlayerIdScheme.NextUnitId();

				if (id < 0)
				{
					error = "There is no player ship id left.";
					return null;
				}

				// Through the engine, not SetUniqueId. The exporter reads the
				// field so the joiner is told the right number, but everything
				// that looks a unit up uses the engine's map, and a unit
				// renumbered without being re-registered is a unit the host
				// cannot find - which is what stopped a player's shuttle moving.
				if (!EngineASX.Instance.ReassignUnitId(unit, id))
				{
					error = "Ship number " + id + " is already in this world.";
					return null;
				}

				// The joiner's shuttle, and the ship the host is flying right
				// now, described on adjacent lines. Both were made by the same
				// code from the same prefabs; if they read differently, the
				// difference is the bug and there is no need to guess which
				// part of it matters.
				Debug.Log("DiagS " + UnitDiagnostics.Describe(unit, "joiner's shuttle"));

				Unit hostShip = engine.LocalPlayer == null
					? null
					: engine.LocalPlayer.Person.CurrentUnit;

				Debug.Log("DiagS " + UnitDiagnostics.Describe(hostShip, "host's own ship"));

				return unit;
			}
			catch (System.Exception e)
			{
				error = "The shuttle could not be created: " + e.Message;
				return null;
			}
		}

	/// <summary>
	/// Puts a new player's ship where a new player's ship goes.
	/// </summary>
	/// <remarks>
	/// A shuttle built by CreateUnit has never been given a position, so it
	/// came out wherever a new GameObject comes out - the origin of whatever
	/// it was parented to - which read as "a random spot in the system".
	///
	/// Every scenario says where a player starts, because every scenario
	/// opens on the player's ship and needs them somewhere in particular: a
	/// dock, a system edge, a scripted first encounter. A joiner dropped
	/// elsewhere is not merely untidy - the opening shot of a story scenario
	/// is framed on that position, so a ship somewhere else means a joiner
	/// watching an empty scene while the story assumes they are already
	/// looking at it.
	///
	/// Asked of the world rather than worked out here, so a scenario with no
	/// spawn point keeps behaving the way it always has.
	/// </remarks>
	private static void PlaceWhereTheScenarioWould(Sector sector, Unit unit)
	{
		if (unit == null || sector == null || EngineASX.Instance == null)
		{
			return;
		}

		// The spawn points are declared on World rather than WorldBase, so a
		// world that is not one simply has none said - which is the same as
		// a scenario with no spawn point, and is left alone.
		World world = EngineASX.Instance.World as World;

		if (world == null)
		{
			return;
		}

		// The same choice a singleplayer game makes: the one the scenario
		// names, or one of its alternatives - preferring this sector.
		UnitComponentHolder holder = world.NewGameSpawnLocation;

		if (holder == null)
		{
			holder = InThisSector(world.NewGameSpawnLocations, sector);
		}

		if (holder == null || holder.Unit == null)
		{
			// Nothing said, so leave it where it is, which is what the engine
			// does for a scenario that has no spawn point.
			return;
		}

		unit.transform.localPosition = holder.transform.localPosition;
		unit.transform.localRotation = holder.transform.localRotation;
	}

	/// <summary>
	/// A spawn point in this sector, if the scenario has one there.
	/// </summary>
	/// <remarks>
	/// Scenarios can offer alternatives in different systems, and a player who
	/// joined to one sector should not appear in another. Exactly this sector
	/// or nothing, because the alternative to a good position is a clearly
	/// wrong one.
	/// </remarks>
	private static UnitComponentHolder InThisSector(List<UnitComponentHolder> options, Sector sector)
	{
		if (options == null)
		{
			return null;
		}

		for (int i = 0; i < options.Count; i++)
		{
			UnitComponentHolder holder = options[i];

			if (holder != null && holder.Unit != null && holder.Unit.Sector == sector)
			{
				return holder;
			}
		}

		return null;
	}


		/// <summary>
		/// The shuttle, found by series name rather than by a hard-coded id.
		/// </summary>
		/// <remarks>
		/// By name because the class ids in this game are authored data, and a
		/// saved numeric id is a trap for the next person who renumbers a
		/// prefab. "Shuttle" is the series every variant of the starting ship
		/// belongs to, so this finds the base ship and its variants alike.
		/// </remarks>
		private static UnitClass FindShuttle()
		{
			UnitClass[] classes = GameController.Instance.LoadedUnitClasses;

			if (classes == null)
			{
				return null;
			}

			UnitClass best = null;

			for (int i = 0; i < classes.Length; i++)
			{
				UnitClass candidate = classes[i];

				if (candidate == null || candidate.UnitType != UnitType.Ship)
				{
					continue;
				}

				if (candidate.UnitSeries == null
					|| !string.Equals(candidate.UnitSeries.Name, ShuttleSeriesName,
						System.StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				// The base class, when there is one, so every joiner gets the
				// same ship rather than a variant chosen by prefab order.
				//
				// The test is on what we already have, not only on what we
				// are looking at. As written it stopped the first time it
				// found a class with a name, which is backwards: naming a
				// variant is the reason to keep looking, so the first named
				// candidate ended up keeping the slot forever and every
				// joiner got Shuttle-A instead of a plain shuttle - with
				// whatever weapons and modules that variant happens to carry.
				bool bestIsBase = best != null && string.IsNullOrEmpty(best.className);
				bool candidateIsBase = string.IsNullOrEmpty(candidate.className);

				if (best == null || (candidateIsBase && !bestIsBase))
				{
					best = candidate;
				}
			}

			// Which class this actually landed on, said out loud. The Shuttle
			// series holds variants that are not all ships, and one that is a
			// station arrives with no weapons and no modules and looks like a
			// component bug, when it is a class-choice one.
			if (best != null)
			{
				bool isBase = string.IsNullOrEmpty(best.className);

				Debug.Log("Multiplayer: joiners start in "
					+ best.GetClassAndSeriesName()
					+ " (class '" + best.className + "', type " + best.UnitType + ")"
					+ (isBase
						? string.Empty
						: " - WARNING: this is a named VARIANT, not the base shuttle."
							+ " Every joiner will get its weapons and modules."));
			}

			return best;
		}

		private const string ShuttleSeriesName = "Shuttle";
	}
}