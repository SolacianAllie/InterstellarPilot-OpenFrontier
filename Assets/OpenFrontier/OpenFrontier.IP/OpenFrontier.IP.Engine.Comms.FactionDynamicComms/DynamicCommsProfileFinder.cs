using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms
{
	public static class DynamicCommsProfileFinder
	{
		public static DynamicCommsProfile GetProfile(Faction playerFaction, Faction aiFaction, IEnumerable<DynamicCommsProfile> profiles)
		{
			_ = GameController.Instance.GameSettings.DynamicCommsSettings;
			Neutrality dynamicCommsNeutrality = GetDynamicCommsNeutrality(playerFaction, aiFaction);
			DynamicCommsProfileOpinion dynamicCommsOpinion = GetDynamicCommsOpinion(playerFaction, aiFaction);
			return GetBestProfile(profiles, dynamicCommsNeutrality, dynamicCommsOpinion);
		}

		private static DynamicCommsProfile GetBestProfile(IEnumerable<DynamicCommsProfile> profiles, Neutrality neutrality, DynamicCommsProfileOpinion opinion)
		{
			DynamicCommsProfile dynamicCommsProfile = null;
			int num = 0;
			foreach (DynamicCommsProfile profile in profiles)
			{
				int num2 = 0;
				if (profile.Neutrality == neutrality)
				{
					num2 += 20;
				}
				else if (profile.Neutrality == Neutrality.Neutral)
				{
					num2 += 5;
				}
				if (profile.Opinion == opinion)
				{
					num2 += 20;
				}
				else if (profile.Neutrality == Neutrality.Neutral)
				{
					num2 += 5;
				}
				if (dynamicCommsProfile == null || num2 > num)
				{
					num = num2;
					dynamicCommsProfile = profile;
				}
			}
			return dynamicCommsProfile;
		}

		private static DynamicCommsProfileOpinion GetDynamicCommsOpinion(Faction playerFaction, Faction aiFaction)
		{
			float opinion = aiFaction.GetOpinion(playerFaction);
			if (opinion >= 0.5f)
			{
				return DynamicCommsProfileOpinion.Friendly;
			}
			if (opinion <= -0.5f)
			{
				return DynamicCommsProfileOpinion.Unfriendly;
			}
			return DynamicCommsProfileOpinion.Neutral;
		}

		private static Neutrality GetDynamicCommsNeutrality(Faction playerFaction, Faction aiFaction)
		{
			return aiFaction.GetNeutralityWithInternal(playerFaction);
		}
	}
}
