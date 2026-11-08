namespace OpenFrontier.IP.Engine.Core.Units
{
	public class KilledUnitHandler
	{
		public static void Handle(KillData killData)
		{
			foreach (KilledUnit killedUnit in killData.KilledUnits)
			{
				if (killedUnit.Faction != null && killedUnit.Faction.IsValidInGame)
				{
					killedUnit.Faction.HandleOwnedUnitKilled(killedUnit.Unit, killedUnit.UnitClass, killData.KillerUnit, killData.KillerFaction, killData.DamageDirectType);
				}
				if (killData.KillerFaction != null && killData.KillerFaction.IsValidInGame)
				{
					killData.KillerFaction.HandleKilledAnotherFactionsUnit(killedUnit.Unit, killData.KillerUnit, killedUnit.Faction);
				}
			}
			foreach (KilledPerson killedPerson in killData.KilledPeople)
			{
				if (killedPerson.Faction != null && killedPerson.Faction.IsValidInGame)
				{
					killedPerson.Faction.HandleOwnedPersonKilled(killedPerson.Person, killData.KillerUnit, killData.KillerFaction, killData.DamageDirectType);
				}
			}
		}
	}
}
