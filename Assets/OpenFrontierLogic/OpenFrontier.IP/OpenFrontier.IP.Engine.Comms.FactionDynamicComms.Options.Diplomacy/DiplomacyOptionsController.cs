using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options.Diplomacy
{
	public static class DiplomacyOptionsController
	{
		public static void PopulateOptions(DynamicCommsHandler commsHandler, List<ICommsStageOption> stageOptionCache)
		{
			CommsStageOptionGroup optionGroup = new CommsStageOptionGroup
			{
				DisplayText = "Diplomacy..."
			};
			if (!commsHandler.OwnerFaction.IsHostileTo(EngineASX.Instance.LocalFaction))
			{
				PrepareToDieStageOption item = new PrepareToDieStageOption
				{
					OptionText = "Prepare to die! [Declare War]",
					OptionGroup = optionGroup
				};
				stageOptionCache.Add(item);
			}
			else
			{
				TruceOption item2 = new TruceOption
				{
					OptionText = "Truce",
					OptionGroup = optionGroup
				};
				stageOptionCache.Add(item2);
			}
			if (BountyHelper.GetTotalBountyPlacedOnFactionByFaction(EngineASX.Instance.LocalFaction, commsHandler.OwnerFaction) > 0)
			{
				PayOffBountyOption item3 = new PayOffBountyOption
				{
					OptionText = "Pay off Bounty",
					OptionGroup = optionGroup
				};
				stageOptionCache.Add(item3);
			}
		}
	}
}
