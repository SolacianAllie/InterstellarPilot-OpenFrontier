using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.PilotRankings;
using UnityEngine;

namespace OpenFrontier.IP.Engine.WorldSeeding.WorldSeedingLayers.PilotRanking
{
	[RequireComponent(typeof(WorldSeederLayer))]
	public class PromotePilotsFromShipSeeder : MonoBehaviour
	{
		public bool IncludePlayerFaction;

		public bool AutoCreateLeaderIfNone = true;

		public void SeedLayer(WorldBase world)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log("Promoting pilots...", this, 1);
			}
			Seed(IncludePlayerFaction, AutoCreateLeaderIfNone, updateDebugInfo: false);
		}

		public static void Seed(bool includePlayerFaction, bool autoCreateLeaderIfNone, bool updateDebugInfo)
		{
			PriorityQueue<Person, float> personCache = new PriorityQueue<Person, float>(100);
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if ((!faction.IsPlayerFaction | includePlayerFaction) && faction.PilotRankingSystem != null && faction.PilotRankingSystem.Ranks.Count > 0)
				{
					if (autoCreateLeaderIfNone && faction.LeaderPerson == null && faction.FactionAI != null)
					{
						faction.FactionAI.TryCreateOrPickLeaderIfNone();
					}
					AssignRanksForFaction(faction, personCache, updateDebugInfo);
				}
			}
		}

		public static void AssignRanksForFaction(Faction faction, bool updateDebugInfo)
		{
			PriorityQueue<Person, float> personCache = new PriorityQueue<Person, float>(100);
			AssignRanksForFaction(faction, personCache, updateDebugInfo);
		}

		private static void AssignRanksForFaction(Faction faction, PriorityQueue<Person, float> personCache, bool updateDebugInfo, bool allowDemotions = false)
		{
			if (faction.PilotRankingSystem.Ranks.Count == 1)
			{
				foreach (Person person in faction.People)
				{
					if (!person.IsAutoPilot && !person.IsLocalPlayer)
					{
						person.AssignFirstPilotRankIfNull();
					}
				}
				return;
			}
			personCache.Clear();
			IEnumerable<Person> enumerable = faction.People.Where((Person e) => !e.IsAutoPilot && !e.IsLocalPlayer);
			int num = enumerable.Count();
			foreach (Person item in enumerable)
			{
				float personPriority = GetPersonPriority(faction, item);
				personCache.Enqueue(item, personPriority);
			}
			for (int num2 = faction.PilotRankingSystem.Ranks.Count - 1; num2 >= 0; num2--)
			{
				PilotRankingSystemRank pilotRankingSystemRank = faction.PilotRankingSystem.Ranks[num2];
				int a = personCache.Count;
				if (pilotRankingSystemRank.RequiredPilotCount > 0)
				{
					a = num / pilotRankingSystemRank.RequiredPilotCount;
				}
				a = Mathf.Max(a, pilotRankingSystemRank.MinNumber);
				a = Mathf.Min(a, pilotRankingSystemRank.MaxNumber);
				int num3 = Mathf.Min(personCache.Count, a);
				for (int num4 = 0; num4 < num3; num4++)
				{
					PriorityQueueItem<Person, float> priorityQueueItem = personCache.Dequeue();
					PilotRank rank = priorityQueueItem.Value.Rank;
					PilotRank rank2 = faction.PilotRankingSystem.Ranks[num2].Rank;
					if (!(rank != rank2) || !((rank == null || rank2.Superiority > rank.Superiority) | allowDemotions))
					{
						continue;
					}
					priorityQueueItem.Value.ChangeRank(rank2);
					if (updateDebugInfo && rank != null)
					{
						if (rank2.Superiority > rank.Superiority)
						{
							EngineASX.Instance.DebugInfo.NumNpcPeoplePromoted++;
						}
						else if (rank2.Superiority < rank.Superiority)
						{
							EngineASX.Instance.DebugInfo.NumNpcPeopleDemoted++;
						}
					}
				}
			}
		}

		private static float GetPersonPriority(Faction faction, Person person)
		{
			if (person == faction.LeaderPerson)
			{
				return float.MaxValue;
			}
			if (person.Rank != null)
			{
				return person.Rank.Superiority;
			}
			if (person.CurrentUnit != null)
			{
				return person.CurrentUnit.UnitClass.RelativeShipSaleCost;
			}
			return 1000f;
		}
	}
}
