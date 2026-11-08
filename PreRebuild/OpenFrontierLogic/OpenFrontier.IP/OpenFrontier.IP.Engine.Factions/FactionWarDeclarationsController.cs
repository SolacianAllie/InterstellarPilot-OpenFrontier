using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionWarDeclarationsController
	{
		public FactionWarDeclaration CreateWarDeclaration(Faction aggressorFaction, Faction defenderFaction, FactionWarMotivation warMotivation)
		{
			FactionWarDeclaration factionWarDeclaration = new FactionWarDeclaration
			{
				Aggressor = aggressorFaction,
				Defender = defenderFaction,
				WarMotivation = warMotivation,
				DefenderFactionsRequestedToJoin = new List<Faction>(),
				AggressorFactionsRequestedToJoin = new List<Faction>(),
				DefenderFactionsJoined = new List<Faction>(),
				AggressorFactionsJoined = new List<Faction>(),
				TimeOfDeclaration = EngineASX.Instance.ScenarioElapsedTime
			};
			if (aggressorFaction.FactionAI != null)
			{
				aggressorFaction.FactionAI.OnRequestFriendsToJoinWar(factionWarDeclaration);
			}
			if (defenderFaction.FactionAI != null)
			{
				defenderFaction.FactionAI.OnRequestFriendsToJoinWar(factionWarDeclaration);
			}
			return factionWarDeclaration;
		}

		public void ApplyWarDeclaration(FactionWarDeclaration factionWarDeclaration)
		{
			ApplyWarDeclarationNeutrality(factionWarDeclaration);
			ApplyWarDeclarationRelations(factionWarDeclaration);
		}

		private void ChangeOpinionForPartyAtWar(Faction faction, Faction otherFaction)
		{
			float baseOpinionChangeForWar = EngineASX.Instance.GameSettings.FactionSettings.FactionWarDeclarationSettings.BaseOpinionChangeForWar;
			baseOpinionChangeForWar -= 0.2f * faction.Aggression;
			faction.ChangeOpinion(otherFaction, baseOpinionChangeForWar);
		}

		private void ApplyWarDeclarationRelations(FactionWarDeclaration factionWarDeclaration)
		{
			FactionWarDeclarationSettings factionWarDeclarationSettings = EngineASX.Instance.GameSettings.FactionSettings.FactionWarDeclarationSettings;
			ChangeOpinionForPartyAtWar(factionWarDeclaration.Defender, factionWarDeclaration.Aggressor);
			ChangeOpinionForPartyAtWar(factionWarDeclaration.Aggressor, factionWarDeclaration.Defender);
			foreach (Faction item in factionWarDeclaration.DefenderFactionsJoined)
			{
				factionWarDeclaration.Defender.ChangeOpinion(item, factionWarDeclarationSettings.OpinionChangeForJoiningFriendInWar);
			}
			foreach (Faction item2 in factionWarDeclaration.AggressorFactionsJoined)
			{
				factionWarDeclaration.Aggressor.ChangeOpinion(item2, factionWarDeclarationSettings.OpinionChangeForJoiningFriendInWar);
			}
			foreach (Faction item3 in factionWarDeclaration.GetDefendersDeclinedToJoin())
			{
				factionWarDeclaration.Defender.ChangeOpinion(item3, factionWarDeclarationSettings.OpinionChangeForDenyingRequestToJoinFriendInWar);
			}
			foreach (Faction item4 in factionWarDeclaration.GetAggressorsDeclinedToJoin())
			{
				factionWarDeclaration.Aggressor.ChangeOpinion(item4, factionWarDeclarationSettings.OpinionChangeForDenyingRequestToJoinFriendInWar);
			}
		}

		private static void ApplyWarDeclarationNeutrality(FactionWarDeclaration factionWarDeclaration)
		{
			SetAtWarTwoWay(factionWarDeclaration.Aggressor, factionWarDeclaration.Defender);
			foreach (Faction item in factionWarDeclaration.DefenderFactionsJoined)
			{
				SetAtWarTwoWay(factionWarDeclaration.Aggressor, item);
				foreach (Faction item2 in factionWarDeclaration.AggressorFactionsJoined)
				{
					item.SetAsHostileTo(item2);
				}
			}
			foreach (Faction item3 in factionWarDeclaration.AggressorFactionsJoined)
			{
				SetAtWarTwoWay(item3, factionWarDeclaration.Defender);
				foreach (Faction item4 in factionWarDeclaration.DefenderFactionsJoined)
				{
					item3.SetAsHostileTo(item4);
				}
			}
		}

		public static void SetAtWarTwoWay(Faction f1, Faction f2)
		{
			f1.SetAsHostileTo(f2);
			f2.SetAsHostileTo(f1);
		}
	}
}
