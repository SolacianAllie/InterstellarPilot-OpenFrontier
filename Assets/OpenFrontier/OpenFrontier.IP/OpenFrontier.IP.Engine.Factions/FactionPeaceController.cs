using System.Text;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionPeaceController
	{
		private static float GetOpinionChangeOnMakePeace(Faction faction)
		{
			return Mathf.Lerp(EngineASX.Instance.GameSettings.FactionSettings.MaxOpinionChangeOnMakePeace, EngineASX.Instance.GameSettings.FactionSettings.MinOpinionChangeOnMakePeace, faction.Aggression);
		}

		public static void EnforcePeace(FactionAIBase factionAI, FactionAttitude attitude)
		{
			Faction targetFaction = attitude.TargetFaction;
			factionAI.Faction.SetAsNeutralWith(targetFaction);
			targetFaction.SetAsNeutralWith(factionAI.Faction);
			factionAI.Faction.RemoveRecentDamageFrom(targetFaction);
			targetFaction.RemoveRecentDamageFrom(factionAI.Faction);
			float opinion = attitude.Opinion;
			attitude.SetOpinionAndRecordTimeOfChange(opinion + GetOpinionChangeOnMakePeace(factionAI.Faction));
			FactionAttitude orCreateAttitude = targetFaction.GetOrCreateAttitude(factionAI.Faction);
			orCreateAttitude.SetOpinionAndRecordTimeOfChange(orCreateAttitude.Opinion + GetOpinionChangeOnMakePeace(targetFaction));
			if (factionAI.Faction.People.Count > 0 && targetFaction.People.Count > 0 && !targetFaction.IsPlayerFaction && ShouldShowPlayerNotificationForNpcPeace(factionAI.Faction, targetFaction))
			{
				PlayerActiveMessage message = GenerateForPeaceDeclaredBetweenNpcs(factionAI.Faction, targetFaction);
				EngineASX.Instance.LocalPlayer.AddMessage(message);
			}
		}

		public static PlayerActiveMessage GenerateForPeaceDeclaredBetweenNpcs(Faction faction1, Faction faction2)
		{
			return new PlayerActiveMessage
			{
				FromText = "Computer",
				ToText = "#player#",
				MessageText = GenerateMessageTextForPeaceDeclaredBetweenNpcs(faction1, faction2),
				SubjectText = "Peace between " + faction1.GetLongNameElseShort() + " and " + faction2.GetLongNameElseShort(),
				AllowDelete = true
			};
		}

		public static bool ShouldShowPlayerNotificationForNpcPeace(Faction faction1, Faction faction2)
		{
			if (faction1.IsFreelancer || faction2.IsFreelancer)
			{
				return false;
			}
			if (!faction1.IsAIFactionType || !faction2.IsAIFactionType)
			{
				return false;
			}
			if (faction1.IsBanditOrOutlaw() || faction2.IsBanditOrOutlaw())
			{
				return false;
			}
			if (!EngineASX.Instance.LocalFaction.HasAttitudeToFaction(faction1) || !EngineASX.Instance.LocalFaction.HasAttitudeToFaction(faction2))
			{
				return false;
			}
			return true;
		}

		private static string GenerateMessageTextForPeaceDeclaredBetweenNpcs(Faction faction1, Faction faction2)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Peace treaty has been signed by " + faction1.GetLongNameElseShort() + " and " + faction2.GetLongNameElseShort() + ". All hostilities between the parties have ceased.");
			stringBuilder.AppendLine();
			return stringBuilder.ToString();
		}
	}
}
