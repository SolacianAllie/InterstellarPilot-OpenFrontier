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
		public static Faction Create(EngineASX engine, string pilotName, string pilotTitle,
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

			faction.UniqueId = -1;
			faction.Init();

			// Assigned here rather than left to the prefab's own values, which
			// would be the host's defaults and would name every joiner after
			// the host.
			faction.Name = factionName;
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
				unit.Faction = faction;

				// -1 above means "give me the next id", which is what makes the
				// host's counter the single source of numbering both copies
				// will agree on.
				unit.SetUniqueId(engine.UnitIdCounter);
				engine.UnitIdCounter++;

				return unit;
			}
			catch (System.Exception e)
			{
				error = "The shuttle could not be created: " + e.Message;
				return null;
			}
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
				if (best == null || string.IsNullOrEmpty(best.className))
				{
					best = candidate;
				}
			}

			return best;
		}

		private const string ShuttleSeriesName = "Shuttle";
	}
}