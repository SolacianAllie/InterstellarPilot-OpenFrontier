namespace OpenFrontier.IP.Engine
{
	public static class PersonExtensions
	{
		public static Fleet GetFleet(this Person person)
		{
			NpcPilot npcPilot = person.NpcPilot;
			if (npcPilot != null)
			{
				return npcPilot.Fleet;
			}
			return null;
		}

		public static Unit GetRootUnit(this Person person)
		{
			if (person.CurrentUnit != null)
			{
				return person.CurrentUnit.GetRootUnit();
			}
			return null;
		}

		public static bool IsInCombat(this Person person)
		{
			if (person.CurrentUnit != null)
			{
				return person.CurrentUnit.IsInCombat();
			}
			return false;
		}

		public static string GetShortNameAndFaction(this Person person)
		{
			if (person.Faction != null)
			{
				return person.GetNameAndRank() + " (" + person.Faction.GetShortNameElseLong() + ")";
			}
			return person.GetNameAndRank();
		}

		public static bool IsLocalFaction(this Person person)
		{
			if (person.Faction != null)
			{
				return person.Faction.IsPlayerFaction;
			}
			return false;
		}

		public static bool HasBounty(this Person person)
		{
			return EngineASX.Instance.HasPersonGotBounty(person);
		}

		public static string GetPilotExtraInfoText(this Person pilot)
		{
			string text = "";
			if (pilot.Kills > 0)
			{
				text += $"Kills: {pilot.Kills:N0}";
			}
			if (pilot.Deaths > 0)
			{
				if (text.Length > 0)
				{
					text += ", ";
				}
				text += $"Deaths: {pilot.Deaths:N0}";
			}
			int bountyValueOnPerson = EngineASX.Instance.GetBountyValueOnPerson(pilot);
			if (bountyValueOnPerson > 0)
			{
				if (text.Length > 0)
				{
					text += ", ";
				}
				text += $"Bounty: {TextFormattingHelper.FormatCredits(bountyValueOnPerson)}";
			}
			return text;
		}
	}
}
