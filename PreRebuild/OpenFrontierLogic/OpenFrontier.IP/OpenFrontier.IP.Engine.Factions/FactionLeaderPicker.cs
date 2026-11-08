using System.Linq;
using OpenFrontier.IP.Engine.PilotRankings;

namespace OpenFrontier.IP.Engine.Factions
{
	public static class FactionLeaderPicker
	{
		public static Person PickBasedOnShipSize(Faction faction)
		{
			int num = 0;
			Person person = null;
			foreach (Person person2 in faction.People)
			{
				if (person2 != null && !person2.IsAutoPilot)
				{
					int num2 = 0;
					if (person2.IsPilot)
					{
						num2 = person2.CurrentUnit.UnitClass.SaleCost;
					}
					if (person == null || num2 > num)
					{
						person = person2;
						num = num2;
					}
				}
			}
			return person;
		}

		public static Person PickBasedOnRank(Faction faction)
		{
			float num = 0f;
			Person person = null;
			foreach (Person person2 in faction.People)
			{
				if (!(person2 != null) || person2.IsAutoPilot)
				{
					continue;
				}
				float num2 = 0f;
				if (person2.IsPilot)
				{
					num2 = person2.CurrentUnit.UnitClass.RelativeShipSaleCost;
				}
				if (person2.Rank != null)
				{
					PilotRankingSystemRank pilotRankingSystemRank = faction.PilotRankingSystem.Ranks.FirstOrDefault((PilotRankingSystemRank e) => e.Rank == person2.Rank);
					if (pilotRankingSystemRank != null)
					{
						int num3 = faction.PilotRankingSystem.Ranks.IndexOf(pilotRankingSystemRank);
						if (num3 >= 0)
						{
							num2 += 100f * (float)num3;
						}
					}
				}
				if (person == null || num2 > num)
				{
					person = person2;
					num = num2;
				}
			}
			return person;
		}
	}
}
