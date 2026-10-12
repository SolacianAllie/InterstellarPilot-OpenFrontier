using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionWarFriendRequestor
	{
		public struct ScoredFaction
		{
			public float Score;

			public Faction Faction;
		}

		public static IEnumerable<Faction> MakeRequests(Faction faction, FactionWarDeclaration warDeclaration)
		{
			List<ScoredFaction> list = new List<ScoredFaction>();
			FactionWarDeclarationSettings factionWarDeclarationSettings = EngineASX.Instance.GameSettings.FactionSettings.FactionWarDeclarationSettings;
			foreach (FactionAttitude relation in faction.Relations)
			{
				if (relation.TargetFaction != warDeclaration.Aggressor && relation.TargetFaction != warDeclaration.Defender && relation.GetAge() > factionWarDeclarationSettings.MinFactionRelationshipAgeToJoinWar && !Faction.IsEitherFactionHostileTo(faction, relation.TargetFaction))
				{
					ScoredFaction? scoredFaction = ScoreRequest(faction, relation, factionWarDeclarationSettings);
					if (scoredFaction.HasValue)
					{
						list.Add(scoredFaction.Value);
					}
				}
			}
			return from e in list.OrderByDescending((ScoredFaction e) => e.Score).Take(factionWarDeclarationSettings.MaxFriendsAskedToJoinWar)
				select e.Faction;
		}

		public static ScoredFaction? ScoreRequest(Faction faction, FactionAttitude relation, FactionWarDeclarationSettings settings)
		{
			if (relation.Opinion > settings.RequestFriendMinOpinion)
			{
				float num = 0f;
				num += relation.Opinion * settings.RequestFriendRelationshipFactor;
				num *= GetScoreFromFactionType(relation.TargetFaction.FactionType);
				num *= faction.Cooperation;
				if (Random.value < num)
				{
					return new ScoredFaction
					{
						Faction = relation.TargetFaction,
						Score = num
					};
				}
			}
			return null;
		}

		private static float GetScoreFromFactionType(FactionType factionType)
		{
			switch (factionType)
			{
			case FactionType.PassengerTransport:
			case FactionType.Explorer:
			case FactionType.Bar:
				return 0.1f;
			case FactionType.Trader:
			case FactionType.Miner:
				return 0.6f;
			case FactionType.Empire:
				return 1.2f;
			default:
				return 1f;
			}
		}
	}
}
