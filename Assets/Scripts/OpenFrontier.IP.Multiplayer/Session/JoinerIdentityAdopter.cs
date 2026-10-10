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
		/// <returns>A sentence for the player, saying what happened.</returns>
		public static string Adopt(int factionId, int shipId)
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.World == null)
			{
				return "there is no world to join";
			}

			if (factionId < 0)
			{
				// The host had no world to make a faction in. Not something to
				// shout about - the joiner can see for themselves that they
				// are still the host's pilot.
				return "the host never made a player for you";
			}

			Faction mine = FindFaction(factionId);

			if (mine == null)
			{
				return "your faction (" + factionId + ") is not in this world";
			}

			Person person = FindOrMakePerson(engine, mine);
			GamePlayer player = engine.LocalPlayer;

			if (player == null || person == null)
			{
				return "there is no local player to give your faction to";
			}

			player.Person.Faction = mine;
			mine.LeaderPerson = person;

			Unit ship = shipId >= 0 ? engine.GetUnitByid(shipId) : null;

			if (ship != null)
			{
				ship.Faction = mine;
			}

			if (ship == null)
			{
				return "faction " + factionId + " (" + mine.Name + "), but no shuttle found";
			}

			// Sit in it. Changing who owns the shuttle is not the same as
			// flying it: LocalUnit is the local player's CurrentUnit, and
			// after loading a save that is still the ship the save was taken
			// with - the host's. So the joiner kept flying the host's ship and
			// only changed its paint, which is exactly what it looked like.
			player.Person.CurrentUnit = ship;

			return "faction " + factionId + " (" + mine.Name + ") flying shuttle " + shipId;
		}

		private static Faction FindFaction(int factionId)
		{
			// Through the engine's own lookup rather than walking the list,
			// so it is the same answer the rest of the game would give.
			return EngineASX.Instance.GetFactionByid(factionId);
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