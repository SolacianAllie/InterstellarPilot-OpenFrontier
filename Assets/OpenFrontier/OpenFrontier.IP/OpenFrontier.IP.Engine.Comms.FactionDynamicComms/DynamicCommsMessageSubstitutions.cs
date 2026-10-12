using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public static class DynamicCommsMessageSubstitutions
	{
		public static string Substitute(string message, Person playerPilot, Faction aiFaction)
		{
			DynamicCommsSubstitutionSettings dynamicCommsSubstitutionSettings = GameController.Instance.GameSettings.DynamicCommsSettings.DynamicCommsSubstitutionSettings;
			string newValue = "We";
			if (aiFaction.AISettings != null && aiFaction.AISettings.PreferSingleShip)
			{
				newValue = "I";
			}
			message = message.Replace(dynamicCommsSubstitutionSettings.WeOrI, newValue);
			message = message.Replace(dynamicCommsSubstitutionSettings.PlayerName, playerPilot.GetNameAndTitleOrFullRank());
			message = message.Replace(dynamicCommsSubstitutionSettings.PlayerTitle, playerPilot.Title);
			return message;
		}
	}
}
