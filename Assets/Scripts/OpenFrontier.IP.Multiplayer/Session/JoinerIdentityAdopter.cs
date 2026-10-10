using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Makes the loaded world about this player rather than the host.
	/// </summary>
	/// <remarks>
	/// A save carries exactly one local player, so a joining client that
	/// loads the host's world comes out of it as the host's pilot - same
	/// faction, same ship, same name. The host knows better, and says so in
	/// the descriptor; this is the client acting on that.
	///
	/// Which faction is "mine" is a purely local fact and deliberately never
	/// enters the save. The host's copy has the joiner's faction as just
	/// another faction, controlled from somewhere else, and the host does not
	/// need a second local player at all. Each machine simply marks its own.
	/// </remarks>
	public static class JoinerIdentityAdopter
	{
		/// <summary>What was adopted, for the log to read.</summary>
		public static string LastResult { get; private set; }

		/// <summary>
		/// Points this copy's local player at the joiner's faction and ship.
		/// </summary>
		/// <returns>True when the world is now about this player.</returns>
		public static bool Adopt(int factionId, int shipId, out string result)
		{
			result = null;
			LastResult = null;

			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.World == null)
			{
				result = "no world loaded";
				return false;
			}

			if (factionId < 0)
			{
				// The host had no world to make a faction in, so there is
				// nothing to become. Not an error to shout about - the joiner
				// can see for themselves that they are still the host's pilot.
				result = "the host did not make a faction";
				return false;
			}

			Faction mine = FindFaction(factionId);

			if (mine == null)
			{
				result = "faction " + factionId + " is not in this world";
				return false;
			}

			Person person = FindOrMakePerson(engine, mine);
			GamePlayer player = engine.LocalPlayer;

			if (player == null || person == null)
			{
				result = "no local player to point at a faction";
				return false;
			}

			player.Person.Faction = mine;
			mine.LeaderPerson = person;

			// The previous player's faction is not ours to automate or keep.
			// It is the host's, and the host is flying it.
			if (engine.LocalFaction != mine)
			{
				// Nothing to undo here on purpose: the host's faction is left
				// exactly as it was found, because the host's copy still owns
				// it and both copies must agree.
			}

			Unit ship = shipId >= 0 ? engine.GetUnitByid(shipId) : null;

			if (ship != null)
			{
				ship.Faction = mine;
			}

			result = ship == null
				? "faction " + factionId + " (" + mine.Name + "), no shuttle found"
				: "faction " + factionId + " (" + mine.Name + ") flying shuttle " + shipId;

			LastResult = result;
			return true;
		}

		private static Faction FindFaction(int factionId)
		{
			IList<Faction> factions = EngineASX.Instance.Factions;

			if (factions == null)
			{
				return null;
			}

			for (int i = 0; i < factions.Count; i++)
			{
				if (factions[i] != null && factions[i].UniqueId == factionId)
				{
					return factions[i];
				}
			}

			return null;
		}

		private static Person FindOrMakePerson(EngineASX engine, Faction faction)
		{
			// The faction was created with a leader, so normally this finds
			// the one the host made. If the leader did not survive the
			// round trip through the save, one is made rather than leaving
			// the player with a faction they have no pilot in.
			Person existing = faction.LeaderPerson;

			if (existing != null)
			{
				return existing;
			}

			Person person = UnityObjectHelper.NewGameObject<Person>();

			if (person == null)
			{
				return null;
			}

			person.transform.SetParent(faction.transform, false);
			person.Init();
			person.Faction = faction;

			return person;
		}
	}
}