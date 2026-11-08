using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class TruceHelper
	{
		public static void PlayerMakeTruceWithCreditsExchange(Faction playerFaction, Person playerPilot, Faction otherFaction, int costToPlayer)
		{
			EngineASX.Instance.AddCreditsToPlayerFactionWithMsg(-costToPlayer, FactionTransactionType.Tribute, otherFaction);
			otherFaction.ApplyTransaction(costToPlayer, FactionTransactionType.Tribute, playerFaction);
			PlayerMakeTruce(playerFaction, playerPilot, otherFaction);
		}

		public static void PlayerMakeTruce(Faction playerFaction, Person playerPilot, Faction otherFaction)
		{
			if (playerPilot != null)
			{
				BountyHelper.ReleaseBountiesOnPersonPlacedByFaction(playerPilot, otherFaction);
			}
			Faction.MakePeace(playerFaction, otherFaction);
		}

		public static int CalculateTruceCost(Faction requestor, FactionAIBase factionAI)
		{
			int num = 500;
			float num2 = Mathf.Min(requestor.GetCachedNetWorth(), factionAI.Faction.GetCachedNetWorth());
			if (num2 > 0f)
			{
				float opinion = factionAI.Faction.GetOpinion(requestor);
				float num3 = 1f - opinion;
				float num4 = Mathf.Pow(10f, 6f);
				float num5 = Mathf.Pow(10f, 6f) * 30f;
				num += Maths.RoundToInt(Mathf.Clamp01(num2 / num5) * num4 * num3, 500);
			}
			return num;
		}
	}
}
