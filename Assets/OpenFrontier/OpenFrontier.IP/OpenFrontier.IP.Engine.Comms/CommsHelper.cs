using OpenFrontier.IP.Engine.Factions;

namespace OpenFrontier.IP.Engine.Comms
{
	public static class CommsHelper
	{
		public static string DefaultDismissText = "Nevermind";

		public static bool TryGetCommsHandlerFromUnit(Unit unit, out ICommsHandler commsHandler)
		{
			commsHandler = null;
			if (unit.Components != null && !unit.UnitClass.IsTurret)
			{
				if (unit.Components.Crew.Count > 0)
				{
					foreach (Person item in unit.Components.Crew)
					{
						if (item != null && item.Faction == unit.Faction)
						{
							return TryGetCommsHandlerFromPerson(item, out commsHandler);
						}
					}
				}
				if (unit.IsMajorStation() && unit.Faction != null && unit.Faction.LeaderPerson != null)
				{
					return TryGetCommsHandlerFromPerson(unit.Faction.LeaderPerson, out commsHandler);
				}
				return false;
			}
			return false;
		}

		public static bool TryGetCommsHandlerFromPerson(Person pilot, out ICommsHandler commsHandler)
		{
			commsHandler = null;
			if (pilot != null)
			{
				if (TryGetManualCommsHandlerFromPerson(pilot, out commsHandler))
				{
					return true;
				}
				if (pilot.Faction != null && pilot.Faction.FactionAI != null && !pilot.IsAutoPilot)
				{
					commsHandler = pilot.Faction.FactionAI.GetCommsHandlerForPilot(pilot);
					if (commsHandler != null && commsHandler.IsEnabled && commsHandler.GetStage() != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool TryGetManualCommsHandlerFromPerson(Person person, out ICommsHandler commsHandler)
		{
			commsHandler = null;
			DialogBase commsHandler2 = person.CommsHandler;
			if (commsHandler2 != null && commsHandler2.IsEnabled && commsHandler2.GetStage() != null)
			{
				commsHandler = commsHandler2;
				return true;
			}
			return false;
		}

		public static string FormatMessageWithCreditsCost(string msg, int cost)
		{
			return $"{msg} [Pay {TextFormattingHelper.FormatCredits(cost)} credits]";
		}

		public static void OpenCommsWithHandler(ICommsHandler commsHandler, Faction requestorFaction)
		{
			if (commsHandler.OwnerFaction != null)
			{
				commsHandler.OwnerFaction.GetOrCreateAttitude(requestorFaction);
			}
			EngineASX.Instance.LocalFaction.SyncNeutrality(commsHandler.OwnerFaction);
			ICommsStage stage = commsHandler.GetStage();
			if (stage != null)
			{
				EngineASX.Instance.DialogController.ChangeStage(stage);
			}
		}
	}
}
