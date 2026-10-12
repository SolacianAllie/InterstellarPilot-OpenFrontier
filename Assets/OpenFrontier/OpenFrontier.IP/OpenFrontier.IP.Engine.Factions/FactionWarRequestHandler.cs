using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionWarRequestHandler
	{
		public static bool HandleRequest(Faction requestor, Faction requested, FactionWarDeclaration warDeclaration)
		{
			FactionAttitude orCreateAttitude = requested.GetOrCreateAttitude(requestor);
			if (orCreateAttitude == null)
			{
				return false;
			}
			if (requested == requestor)
			{
				return false;
			}
			if (warDeclaration.AggressorFactionsRequestedToJoin.Contains(requested) && warDeclaration.DefenderFactionsRequestedToJoin.Contains(requested))
			{
				return false;
			}
			if (orCreateAttitude.Opinion < 0f)
			{
				return false;
			}
			float baseScore = GetBaseScore(requested);
			baseScore += requested.Aggression * 5f;
			baseScore += requested.Cooperation * 2f;
			baseScore += Mathf.Pow(orCreateAttitude.Opinion, 2f) * 5f;
			float relativeNetWorthTo = requestor.GetRelativeNetWorthTo(requested);
			if (relativeNetWorthTo > 2f)
			{
				baseScore += Mathf.Clamp(relativeNetWorthTo, 0f, 10f) / 10f * 3f;
			}
			switch (warDeclaration.WarMotivation)
			{
			case FactionWarMotivation.UnderAttack:
				if (requestor == warDeclaration.Aggressor)
				{
					baseScore += 4f;
				}
				break;
			case FactionWarMotivation.NoneSpecified:
			case FactionWarMotivation.Hostility:
				if (requestor == warDeclaration.Defender)
				{
					baseScore += 4f;
				}
				break;
			}
			Faction opposingFaction = ((requestor == warDeclaration.Aggressor) ? warDeclaration.Defender : warDeclaration.Aggressor);
			List<Faction> list = ((requestor == warDeclaration.Aggressor) ? warDeclaration.DefenderFactionsJoined : warDeclaration.AggressorFactionsJoined);
			float requestedPower = requested.GetCachedNetWorth();
			baseScore += GetScoreBasedOnOpposingFaction(requested, requestedPower, opposingFaction);
			foreach (Faction item in list)
			{
				_ = item;
				baseScore += GetScoreBasedOnOpposingFaction(requested, requestedPower, opposingFaction);
			}
			return baseScore > 0f;
		}

		private static float GetBaseScore(Faction requested)
		{
			float num = -10f;
			if (requested.FactionType == FactionType.Outlaw)
			{
				num -= 1.5f;
			}
			return num;
		}

		private static float GetScoreBasedOnOpposingFaction(Faction requested, float requestedPower, Faction opposingFaction)
		{
			if (requestedPower == 0f)
			{
				return 0f;
			}
			FactionAttitude orCreateAttitude = requested.GetOrCreateAttitude(opposingFaction);
			float num = 0f;
			if (requested.IsHostileToOrAlwaysHostileTo(opposingFaction))
			{
				return num + 0.1f;
			}
			switch (orCreateAttitude.Neutrality)
			{
			case Neutrality.Allied:
				return num - 1000f;
			case Neutrality.Neutral:
				if ((opposingFaction.IsCivilian || opposingFaction.Virtue > 0.7f) && requested.Virtue > 0.05f)
				{
					num -= Mathf.Pow(requested.Virtue, 0.8f) * 5f;
				}
				num = ((!(orCreateAttitude.Opinion > 0f)) ? (num + Mathf.Abs(orCreateAttitude.Opinion) * 0.08f) : (num - orCreateAttitude.Opinion * 4f));
				break;
			case Neutrality.Hostile:
				return num + 0.1f;
			}
			if (requested.IsBanditOrOutlaw() && opposingFaction.IsBanditOrOutlaw())
			{
				num -= 0.5f;
			}
			if (requested.Aggression < 1f)
			{
				long cachedNetWorth = opposingFaction.GetCachedNetWorth();
				float num2 = 4f * (1f - requested.Aggression);
				if ((float)cachedNetWorth > 0f)
				{
					float num3 = requestedPower / (float)cachedNetWorth;
					if (num3 < 1f)
					{
						num -= (1f - num3) * num2;
					}
				}
			}
			return num;
		}
	}
}
