using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.AI.ActiveOrders;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.UniverseEventsNotifier
{
	public class UniverseEventsNotifierController : MonoBehaviour
	{
		private struct AttackInfo
		{
			public Unit Unit;

			public float Time;

			public Sector Sector;

			public Vector3 Position;
		}

		public float CooldownTimeBeforeHostileNotifactions = 20f;

		private EngineASX engine;

		private List<AttackInfo> recentlyAttackedPlayerUnits = new List<AttackInfo>();

		private Dictionary<int, float> lastTimeSatellitePickedUpHostile = new Dictionary<int, float>(10);

		private Dictionary<int, float> lastTimePlayerFleetSentNotifications = new Dictionary<int, float>(10);

		private Dictionary<int, float> lastTimeHostileUnitDetected = new Dictionary<int, float>(100);

		private float lastTimeScannedByOther = float.MinValue;

		public float TimeBetweenScannedNotifications = 20f;

		private const float AttackEventTimeout = 240f;

		public float TimeBetweenSatelliteNotifications = 240f;

		public float TimeBetweenPlayerFleetNotifications = 20f;

		public float TimeBetweenHostileTargetNotifications = 120f;

		public string DefaultFromText = "Computer";

		private static string[] controllingFactionPissedMessages = new string[3] { "Your attack against {TargetProperty} in our sector is not authorized. Stop immediately or face the consequences.", "Cease all your attacks against {TargetProperty} in our sector immediately.", "Your attacks against {TargetProperty} in our sector must stop immediately. You will not receive this warning again." };

		private void Update()
		{
			if (!(engine != null) && EngineASX.LoadedAndReady)
			{
				engine = EngineASX.Instance;
				engine.UnitKilled += Engine_UnitKilled;
				engine.UnitAttacked += Engine_UnitAttacked;
			}
		}

		private void OnDestroy()
		{
			if (engine != null)
			{
				engine.UnitKilled -= Engine_UnitKilled;
				engine.UnitAttacked -= Engine_UnitAttacked;
			}
		}

		public void Engine_UnitConstructed(EngineASX engine, Unit unit)
		{
			if (this.engine.LocalPlayer != null && unit.IsOwnedByPlayer)
			{
				PlayerActiveMessage message = CreatePropertyConstructedMessaged(unit);
				this.engine.LocalPlayer.AddMessage(message);
			}
		}

		private void Engine_UnitAttacked(EngineASX engine, Unit unit, Sector sector, Unit attackingUnit, Faction attackerFaction)
		{
			if (this.engine.LocalPlayer != null && attackerFaction != this.engine.LocalFaction && (!(this.engine.World != null) || !(this.engine.World.ScenarioOptions != null) || this.engine.World.ScenarioOptions.PlayerPropertyAttackNotifications) && !unit.IsPlayerCurrentUnit && unit.IsOwnedByPlayer && unit.IsStationOrShip() && !(this.engine.PlayerUnit == null))
			{
				bool flag = !this.engine.PlayerUnit.IsUnderAttack();
				float num = 1500f;
				if ((this.engine.PlayerUnit == null || unit.Sector != this.engine.PlayerUnit.Sector || (flag && Vector3.Distance(unit.transform.position, this.engine.PlayerUnit.transform.position) > num)) && !HasRecentlyNotifiedAboutUnit(unit))
				{
					PlayerActiveMessage message = CreatePropertyUnderAttackMessage(unit, sector, attackingUnit, attackerFaction);
					this.engine.LocalPlayer.AddMessage(message, notifications: true, important: true);
					recentlyAttackedPlayerUnits.Add(new AttackInfo
					{
						Position = unit.transform.position,
						Sector = unit.Sector,
						Time = Time.time,
						Unit = unit
					});
				}
			}
		}

		private bool HasRecentlyNotifiedAboutUnit(Unit unit)
		{
			for (int i = 0; i < recentlyAttackedPlayerUnits.Count; i++)
			{
				AttackInfo attackInfo = recentlyAttackedPlayerUnits[i];
				if (attackInfo.Time + 240f > Time.time)
				{
					if (attackInfo.Sector == unit.Sector && Vector3.Distance(attackInfo.Position, unit.transform.position) < 1000f)
					{
						return true;
					}
				}
				else
				{
					recentlyAttackedPlayerUnits.RemoveAt(i);
					i--;
				}
			}
			return false;
		}

		private void Engine_UnitKilled(EngineASX engine, Unit unit, Sector sector, Unit attackingUnit, Faction attackerFaction)
		{
			if (this.engine.LocalPlayer != null && attackerFaction != engine.LocalFaction && !unit.IsPlayerCurrentUnit && unit.IsOwnedByPlayer && unit.IsStationOrShip())
			{
				PlayerActiveMessage message = CreatePropertyDestroyedMessage(unit, sector, attackingUnit, attackerFaction);
				this.engine.LocalPlayer.AddMessage(message, notifications: true, important: true);
			}
		}

		private PlayerActiveMessage CreatePropertyConstructedMessaged(Unit unit)
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.SubjectText = $"{unit.Components.GetClassAndName(shortName: true)} Constructed in {unit.Sector.Name} sector";
			playerActiveMessage.MessageText = $"Construction of {unit.Components.GetClassAndName()} has finished.";
			playerActiveMessage.FromText = DefaultFromText;
			playerActiveMessage.SetSubjectUnitAndPosition(unit);
			return playerActiveMessage;
		}

		private PlayerActiveMessage CreatePropertyUnderAttackMessage(Unit unit, Sector sector, Unit attackingUnit, Faction attackingFaction)
		{
			return CreatePropertyUnderAttackEventMessage(unit, sector, attackingUnit, attackingFaction);
		}

		private PlayerActiveMessage CreatePropertyDestroyedMessage(Unit unit, Sector sector, Unit attackingUnit, Faction attackingFaction)
		{
			return CreatePropertyDestroyedEventMessage(unit, sector, attackingUnit, attackingFaction);
		}

		private PlayerActiveMessage CreatePropertyDestroyedEventMessage(Unit unit, Sector sector, Unit attackingUnit, Faction attackingFaction)
		{
			string text = "Owned " + unit.GetFriendlyName(shortName: true) + " destroyed";
			if (!sector.IsActive)
			{
				text = text + " in " + sector.Name + " sector";
			}
			string foreignUnitDescription = GetForeignUnitDescription(attackingUnit, attackingFaction, shortName: false);
			string messageText = "Your " + GetContextString(unit) + " \"" + unit.GetFriendlyName() + "\" was destroyed in the " + sector.Name + " sector by " + foreignUnitDescription + ".";
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.SubjectText = text;
			playerActiveMessage.MessageText = messageText;
			playerActiveMessage.FromText = DefaultFromText;
			playerActiveMessage.SetSubjectUnitAndPosition(unit);
			return playerActiveMessage;
		}

		private string GetShortForeignUnitDescription(Unit unit, Faction faction)
		{
			string empty = string.Empty;
			string empty2 = string.Empty;
			if (unit != null)
			{
				if (unit.UnitType == UnitType.Ship)
				{
					Person pilot = unit.GetPilot();
					empty2 = ((!(pilot != null) || pilot.IsAutoPilot) ? "an unknown pilot" : pilot.ShortName);
				}
				else
				{
					empty2 = "an unknown pilot";
				}
				empty = empty + empty2 + ", in " + unit.GetFriendlyName(shortName: true);
				if (unit.Faction != null && !unit.Faction.IsFreelancer)
				{
					empty = empty + " (" + faction.GetShortNameElseLong() + ")";
				}
				return empty;
			}
			return "an unknown ship";
		}

		private string GetForeignUnitDescription(Unit unit, Faction faction, bool shortName)
		{
			if (unit != null)
			{
				string empty = string.Empty;
				string text = string.Empty;
				if (unit.UnitType == UnitType.Ship)
				{
					Person pilot = unit.GetPilot();
					if (!(pilot != null) || pilot.IsAutoPilot)
					{
						text = "an unknown pilot";
					}
					else
					{
						text = (shortName ? pilot.ShortNameWithShortRank : pilot.FullNameWithFullRank);
					}
				}
				empty = empty + text + ", pilotting " + unit.GetFriendlyName(shortName);
				if (unit.Faction != null && !unit.Faction.IsFreelancer)
				{
					empty = empty + " of the faction \"" + (shortName ? faction.GetShortNameElseLong() : faction.GetLongNameElseShort()) + "\"";
				}
				return empty;
			}
			if (faction != null)
			{
				if (faction.IsFreelancer)
				{
					return faction.GetDescriptiveFactionNameIncludingPilotName(shortName);
				}
				return "vessels belonging to the faction \"" + (shortName ? faction.GetShortNameElseLong() : faction.GetLongNameElseShort()) + "\"";
			}
			return "an unknown vessel";
		}

		private PlayerActiveMessage CreatePropertyUnderAttackEventMessage(Unit unit, Sector sector, Unit attackingUnit, Faction attackingFaction)
		{
			string text = "Owned " + unit.GetFriendlyName(shortName: true) + " under attack";
			if (!sector.IsActive)
			{
				text = text + " in " + sector.Name + " sector";
			}
			string foreignUnitDescription = GetForeignUnitDescription(attackingUnit, attackingFaction, shortName: false);
			string messageText = "Your " + GetContextString(unit) + " \"" + unit.GetFriendlyName() + "\" is under attack in the " + sector.Name + " sector by " + foreignUnitDescription + ".";
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.MessageText = messageText;
			playerActiveMessage.SubjectText = text;
			playerActiveMessage.FromText = DefaultFromText;
			playerActiveMessage.SetSubjectUnitAndPosition(unit);
			return playerActiveMessage;
		}

		private static string GetContextString(Unit unit)
		{
			return unit.UnitType switch
			{
				UnitType.Ship => "ship", 
				UnitType.Cargo => "cargo", 
				UnitType.Station => "station", 
				_ => "property", 
			};
		}

		public static PlayerActiveMessage CreatePlayerFleetInvalidOrderMessage(ActiveFleetOrder order, string messageFromOrder)
		{
			string text = string.Empty;
			if (!string.IsNullOrWhiteSpace(messageFromOrder))
			{
				text = " - " + messageFromOrder;
			}
			string fleetName = GetFleetName(order.Fleet);
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.FromText = UnitNamer.GetPilotNameShipAndFleet(order.Fleet.GetLeaderPilot());
			playerActiveMessage.ToText = GetPlayerToTextFromFactionNpc();
			playerActiveMessage.SubjectText = fleetName + " cancelled order: " + GetFleetOrderDescription(order);
			playerActiveMessage.MessageText = GetPlayerAddressText() + ", " + GetFleet_I_Or_We(order.Fleet, titleCase: false) + " could not complete the order" + text + ".";
			playerActiveMessage.SetSenderUnitAndPosition(order.Fleet.LeaderUnit);
			return playerActiveMessage;
		}

		private static string GetPlayerToTextFromFactionNpc()
		{
			return GetPlayerAddressText() + ", #player#";
		}

		private static string GetFleetName(Fleet fleet)
		{
			if (!string.IsNullOrEmpty(fleet.Name))
			{
				return fleet.Name;
			}
			return "Fleet";
		}

		private static string GetPlayerAddressText()
		{
			if (!string.IsNullOrWhiteSpace(EngineASX.Instance.LocalPlayer.Person.CustomTitle))
			{
				return EngineASX.Instance.LocalPlayer.Person.CustomTitle;
			}
			return GameController.Instance.DefaultPilotTitle;
		}

		private static string GetFleet_I_Or_We(Fleet fleet, bool titleCase)
		{
			if (fleet.PilotCount > 1)
			{
				if (!titleCase)
				{
					return "we";
				}
				return "We";
			}
			return "I";
		}

		private static string GetFleet_Weve_Or_Ive(Fleet fleet, bool titleCase)
		{
			if (fleet.PilotCount > 1)
			{
				if (!titleCase)
				{
					return "we've";
				}
				return "We've";
			}
			return "I've";
		}

		private static string GetFleetOrderDescription(ActiveFleetOrder order)
		{
			string descriptionForFaction = order.FleetOrder.GetDescriptionForFaction(order.Fleet.Faction, null);
			if (!string.IsNullOrWhiteSpace(descriptionForFaction))
			{
				return descriptionForFaction;
			}
			return "Unknown";
		}

		public static PlayerActiveMessage CreatePlayerFleetCompletedOrderMessage(ActiveFleetOrder order)
		{
			string fleetName = GetFleetName(order.Fleet);
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.FromText = UnitNamer.GetPilotNameShipAndFleet(order.Fleet.GetLeaderPilot());
			playerActiveMessage.ToText = GetPlayerToTextFromFactionNpc();
			playerActiveMessage.SubjectText = fleetName + " completed order: " + GetFleetOrderDescription(order);
			playerActiveMessage.MessageText = GetPlayerAddressText() + ", " + GetFleet_I_Or_We(order.Fleet, titleCase: false) + " have completed the order and await further instruction.";
			playerActiveMessage.SetSenderUnitAndPosition(order.Fleet.LeaderUnit);
			return playerActiveMessage;
		}

		public void OnPlayerSatelliteScannedNewHostile(Unit scanner, Unit scanned)
		{
			if (EngineASX.Instance.World.TimeSinceInitialization > CooldownTimeBeforeHostileNotifactions && scanned.UnitType == UnitType.Ship && (!lastTimeSatellitePickedUpHostile.TryGetValue(scanner.UniqueId, out var value) || Time.time > value + TimeBetweenSatelliteNotifications) && (!lastTimeHostileUnitDetected.TryGetValue(scanned.UniqueId, out var value2) || Time.time > value2 + TimeBetweenHostileTargetNotifications))
			{
				lastTimeSatellitePickedUpHostile[scanner.UniqueId] = Time.time;
				lastTimeHostileUnitDetected[scanned.UniqueId] = Time.time;
				try
				{
					PlayerActiveMessage message = CreatePlayerSatelliteScannedNewHostileMessage(scanner, scanned);
					engine.LocalPlayer.AddMessage(message);
				}
				catch
				{
				}
			}
		}

		public void OnPlayerFleetScannedNewHostile(Unit scanner, Unit scanned)
		{
			if (scanned.IsStationOrShip() && EngineASX.Instance.World.TimeSinceInitialization > CooldownTimeBeforeHostileNotifactions && (!lastTimePlayerFleetSentNotifications.TryGetValue(scanner.UniqueId, out var value) || Time.time > value + TimeBetweenPlayerFleetNotifications) && (!lastTimeHostileUnitDetected.TryGetValue(scanned.UniqueId, out var value2) || Time.time > value2 + TimeBetweenHostileTargetNotifications))
			{
				lastTimePlayerFleetSentNotifications[scanner.UniqueId] = Time.time;
				lastTimeHostileUnitDetected[scanned.UniqueId] = Time.time;
				try
				{
					PlayerActiveMessage message = CreatePlayerFleetScannedNewHostileMessage(scanner, scanned);
					engine.LocalPlayer.AddMessage(message);
				}
				catch
				{
				}
			}
		}

		public void OnPlayerFleetScannedAbandonedShipOrStation(Unit scanner, Unit scanned)
		{
			if (!lastTimePlayerFleetSentNotifications.TryGetValue(scanner.UniqueId, out var value) || Time.time > value + TimeBetweenPlayerFleetNotifications)
			{
				lastTimePlayerFleetSentNotifications[scanner.UniqueId] = Time.time;
				try
				{
					PlayerActiveMessage message = CreatePlayerFleetScannedAbandonedShipStationMessage(scanner, scanned);
					engine.LocalPlayer.AddMessage(message);
				}
				catch
				{
				}
			}
		}

		public void OnPlayerFleetScannedAbandonedCargo(Unit scanner, Unit scanned)
		{
			if (!lastTimePlayerFleetSentNotifications.TryGetValue(scanner.UniqueId, out var value) || Time.time > value + TimeBetweenPlayerFleetNotifications)
			{
				lastTimePlayerFleetSentNotifications[scanner.UniqueId] = Time.time;
				try
				{
					PlayerActiveMessage message = CreatePlayerFleetScannedAbandonedCargoMessage(scanner, scanned);
					engine.LocalPlayer.AddMessage(message);
				}
				catch
				{
				}
			}
		}

		private PlayerActiveMessage CreatePlayerSatelliteScannedNewHostileMessage(Unit scanner, Unit scanned)
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.FromText = scanner.GetFriendlyName();
			playerActiveMessage.ToText = "#player#";
			playerActiveMessage.SubjectText = scanner.GetFriendlyName(shortName: true) + " detected hostile " + scanned.GetFriendlyNameAndFactionShortName() + " in " + scanner.Sector.Name;
			playerActiveMessage.MessageText = "Hostile ship was detected.";
			playerActiveMessage.SetSubjectUnitAndPosition(scanned);
			playerActiveMessage.SetSenderUnitAndPosition(scanner);
			return playerActiveMessage;
		}

		private PlayerActiveMessage CreatePlayerFleetScannedNewHostileMessage(Unit scanner, Unit scanned)
		{
			Fleet fleet = scanner.GetFleet();
			Person person = scanner.GetPilot();
			if (fleet.Leader != null)
			{
				person = fleet.Leader.Person;
			}
			string text = ((scanned.UnitType == UnitType.Ship) ? "ship" : "station");
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.FromText = UnitNamer.GetPilotNameShipAndFleet(person);
			playerActiveMessage.ToText = GetPlayerToTextFromFactionNpc();
			playerActiveMessage.SubjectText = fleet.GetFriendlyName() + " detected hostile " + scanned.GetFriendlyNameAndFactionShortName() + " in " + scanner.Sector.Name;
			playerActiveMessage.MessageText = GetPlayerAddressText() + ", " + GetFleet_Weve_Or_Ive(fleet, titleCase: true) + " detected a new hostile " + text + ".";
			playerActiveMessage.SetSubjectUnitAndPosition(scanned);
			playerActiveMessage.SetSenderUnitAndPosition(scanner);
			return playerActiveMessage;
		}

		private PlayerActiveMessage CreatePlayerFleetScannedAbandonedShipStationMessage(Unit scanner, Unit scanned)
		{
			Fleet fleet = scanner.GetFleet();
			Person person = scanner.GetPilot();
			if (fleet.Leader != null)
			{
				person = fleet.Leader.Person;
			}
			string text = ((scanned.UnitType == UnitType.Ship) ? "ship" : "station");
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.FromText = UnitNamer.GetPilotNameShipAndFleet(person);
			playerActiveMessage.ToText = GetPlayerToTextFromFactionNpc();
			playerActiveMessage.SubjectText = fleet.GetFriendlyName() + " detected abandoned " + scanned.GetFriendlyName() + " in " + scanner.Sector.Name;
			playerActiveMessage.MessageText = GetPlayerAddressText() + ", " + GetFleet_Weve_Or_Ive(fleet, titleCase: false) + " detected abandoned " + text + " in the " + scanner.Sector.Name + " sector.";
			playerActiveMessage.SetSubjectUnitAndPosition(scanned);
			playerActiveMessage.SetSenderUnitAndPosition(scanner);
			return playerActiveMessage;
		}

		private PlayerActiveMessage CreatePlayerFleetScannedAbandonedCargoMessage(Unit scanner, Unit scanned)
		{
			Fleet fleet = scanner.GetFleet();
			Person person = scanner.GetPilot();
			if (fleet.Leader != null)
			{
				person = fleet.Leader.Person;
			}
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.FromText = UnitNamer.GetPilotNameShipAndFleet(person);
			playerActiveMessage.ToText = GetPlayerToTextFromFactionNpc();
			playerActiveMessage.SubjectText = fleet.GetFriendlyName() + " detected abandoned " + scanned.GetFriendlyName() + " in " + scanner.Sector.Name;
			playerActiveMessage.MessageText = GetPlayerAddressText() + ", " + GetFleet_Weve_Or_Ive(fleet, titleCase: false) + " detected abandoned cargo in the " + scanner.Sector.Name + " sector.";
			playerActiveMessage.SetSubjectUnitAndPosition(scanned);
			playerActiveMessage.SetSenderUnitAndPosition(scanner);
			return playerActiveMessage;
		}

		public void NotifyPlayerScannedByOther(Unit scanner)
		{
			if (Time.time > lastTimeScannedByOther + TimeBetweenScannedNotifications)
			{
				UIController.Instance.QuickMsg.AddMessage("Ship scanned by " + GetShortForeignUnitDescription(scanner, scanner.Faction));
				lastTimeScannedByOther = Time.time;
			}
		}

		public PlayerActiveMessage GenerateFactionGainedControlOfSectorMessage(Sector sector, Faction owner)
		{
			if (owner.IsPlayerFaction)
			{
				return new PlayerActiveMessage
				{
					FromText = "Computer",
					ToText = "#player#",
					SubjectText = "You now have control of the " + sector.Name + " sector",
					MessageText = sector.Name + " now under control",
					AllowDelete = true
				};
			}
			return new PlayerActiveMessage
			{
				FromText = "Computer",
				ToText = "#player#",
				SubjectText = owner.GetShortNameElseLong() + " takes control of the " + sector.Name + " sector",
				MessageText = owner.GetLongNameElseShort() + " has gained control of the " + sector.Name + " sector.",
				AllowDelete = true
			};
		}

		public PlayerActiveMessage GenerateFactionLostControlOfSectorMessage(Sector sector, Faction previousOwner)
		{
			if (previousOwner.IsPlayerFaction)
			{
				return new PlayerActiveMessage
				{
					FromText = "Computer",
					ToText = "#player#",
					SubjectText = "Lost control of the " + sector.Name + " sector",
					MessageText = "You have lost control of the " + sector.Name + " sector.",
					AllowDelete = true
				};
			}
			return new PlayerActiveMessage
			{
				FromText = "Computer",
				ToText = "#player#",
				SubjectText = previousOwner.GetShortNameElseLong() + " loses control of the " + sector.Name + " sector",
				MessageText = previousOwner.GetLongNameElseShort() + " has lost control of the " + sector.Name + " sector.",
				AllowDelete = true
			};
		}

		public PlayerActiveMessage GenerateFactionCapturesSectorMessage(Sector sector, Faction newOwner, Faction previousOwner)
		{
			if (newOwner.IsPlayerFaction)
			{
				return GenerateFactionGainedControlOfSectorMessage(sector, newOwner);
			}
			if (previousOwner.IsPlayerFaction)
			{
				return new PlayerActiveMessage
				{
					FromText = "Computer",
					ToText = "#player#",
					SubjectText = "Lost control of the " + sector.Name + " sector",
					MessageText = "Control of the " + sector.Name + " sector has been lost. The sector was captured by " + newOwner.GetLongNameElseShort(),
					AllowDelete = true
				};
			}
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.FromText = "Computer";
			playerActiveMessage.ToText = "#player#";
			playerActiveMessage.SubjectText = newOwner.GetShortNameElseLong() + " takes control of the " + sector.Name + " sector from " + previousOwner.GetShortNameElseLong();
			playerActiveMessage.MessageText = previousOwner.GetLongNameElseShort() + " has lost control of the " + sector.Name + " sector. The sector was captured by " + newOwner.GetLongNameElseShort();
			playerActiveMessage.AllowDelete = true;
			return playerActiveMessage;
		}

		public void OnFactionPissedAtPlayerForAttackingCivilians(Faction pissedFaction, Faction attackedFaction, Sector attackSector)
		{
			PlayerActiveMessage message = GenerateFactionPissedAtPlayerForAttackingCivilians(pissedFaction, attackedFaction, attackSector);
			engine.LocalPlayer.AddMessage(message, notifications: true, important: true);
		}

		public PlayerActiveMessage GenerateFactionPissedAtPlayerForAttackingCivilians(Faction pissedFaction, Faction attackedFaction, Sector attackSector)
		{
			string text = "Cease attacks";
			if (attackSector != null)
			{
				text = text + " in the " + attackSector.Name + " sector";
			}
			string newValue = "targets";
			if (attackedFaction != null)
			{
				if (attackedFaction.IsFreelancer)
				{
					newValue = attackedFaction.GetFriendlyName();
				}
				else
				{
					newValue = ((!(Random.value < 0.5f)) ? ("property of " + attackedFaction.GetFriendlyName()) : (attackedFaction.GetFriendlyName() + " property"));
				}
			}
			return new PlayerActiveMessage
			{
				FromText = pissedFaction.GetFriendlyName(),
				ToText = "#player#",
				SubjectText = text,
				MessageText = controllingFactionPissedMessages.GetRandom().Replace("{TargetProperty}", newValue),
				AllowDelete = true
			};
		}
	}
}
