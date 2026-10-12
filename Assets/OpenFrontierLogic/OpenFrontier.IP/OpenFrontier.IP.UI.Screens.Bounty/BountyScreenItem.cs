using System;
using System.Text;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using TMPro;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.Bounty
{
	public class BountyScreenItem : ScrollListItem<FactionBountyItem>
	{
		public TextMeshProUGUI WantedByText;

		public TextMeshProUGUI RewardCreditsText;

		public TextMeshProUGUI PilotNameText;

		public TextMeshProUGUI FactionText;

		public TextMeshProUGUI LastKnownLocationText;

		private static StringBuilder stringBuilder = new StringBuilder();

		private Faction LocalFaction
		{
			get
			{
				EngineASX instance = EngineASX.Instance;
				if (instance != null)
				{
					return instance.LocalFaction;
				}
				return null;
			}
		}

		private void RefreshWantedByText()
		{
			WantedByText.text = GetWantedByText();
			WantedByText.color = GetWantedByTextColor();
		}

		private Color GetWantedByTextColor()
		{
			return GetFactionColor(Item.Source);
		}

		private Color GetPilotFactionTextColor()
		{
			return GetFactionColor(Item.Person.Faction);
		}

		private Color GetFactionColor(Faction faction)
		{
			EngineASX instance = EngineASX.Instance;
			if (instance != null)
			{
				Faction localFaction = LocalFaction;
				if (localFaction != null && localFaction.IsValidInGame)
				{
					return instance.GetFactionHostilityColor(faction, localFaction);
				}
				return instance.AttitudeNeutralColor;
			}
			return Color.white;
		}

		private string GetWantedByText()
		{
			return Item.Source.GetLongNameElseShort();
		}

		private void RefreshReward()
		{
			RewardCreditsText.text = TextFormattingHelper.FormatCreditsWithDashForZero(Item.Bounty);
		}

		public override void Refresh()
		{
			base.Refresh();
			if (Item.IsValid)
			{
				RefreshLastKnownLocation();
				RefreshReward();
				RefreshWantedByText();
				RefreshPilotNameText();
				RefreshPilotFactionText();
			}
		}

		private void RefreshPilotFactionText()
		{
			FactionText.text = Item.Person.Faction.GetDescriptiveFactionName(shortName: false);
			FactionText.color = GetPilotFactionTextColor();
		}

		private void RefreshPilotNameText()
		{
			PilotNameText.text = Item.Person.GetNameAndTitleOrFullRank(shortName: false);
		}

		private void RefreshLastKnownLocation()
		{
			LastKnownLocationText.text = GetLastKnownLocationText();
		}

		private string GetLastKnownLocationText()
		{
			string lastKnownLocationText = GetLastKnownLocationText(Item.LastKnownSector, Item.LastKnownPilottedShip, Item.TimeOfLastSighting);
			if (!string.IsNullOrWhiteSpace(lastKnownLocationText))
			{
				return lastKnownLocationText;
			}
			return "Unknown";
		}

		public static string GetLastKnownLocationText(Sector sector, Unit unit, double? time)
		{
			if (sector != null)
			{
				stringBuilder.Length = 0;
				stringBuilder.Append(GetLastKnownSectorName(sector));
				if (unit != null)
				{
					stringBuilder.Append(" in " + unit.GetFriendlyName());
				}
				if (time.HasValue)
				{
					TimeSpan gameWorldTimespanFromSeconds = EngineASX.Instance.DateTimeUtils.GetGameWorldTimespanFromSeconds(time.Value, EngineASX.Instance.ScenarioElapsedTime);
					if (gameWorldTimespanFromSeconds.TotalMinutes > 0.0)
					{
						string shortTimespanDescription = EngineASX.Instance.DateTimeUtils.GetShortTimespanDescription(gameWorldTimespanFromSeconds);
						stringBuilder.Append(" " + shortTimespanDescription + " ago");
					}
					else
					{
						stringBuilder.Append(" just now");
					}
				}
				return stringBuilder.ToString();
			}
			return null;
		}

		public static string GetLastKnownSectorName(Sector sector)
		{
			if (EngineASX.Instance.ActiveSector != null)
			{
				return UnitNamer.GetSectorNameAndDistanceForFaction(EngineASX.Instance.LocalFaction, EngineASX.Instance.ActiveSector, sector);
			}
			return sector.Name;
		}
	}
}
