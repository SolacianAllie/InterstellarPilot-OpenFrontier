using System.Linq;
using System.Text;
using OpenFrontier.IP.Engine.Dialog;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionWarDeclarationMessageGenerator
	{
		public static PlayerActiveMessage GenerateForWarDeclaredOnPlayer(FactionWarDeclaration warDeclaration)
		{
			return new PlayerActiveMessage
			{
				FromText = "Computer",
				ToText = "#player#",
				MessageText = GenerateMessageTextForWarDeclaredOnPlayer(warDeclaration),
				SubjectText = "War declared against you by " + warDeclaration.Aggressor.GetShortNameElseLong() + (warDeclaration.AggressorFactionsJoined.Any() ? " and others" : string.Empty),
				AllowDelete = true
			};
		}

		public static PlayerActiveMessage GenerateForWarDeclaredBetweenNpcs(FactionWarDeclaration warDeclaration)
		{
			return new PlayerActiveMessage
			{
				FromText = "Computer",
				ToText = "#player#",
				MessageText = GenerateMessageTextForWarDeclaredBetweenNpcs(warDeclaration),
				SubjectText = warDeclaration.Aggressor.GetLongNameElseShort() + " declares war on " + warDeclaration.Defender.GetLongNameElseShort(),
				AllowDelete = true
			};
		}

		private static string GenerateMessageTextForWarDeclaredOnPlayer(FactionWarDeclaration warDeclaration)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (warDeclaration.Aggressor.IsFreelancer)
			{
				stringBuilder.AppendLine(warDeclaration.Aggressor.Name + " (" + warDeclaration.Aggressor.GetDescriptiveFactionName(shortName: false) + ") has declared war on you.");
			}
			else
			{
				stringBuilder.AppendLine("Faction \"" + warDeclaration.Aggressor.Name + "\" has declared war on you.");
			}
			stringBuilder.AppendLine();
			if (warDeclaration.AggressorFactionsJoined.Any())
			{
				stringBuilder.AppendLine("The following factions have also decided to join the war:");
				foreach (Faction item in warDeclaration.AggressorFactionsJoined.OrderByDescending((Faction e) => e.GetCachedNetWorth()))
				{
					stringBuilder.AppendLine("\t" + item.GetDescriptiveFactionNameIncludingPilotName(shortName: false));
				}
				stringBuilder.AppendLine();
			}
			string text = GeneratePersonalMessageForPlayerFromAggressor(warDeclaration);
			if (text != null && warDeclaration.Aggressor.LeaderPerson != null)
			{
				stringBuilder.Append("Message received from " + warDeclaration.Aggressor.LeaderPerson.GetNameAndRank() + ":");
				if (!warDeclaration.Aggressor.IsFreelancer)
				{
					stringBuilder.Append(" (leader of " + warDeclaration.Aggressor.GetShortNameElseLong() + ")");
				}
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("\"" + text + "\"");
			}
			return stringBuilder.ToString();
		}

		private static string GenerateMessageTextForWarDeclaredBetweenNpcs(FactionWarDeclaration warDeclaration)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("War has been declared by " + warDeclaration.Aggressor.GetLongNameElseShort() + " on " + warDeclaration.Defender.GetLongNameElseShort());
			stringBuilder.AppendLine();
			if (warDeclaration.AggressorFactionsJoined.Any())
			{
				stringBuilder.AppendLine("The following factions have also decided to join " + warDeclaration.Aggressor.GetLongNameElseShort() + " in the war:");
				foreach (Faction item in warDeclaration.AggressorFactionsJoined.OrderByDescending((Faction e) => e.GetCachedNetWorth()))
				{
					stringBuilder.AppendLine("\t" + item.GetDescriptiveFactionNameIncludingPilotName(shortName: false));
				}
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}

		private static string GeneratePersonalMessageForPlayerFromAggressor(FactionWarDeclaration warDeclaration)
		{
			return EngineASX.Instance.AdvDialogController.GetMessaage(AdvDialogType.WarDeclaration, warDeclaration.Aggressor, warDeclaration.Aggressor.LeaderPerson, warDeclaration.Aggressor.GetOpinion(EngineASX.Instance.LocalFaction));
		}

		public static void GenerateAndSendForWarDeclaredOnPlayer(FactionWarDeclaration warDeclaration)
		{
			PlayerActiveMessage message = GenerateForWarDeclaredOnPlayer(warDeclaration);
			EngineASX.Instance.LocalPlayer.AddMessage(message, notifications: true, important: true);
		}

		public static void GenerateAndSendForWarDeclaredBetweenNpcs(FactionWarDeclaration warDeclaration)
		{
			PlayerActiveMessage message = GenerateForWarDeclaredBetweenNpcs(warDeclaration);
			EngineASX.Instance.LocalPlayer.AddMessage(message);
		}
	}
}
