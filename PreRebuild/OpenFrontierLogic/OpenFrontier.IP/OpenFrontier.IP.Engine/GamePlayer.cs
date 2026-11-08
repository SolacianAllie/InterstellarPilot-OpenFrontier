using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine.Core;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[RequireComponent(typeof(Person))]
	public class GamePlayer : MonoBehaviour
	{
		public delegate void MessageAddRemovedHandler(GamePlayer sender, PlayerActiveMessage m, bool added);

		public struct delayedMessage
		{
			public PlayerActiveMessage Message;

			public double ShowTime;

			public bool Important;

			public bool Notifications;
		}

		public GamePlayerStats Stats;

		public const int MaxXpLevel = 30;

		public static int MsgIdCounter = 100000;

		private Mission activeMission;

		private List<delayedMessage> delayedMessages = new List<delayedMessage>();

		private EngineASX engine;

		private List<PlayerActiveMessage> messageList = new List<PlayerActiveMessage>();

		private Dictionary<int, PlayerActiveMessage> messagesById = new Dictionary<int, PlayerActiveMessage>();

		private Person person;

		private HashSet<int> visitedUnits = new HashSet<int>();

		private int xp;

		private int xpLevel = 1;

		public int Level
		{
			get
			{
				return xpLevel;
			}
			set
			{
				xpLevel = value;
			}
		}

		public int XP
		{
			get
			{
				return xp;
			}
			set
			{
				xp = value;
				if (xp < 0)
				{
					xp = 0;
				}
			}
		}

		public PlayerWaypointController WaypointController => engine.WaypointController;

		public Person Person => person;

		public Faction Faction => person.Faction;

		public IEnumerable<Unit> VisitedUnits
		{
			get
			{
				foreach (int visitedUnit in visitedUnits)
				{
					Unit unitByid = engine.GetUnitByid(visitedUnit);
					if (unitByid != null)
					{
						yield return unitByid;
					}
				}
			}
		}

		public IEnumerable<PlayerActiveMessage> Messages => messagesById.Values;

		public EngineASX Engine => engine;

		public Sector Sector => person.Sector;

		public int UnreadMessageCount
		{
			get
			{
				int num = 0;
				for (int i = 0; i < messageList.Count; i++)
				{
					if (!messageList[i].Opened)
					{
						num++;
					}
				}
				return num;
			}
		}

		public bool HasUnreadMessages
		{
			get
			{
				for (int i = 0; i < messageList.Count; i++)
				{
					if (!messageList[i].Opened)
					{
						return true;
					}
				}
				return false;
			}
		}

		public Mission ActiveMission
		{
			get
			{
				return activeMission;
			}
			set
			{
				if (!(activeMission != value))
				{
					return;
				}
				activeMission = value;
				if (!(WaypointController != null))
				{
					return;
				}
				foreach (PlayerWaypointPath missionPath in WaypointController.MissionPaths)
				{
					missionPath.Waypoint = null;
				}
			}
		}

		public int Credits
		{
			get
			{
				if (person.Faction != null)
				{
					return person.Faction.Credits;
				}
				return 0;
			}
			set
			{
				if (person.Faction != null)
				{
					person.Faction.Credits = value;
					return;
				}
				throw new ArgumentException("Player does not have faction. Cannot assign credits");
			}
		}

		public bool HasCustomWaypoint => WaypointController.CustomPath.HasWaypoint;

		public bool HasCustomUnitWaypoint => CustomUnitWaypoint != null;

		public Unit CustomUnitWaypoint
		{
			get
			{
				if (HasCustomWaypoint)
				{
					return WaypointController.CustomPath.Waypoint.Value.TargetUnit;
				}
				return null;
			}
		}

		public Dictionary<int, PlayerActiveMessage> MessagesById => messagesById;

		public List<delayedMessage> DelayedMessages => delayedMessages;

		public List<PlayerActiveMessage> MessageList => messageList;

		public event MessageAddRemovedHandler MessagedAddRemoved;

		public static int CalculateLevelUpXP(int level)
		{
			return (int)(Mathf.Pow((float)level * 75.05f, 1.5f) / 100f) * 100;
		}

		public static int CalculateRemainingLevelUpXP(int curLevel, int curXp)
		{
			return CalculateLevelUpXP(curLevel) - curXp;
		}

		public void Awake()
		{
			engine = EngineASX.Instance;
			person = GetComponent<Person>();
			CreateStatsIfNull();
		}

		public void CreateStatsIfNull()
		{
			if (Stats == null)
			{
				Stats = UnityObjectHelper.NewGameObject<GamePlayerStats>(transform);
				Stats.gameObject.name = "Stats";
			}
		}

		public void AddXp(int xp)
		{
			if (xp != 0)
			{
				XP += xp;
				bool flag = false;
				UIController.Instance.QuickMsg.AddMessage($"Added {xp} XP");
				while (CanLevelUp() && this.xp >= CalculateLevelUpXP())
				{
					LevelUp();
					flag = true;
				}
				if (flag)
				{
					ShowLevelUpMessage();
				}
			}
		}

		[ContextMenu("Level Up")]
		public void LevelUp()
		{
			xpLevel++;
		}

		public bool CanLevelUp()
		{
			return xpLevel < 30;
		}

		public int CalculateLevelUpXP()
		{
			return CalculateLevelUpXP(xpLevel);
		}

		public int CalculateRemainingLevelUpXP()
		{
			return CalculateRemainingLevelUpXP(xpLevel, xp);
		}

		public int GetUniqueMessageId()
		{
			while (messagesById.ContainsKey(MsgIdCounter))
			{
				MsgIdCounter++;
			}
			return MsgIdCounter;
		}

		public bool RegisterUnitVisited(Unit unit)
		{
			if (unit != null && !visitedUnits.Contains(unit.UniqueId))
			{
				visitedUnits.Add(unit.UniqueId);
				return true;
			}
			return false;
		}

		public bool IsUnitVisited(Unit unit)
		{
			return visitedUnits.Contains(unit.UniqueId);
		}

		public void TrimInvalidVisitedUnits()
		{
			int[] array = visitedUnits.ToArray();
			visitedUnits.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				Unit unitByid = engine.GetUnitByid(array[i]);
				if (unitByid != null)
				{
					visitedUnits.Add(unitByid.UniqueId);
				}
			}
		}

		[ContextMenu("Visit all units")]
		public void VisitAllUnits()
		{
			EngineASX.Instance.EnumerateUnits((Unit unit) =>
			{
				RegisterUnitVisited(unit);
			});
		}

		public void AddMessage(MessageTemplate messageTemplate, bool notifications = true, bool important = false)
		{
			PlayerActiveMessage message = new PlayerActiveMessage
			{
				AllowDelete = true,
				MessageTemplate = messageTemplate,
				Opened = false
			};
			AddMessage(message, notifications, important);
		}

		public void AddMessage(string messageText, bool notifications = true, bool important = false)
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.MessageText = messageText;
			AddMessage(playerActiveMessage, notifications, important);
		}

		public void AddMessage(string from, string to, string subject, string messageText, bool notifications = true, bool important = false)
		{
			PlayerActiveMessage playerActiveMessage = new PlayerActiveMessage();
			playerActiveMessage.MessageText = messageText;
			playerActiveMessage.ToText = to;
			playerActiveMessage.FromText = from;
			playerActiveMessage.SubjectText = subject;
			AddMessage(playerActiveMessage, notifications, important);
		}

		public void AddMessage(PlayerActiveMessage message, bool notifications = true, bool important = false)
		{
			if (message == null)
			{
				Debug.LogError("Null message", this);
				return;
			}
			AddMessageInternal(message);
			if (notifications)
			{
				string subjectText = message.GetSubjectText();
				string msg = ((!string.IsNullOrWhiteSpace(subjectText)) ? ("New message: " + subjectText) : "Message received");
				UIController.Instance.QuickMsg.AddMessage(msg);
				if (important)
				{
					engine.MessageReceivedAudioSource.Play();
				}
				else
				{
					engine.MessageReceivedUnimportantAudioSource.Play();
				}
			}
		}

		public void AddMessageDelayed(PlayerActiveMessage message, float delay, bool notifications = true, bool important = false)
		{
			if (message == null)
			{
				Debug.LogError("Null message", this);
				return;
			}
			delayedMessage item = new delayedMessage
			{
				Message = message,
				ShowTime = engine.ScenarioElapsedTime + (double)delay,
				Notifications = notifications,
				Important = important
			};
			delayedMessages.Add(item);
		}

		public void RemoveMessage(PlayerActiveMessage message)
		{
			if (message != null && messagesById.Remove(message.UniqueId))
			{
				messageList.Remove(message);
				if (MessagedAddRemoved != null)
				{
					MessagedAddRemoved(this, message, added: false);
				}
			}
		}

		public void TrimUnimportantMessages()
		{
			int num = 0;
			int num2 = messageList.Count - 1;
			while (num2 >= 0 && num2 < messageList.Count)
			{
				PlayerActiveMessage playerActiveMessage = messageList[num2];
				if (playerActiveMessage.AllowDelete)
				{
					if (num == engine.GameSettings.PlayerMessagesMaxUnimportant)
					{
						RemoveMessage(playerActiveMessage);
					}
					else
					{
						num++;
					}
				}
				num2--;
			}
		}

		public PlayerActiveMessage GetMessageById(int id)
		{
			PlayerActiveMessage value = null;
			if (messagesById.TryGetValue(id, out value))
			{
				return value;
			}
			return null;
		}

		public void SetCustomWaypointToUnit(Unit unit, bool autoRemove = true, bool showMsg = true)
		{
			if (WorldHelper.AllowSetWaypointToUnit(unit))
			{
				SetCustomWaypoint(PlayerWaypoint.FromUnit(unit), autoRemove);
			}
		}

		public void SetCustomWaypointToSectorPosition(Sector sector, Vector3 sectorPosition, bool autoRemove)
		{
			SetCustomWaypoint(PlayerWaypoint.FromSectorPosition(sector, sectorPosition), autoRemove);
		}

		public void SetCustomWaypoint(PlayerWaypoint target)
		{
			SetCustomWaypoint(target, autoRemove: false);
		}

		public void SetCustomWaypoint(PlayerWaypoint target, bool autoRemove)
		{
			SetCustomWaypoint(target, autoRemove, showMsg: true);
		}

		public bool IsCustomWaypoint(Unit unit)
		{
			if (WaypointController.CustomPath.Waypoint.HasValue)
			{
				return WaypointController.CustomPath.Waypoint.Value.TargetUnit == unit;
			}
			return false;
		}

		public void ToggleCustomWaypoint(Unit unit)
		{
			if (IsCustomWaypoint(unit) || unit.IsPlayerCustomPathMarker())
			{
				ClearCustomWaypoint();
			}
			else
			{
				SetCustomWaypointToUnit(unit);
			}
		}

		public void SetCustomWaypoint(PlayerWaypoint target, bool autoRemove, bool showMsg)
		{
			if (target.TargetUnit != null && target.TargetUnit.Sector == null)
			{
				Debug.LogError("Setting waypoint to unit that has no scene", this);
			}
			if (target.TargetUnit != CustomUnitWaypoint)
			{
				ClearCustomWaypoint();
			}
			WaypointController.CustomPath.Waypoint = target;
			WaypointController.CustomPath.AutoRemoveWaypoint = autoRemove;
			if (showMsg)
			{
				UIController.Instance.QuickMsg.AddMessage($"Waypoint set to {GetWaypointString(target)}");
			}
		}

		public void ClearCustomWaypoint()
		{
			WaypointController.CustomPath.Waypoint = null;
		}

		public string GetWaypointString(PlayerWaypoint target)
		{
			string text = "Unknown";
			if (target.TargetUnit != null)
			{
				target.GetTargetSector();
				text = target.TargetUnit.GetFriendlyNameForLocalFaction();
				if (string.IsNullOrEmpty(text))
				{
					text = "Unknown";
				}
				Sector targetSector = target.GetTargetSector();
				if (targetSector != null)
				{
					text = text + " in " + targetSector.Name;
				}
			}
			else if (target.GetTargetSector() != null)
			{
				text = target.GetTargetSector().Name;
			}
			return text;
		}

		private void ShowLevelUpMessage()
		{
			UIController.Instance.QuickMsg.AddMessage($"You have reached level {xpLevel}");
		}

		private void Update()
		{
			for (int i = 0; i < delayedMessages.Count; i++)
			{
				if (engine.ScenarioElapsedTime > delayedMessages[i].ShowTime)
				{
					AddMessage(delayedMessages[i].Message, delayedMessages[i].Notifications, delayedMessages[i].Important);
					delayedMessages.RemoveAt(i);
				}
			}
		}

		private void AddMessageInternal(PlayerActiveMessage message)
		{
			if (message.UniqueId <= 0)
			{
				message.UniqueId = GetUniqueMessageId();
				MsgIdCounter++;
			}
			if (!messagesById.ContainsKey(message.UniqueId))
			{
				if (message.EngineTimeStamp == 0.0)
				{
					message.EngineTimeStamp = Engine.ScenarioElapsedTime;
				}
				messagesById.Add(message.UniqueId, message);
				messageList.Add(message);
				if (MessagedAddRemoved != null)
				{
					MessagedAddRemoved(this, message, added: true);
				}
			}
			else
			{
				Debug.LogError($"Cannot add message. Message with ID \"{message.UniqueId}\" has already been added", this);
			}
			TrimUnimportantMessages();
		}
	}
}
