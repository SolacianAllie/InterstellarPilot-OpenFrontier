using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Factions.Bounty;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class BountyHelper
	{
		public static bool IsBountyPlacedOnUnitPilot(Unit unit)
		{
			if (unit.Components != null && unit.Components.PilotPerson != null)
			{
				return EngineASX.Instance.HasPersonGotBounty(unit.Components.PilotPerson);
			}
			return false;
		}

		public static void RemoveBountiesPlacedByFactionOnPerson(Faction placingFaction, Person person)
		{
			List<FactionBountyItem> list = new List<FactionBountyItem>();
			List<FactionBountyItem> bountiesOnPerson = EngineASX.Instance.GetBountiesOnPerson(person);
			if (bountiesOnPerson != null && bountiesOnPerson.Count > 0)
			{
				foreach (FactionBountyItem item in bountiesOnPerson)
				{
					if (item != null && item.Source == placingFaction)
					{
						list.Add(item);
					}
				}
			}
			EngineASX.Instance.RemoveBountiesOnPersonPlacedByFaction(person, placingFaction);
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (!(faction.BountyBoard != null))
				{
					continue;
				}
				foreach (FactionBountyItem item2 in list)
				{
					faction.BountyBoard.BountyItems.Remove(item2);
				}
			}
		}

		public static void RemoveBountiesPlacedByFaction(Faction faction)
		{
			foreach (Faction faction2 in EngineASX.Instance.Factions)
			{
				if (faction2.BountyBoard != null)
				{
					faction2.BountyBoard.RemoveBountyPlacedByFaction(faction);
				}
			}
		}

		public static void ReleaseBountiesOnFaction(Faction faction)
		{
			foreach (Person person in faction.People)
			{
				if (person != null)
				{
					ReleaseBountiesOnPerson(person);
				}
			}
		}

		public static void ReleaseBountiesOnPerson(Person person)
		{
			List<FactionBountyItem> bountiesOnPerson = person.Engine.GetBountiesOnPerson(person);
			if (bountiesOnPerson == null)
			{
				return;
			}
			foreach (FactionBountyItem item in bountiesOnPerson)
			{
				ReleaseItemInternal(item);
			}
			person.Engine.RemoveBountiesOnPersonInternal(person);
		}

		private static void ReleaseItemInternal(FactionBountyItem item)
		{
			if (!(item.Source != null))
			{
				return;
			}
			if (item.Bounty > 0)
			{
				item.Source.ApplyTransaction(item.Bounty, FactionTransactionType.Bounty);
			}
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.BountyBoard != null && faction.BountyBoard.RemoveBounty(item))
				{
					break;
				}
			}
		}

		public static int GetTotalBountyPlacedOnFaction(Faction targetFaction)
		{
			int num = 0;
			foreach (Person person in targetFaction.People)
			{
				List<FactionBountyItem> bountiesOnPerson = person.Engine.GetBountiesOnPerson(person);
				if (bountiesOnPerson == null || bountiesOnPerson.Count <= 0)
				{
					continue;
				}
				foreach (FactionBountyItem item in bountiesOnPerson.ToList())
				{
					if (item.IsValid)
					{
						num += item.Bounty;
					}
				}
			}
			return num;
		}

		public static int GetTotalBountyPlacedOnFactionByFaction(Faction targetFaction, Faction placingFaction)
		{
			int num = 0;
			foreach (Person person in targetFaction.People)
			{
				List<FactionBountyItem> bountiesOnPerson = person.Engine.GetBountiesOnPerson(person);
				if (bountiesOnPerson == null || bountiesOnPerson.Count <= 0)
				{
					continue;
				}
				foreach (FactionBountyItem item in bountiesOnPerson.ToList())
				{
					if (item.Source == placingFaction && item.IsValid)
					{
						num += item.Bounty;
					}
				}
			}
			return num;
		}

		public static int GetTotalBountyPlacedOnPersonByFaction(Person person, Faction faction)
		{
			int num = 0;
			List<FactionBountyItem> bountiesOnPerson = person.Engine.GetBountiesOnPerson(person);
			if (bountiesOnPerson != null && bountiesOnPerson.Count > 0)
			{
				foreach (FactionBountyItem item in bountiesOnPerson.ToList())
				{
					if (item.Source == faction && item.IsValid)
					{
						num += item.Bounty;
					}
				}
			}
			return num;
		}

		public static void ReleaseBountiesOnPersonPlacedByFaction(Person person, Faction faction)
		{
			List<FactionBountyItem> bountiesOnPerson = person.Engine.GetBountiesOnPerson(person);
			if (bountiesOnPerson != null)
			{
				foreach (FactionBountyItem item in bountiesOnPerson.ToList())
				{
					if (item.Source == faction)
					{
						ReleaseItemInternal(item);
					}
				}
			}
			person.Engine.RemoveBountiesOnPersonPlacedByFaction(person, faction);
		}

		public static void GrantBounties(Person killedPerson, Faction attackerFaction)
		{
			if (!(attackerFaction != null) || !(attackerFaction != killedPerson.Faction))
			{
				return;
			}
			EngineASX engine = killedPerson.Engine;
			List<FactionBountyItem> bountiesOnPerson = engine.GetBountiesOnPerson(killedPerson);
			if (bountiesOnPerson == null)
			{
				return;
			}
			foreach (FactionBountyItem item in bountiesOnPerson.ToList())
			{
				if (item == null || !(item.Source != null) || item.Source == attackerFaction)
				{
					continue;
				}
				attackerFaction.ApplyTransaction(item.Bounty, FactionTransactionType.Bounty, item.Source);
				if (attackerFaction.IsPlayerFaction)
				{
					PlayerActiveMessage message = new PlayerActiveMessage
					{
						ToText = engine.LocalFaction.GetLongNameElseShort(),
						FromText = item.Source.GetLongNameElseShort(),
						AllowDelete = true,
						SubjectText = "Bounty Received",
						MessageText = $"As a result of your destruction of {item.Person.GetNameAndRank()}, your account has been credited with the bounty value of {TextFormattingHelper.FormatCredits(item.Bounty)} placed by {item.Source.GetLongNameElseShort()}"
					};
					if (engine.LocalPlayer.Stats != null)
					{
						engine.LocalPlayer.Stats.TotalBountyClaimed += item.Bounty;
					}
					engine.LocalPlayer.Faction.CreateAttitudeIfNone(item.Source);
					engine.LocalPlayer.AddMessage(message);
				}
				else
				{
					EngineASX.Instance.DebugInfo.NpcBountyHunterReward += item.Bounty;
				}
				item.Bounty = 0;
			}
		}

		public static int RoundBountyToNearest(float bounty)
		{
			return (int)(bounty / 500f) * 500;
		}

		public static FactionBountyItem AddBountyWithRounding(Faction source, FactionBountyBoard bountyBoard, Person person, float bounty, double timeOfSighting, bool updateLastKnownPosition)
		{
			int bounty2 = RoundBountyToNearest(bounty);
			return AddBounty(source, bountyBoard, person, bounty2, timeOfSighting, updateLastKnownPosition);
		}

		public static FactionBountyItem AddBounty(Faction sourceFaction, FactionBountyBoard bountyBoard, Person person, int bounty, double? timeOfSighting, bool updateLastKnownPosition)
		{
			if (bountyBoard != null)
			{
				if (person != null)
				{
					if (bounty > 0)
					{
						FactionBountyItem factionBountyItem = bountyBoard.AddBounty(sourceFaction, person, bounty, timeOfSighting);
						sourceFaction.Engine.AddBountyOnPerson(person, factionBountyItem);
						factionBountyItem.LastKnownPilottedShip = person.CurrentUnit;
						if (updateLastKnownPosition && factionBountyItem.LastKnownPilottedShip != null)
						{
							factionBountyItem.UpdateLastKnownPosition(factionBountyItem.LastKnownPilottedShip.Sector, factionBountyItem.LastKnownPilottedShip.SectorPosition);
						}
						return factionBountyItem;
					}
				}
				else
				{
					Debug.LogError("AddBounty: required valid pilot");
				}
			}
			else
			{
				Debug.LogError($"Cannot add bounty: faction {sourceFaction} No bounty board specified");
			}
			return null;
		}

		public static void PayOffBountyPlacedByFactionOnFaction(EngineASX engine, Faction targetFaction, Faction bountyPlacingFaction)
		{
			foreach (Person person in targetFaction.People)
			{
				int totalBountyPlacedOnPersonByFaction = GetTotalBountyPlacedOnPersonByFaction(person, bountyPlacingFaction);
				if (totalBountyPlacedOnPersonByFaction > 0)
				{
					targetFaction.ApplyTransaction(-totalBountyPlacedOnPersonByFaction, FactionTransactionType.Bounty, bountyPlacingFaction);
					bountyPlacingFaction.ApplyTransaction(totalBountyPlacedOnPersonByFaction, FactionTransactionType.Bounty, targetFaction);
				}
				RemoveBountiesPlacedByFactionOnPerson(bountyPlacingFaction, person);
			}
		}

		public static void PayOffBountyPlacedByFactionOnPerson(EngineASX engine, Person targetPerson, Faction bountyPlacingFaction)
		{
			int totalBountyPlacedOnPersonByFaction = GetTotalBountyPlacedOnPersonByFaction(targetPerson, bountyPlacingFaction);
			if (totalBountyPlacedOnPersonByFaction > 0)
			{
				if (bountyPlacingFaction.IsPlayerFaction)
				{
					engine.AddCreditsToPlayerFactionWithMsg(-totalBountyPlacedOnPersonByFaction, FactionTransactionType.Bounty, bountyPlacingFaction);
				}
				else
				{
					targetPerson.Faction.ApplyTransaction(-totalBountyPlacedOnPersonByFaction, FactionTransactionType.Bounty, bountyPlacingFaction);
				}
				bountyPlacingFaction.ApplyTransaction(totalBountyPlacedOnPersonByFaction, FactionTransactionType.Bounty, targetPerson.Faction);
			}
			RemoveBountiesPlacedByFactionOnPerson(bountyPlacingFaction, targetPerson);
		}

		public static void PayOffBountyItem(EngineASX engine, FactionBountyItem item, Faction payingFaction)
		{
			int bounty = item.Bounty;
			_ = item.Person;
			if (bounty > 0)
			{
				payingFaction.ApplyTransaction(-bounty, FactionTransactionType.Bounty, item.Source);
				item.Bounty = 0;
				ReleaseItemInternal(item);
				if (EngineASX.Instance.GetBountyValueOnPerson(item.Person) == 0)
				{
					EngineASX.Instance.RemoveBountiesOnPersonInternal(item.Person);
				}
			}
		}

		public static bool CanFactionPlaceBounty(Faction faction, int bountyValue)
		{
			if (bountyValue >= faction.Engine.GameSettings.BountyMinCreditsPlaced && bountyValue <= faction.Credits)
			{
				return faction.EstimateNonMinorShipAndStationCount() > 0;
			}
			return false;
		}

		public static void PlaceBountyWithNotifications(Faction sourceFaction, FactionBountyBoard bountyBoard, Person targetPerson, int bountyValue)
		{
			sourceFaction.Credits -= bountyValue;
			if (AddBounty(sourceFaction, bountyBoard, targetPerson, bountyValue, EngineASX.Instance.ScenarioElapsedTime, updateLastKnownPosition: true) != null)
			{
				targetPerson.Faction.CreateAttitudeIfNone(sourceFaction);
				if (targetPerson.Faction.IsPlayerFaction)
				{
					sourceFaction.Engine.OnBountyPlacedOnPlayerPilot(targetPerson, sourceFaction, bountyValue);
				}
			}
			else if (LogWrapper.LogMsgs)
			{
				Debug.LogError("Failed to place bounty");
			}
		}

		public static void UpdateBountyLastKnownPositionAndTime(Person person)
		{
			List<FactionBountyItem> bountiesOnPerson = EngineASX.Instance.GetBountiesOnPerson(person);
			if (bountiesOnPerson == null)
			{
				return;
			}
			foreach (FactionBountyItem item in bountiesOnPerson)
			{
				item.LastKnownSector = person.Sector;
				if (person.IsPilot)
				{
					item.LastKnownPilottedShip = person.CurrentUnit;
					item.LastKnownSectorPosition = person.CurrentUnit.SectorPosition;
					item.TimeOfLastSighting = EngineASX.Instance.ScenarioElapsedTime;
				}
			}
		}
	}
}
