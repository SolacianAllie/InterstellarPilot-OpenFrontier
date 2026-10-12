using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Comms.FactionDynamicComms.Options
{
	public class PrepareToDieStageOption : DynamicCommsStageOption
	{
		public override void Select(ICommsController commsController)
		{
			base.Select(commsController);
			Faction ownerFaction = commsController.CommsStage.Handler.OwnerFaction;
			if (ownerFaction != null)
			{
				FactionWarDeclarationsController factionWarDeclarationsController = new FactionWarDeclarationsController();
				FactionWarDeclaration factionWarDeclaration = factionWarDeclarationsController.CreateWarDeclaration(EngineASX.Instance.LocalFaction, ownerFaction, FactionWarMotivation.NoneSpecified);
				factionWarDeclarationsController.ApplyWarDeclaration(factionWarDeclaration);
				Person ownerPilot = commsController.CommsStage.Handler.OwnerPilot;
				Person person = EngineASX.Instance.LocalPlayer.Person;
				if (ownerPilot != null && person != null && person.IsPilot)
				{
					Fleet fleet = ownerPilot.GetFleet();
					if (fleet != null)
					{
						EngineASX.Instance.FleetRecentAttacksLog.LogAttack(person.CurrentUnit, fleet);
					}
				}
			}
			commsController.Dismiss();
		}
	}
}
