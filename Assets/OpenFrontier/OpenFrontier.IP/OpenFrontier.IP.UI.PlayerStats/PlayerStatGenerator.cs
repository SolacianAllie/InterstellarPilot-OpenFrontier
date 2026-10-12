using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI.PlayerStats
{
	public class PlayerStatGenerator : StatGeneratorUI<GamePlayer>
	{
		protected override void refresh()
		{
			base.refresh();
			if (Item != null && Item.Engine != null)
			{
				AddDateStat();
				AddCreditsStat();
				AddStat("Net worth", TextFormattingHelper.FormatCredits(Item.Faction.GetCachedNetWorth(), includeSuffix: true));
				AddStat("Sectors discovered", $"{Item.Faction.Intel.DiscoveredScenesCount} / {Item.Engine.Sectors.Count}");
				AddStat("Sectors visited", $"{Mathf.Min(EngineASX.Instance.LocalPlayer.Stats.SectorsVisitedCount, Item.Engine.Sectors.Count)} / {Item.Engine.Sectors.Count}");
				AddStat("Ships owned", Item.Faction.GetCountOfUnitType(UnitType.Ship));
				AddStat("Stations owned", Item.Faction.GetCountOfUnitType(UnitType.Station));
				AddStat("Current bounty", TextFormattingHelper.FormatCredits(Item.Engine.LocalPlayer.Person.GetTotalBounty()));
				AddStat("Bounty claimed", TextFormattingHelper.FormatCredits(Item.Engine.LocalPlayer.Stats.TotalBountyClaimed));
				if (Item.Person.Deaths > 0)
				{
					AddStat("Deaths", TextFormattingHelper.FormatNumber(Item.Person.Deaths));
				}
				if (Item.Faction.Stats != null)
				{
					AddStat("Ships claimed", Item.Faction.Stats.TotalShipsClaimed);
					AddStat("Ships lost", Item.Faction.Stats.GetUnitsLostCountByType(UnitType.Ship));
					AddStat("Stations lost", Item.Faction.Stats.GetUnitsLostCountByType(UnitType.Station));
					AddStat("Ships killed", Item.Faction.Stats.GetUnitsKilledCountByType(UnitType.Ship));
					AddStat("Stations killed", Item.Faction.Stats.GetUnitsKilledCountByType(UnitType.Station));
					AddStat("Ships mined to death", Item.Engine.LocalPlayer.Stats.ShipsMinedToDeath);
					AddStat("Scratchcards scratched", TextFormattingHelper.FormatNumber(Item.Faction.Stats.ScratchcardsScratched));
					AddStat("Highest scratchcard win", TextFormattingHelper.FormatNumber(Item.Faction.Stats.HighestScratchcardWin));
				}
			}
		}

		private void AddCreditsStat()
		{
			string wealthLevelDescription = Item.Faction.GetWealthLevelDescription();
			string text = TextFormattingHelper.FormatCredits(Item.Credits, includeSuffix: true);
			if (wealthLevelDescription != null)
			{
				text = $"{text} ({wealthLevelDescription})";
			}
			AddStat("Credits", text);
		}

		private void AddDateStat()
		{
			string formattedGameWorldDate = Item.Engine.DateTimeUtils.FormattedGameWorldDate;
			int gameWorldDay = Item.Engine.DateTimeUtils.GetGameWorldDay();
			string val = $"{formattedGameWorldDate} (Day {gameWorldDay})";
			AddStat("Date", val);
		}
	}
}
