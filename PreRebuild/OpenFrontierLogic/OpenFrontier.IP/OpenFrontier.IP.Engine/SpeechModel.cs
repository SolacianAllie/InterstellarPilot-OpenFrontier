using System;
using System.Collections.Generic;
using OpenFrontier.IP.Engine.Dialog;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI;
using OpenFrontier.IP.UI.Screens.Hud;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine
{
	public class SpeechModel : MonoBehaviour
	{
		[Serializable]
		public class SpeechRequest
		{
			public bool AllowSkip;

			public DialogEvent DialogEvent;

			public bool Expires = true;

			public bool IsImportant = true;

			public float RequestTime;

			public float ShowTime;

			public Person SourcePerson;

			public Unit SourceUnit;

			public UnitClass SourceUnitClass;

			public Faction SourceFaction;

			public bool PlayWhenSourceNull;

			public string SourcePilotText;

			public string SourceText;

			public string Text;

			public float Timeout;

			public bool ShowTimeElapsed => Time.time > ShowTime;

			public float GetDistanceFromSource(HudScreen hud)
			{
				if (SourcePerson != null)
				{
					return Vector3.Distance(hud.PlayerUnit.transform.position, SourcePerson.transform.position);
				}
				return 0f;
			}

			public bool CanShow()
			{
				return Time.time > ShowTime;
			}

			public bool IsValid()
			{
				if (!(SourcePerson != null) || !(SourcePerson.Sector != null))
				{
					return PlayWhenSourceNull;
				}
				return true;
			}
		}

		private SpeechRequest activeRequest;

		public float ChangeRequestDist = 0.75f;

		public float DefaultSpeechRangeLower = 300f;

		public float DefaultSpeechRangeUpper = 500f;

		public GameObject DialogRoot;

		public Text DialogSourcePilotLabel;

		public Text DialogSourceLabel;

		public Text DialogTextLabel;

		public Button DismissDialogButton;

		public HudScreen Hud;

		public float MaxDelay = 2f;

		public float MaxMsgDuration = 20f;

		public int MessagesShownCount;

		public float MinDelay = 1f;

		public float MinMsgDuration = 3f;

		public float MsgTimePerChar = 0.2f;

		public float MinRequestShowDuration = 1.5f;

		private Dictionary<int, SpeechRequest> pilotToRequestMap = new Dictionary<int, SpeechRequest>();

		private List<SpeechRequest> requests = new List<SpeechRequest>();

		public GameObject SpeechIndicator;

		private float timeLastDialogRemoved;

		public Image ShipIcon;

		public bool UseAttitudeColors = true;

		public bool IsShowingSpeech => activeRequest != null;

		public SpeechRequest ActiveRequest
		{
			get
			{
				return activeRequest;
			}
			set
			{
				if (activeRequest == value)
				{
					return;
				}
				activeRequest = value;
				if (activeRequest != null)
				{
					MessagesShownCount++;
					if (activeRequest.DialogEvent != null)
					{
						Hud.Eng.NotifyDialogEventMsgShown(activeRequest.DialogEvent);
					}
				}
				DismissDialogButton.gameObject.SetActive(activeRequest != null && activeRequest.AllowSkip);
				UpdateDisplay();
				UpdateVisibility();
				if (activeRequest == null)
				{
					timeLastDialogRemoved = Time.time;
				}
			}
		}

		public float TimeLastDialogRemoved => timeLastDialogRemoved;

		public List<SpeechRequest> Requests => requests;

		public void Start()
		{
			DismissDialogButton.onClick.AddListener(DismissDialogButton_Activated);
			Hud.InGameMenuController.MenuChanged += InGameMenuController_MenuChanged;
			UpdateVisibility();
		}

		public void Update()
		{
			ExpireOldRequests();
			if (activeRequest == null || (!activeRequest.IsImportant && Time.time - activeRequest.ShowTime > MinRequestShowDuration))
			{
				SelectBestRequest();
			}
		}

		public void ClearRequests()
		{
			while (requests.Count > 0)
			{
				RemoveRequest(requests[0]);
			}
		}

		public string GetSourcePilotText(Person sourcePerson, Unit sourceUnit)
		{
			if (sourcePerson != null)
			{
				Faction faction = sourcePerson.Faction;
				string text = null;
				if (string.IsNullOrWhiteSpace(sourcePerson.GetShortNameElseLong()))
				{
					text = ((!(sourceUnit != null)) ? "Pilot" : sourceUnit.GetFriendlyName(shortName: true));
				}
				else
				{
					text = sourcePerson.ShortNameWithShortRank;
				}
				if (faction != null && !faction.IsFreelancer)
				{
					text = text + " (" + faction.GetShortNameElseLong() + ")";
				}
				return text;
			}
			return null;
		}

		public SpeechRequest AddRequest(Person sourcePerson, string message)
		{
			return AddRequest(sourcePerson, null, message);
		}

		public float GetRandomDelay()
		{
			return UnityEngine.Random.Range(MinDelay, MaxDelay);
		}

		public SpeechRequest AddRequest(Person source, string sourceStr, string message)
		{
			float randomDelay = GetRandomDelay();
			return AddRequest(source, sourceStr, message, randomDelay);
		}

		public SpeechRequest AddRequest(Person sourcePerson, string sourceDescription, string message, float delay)
		{
			int key = -1;
			if (sourcePerson != null)
			{
				key = sourcePerson.UniqueId;
			}
			SpeechRequest value = null;
			if (!pilotToRequestMap.TryGetValue(key, out value))
			{
				value = new SpeechRequest();
				requests.Add(value);
			}
			value.RequestTime = Time.time;
			value.SourcePerson = sourcePerson;
			if (value.SourcePerson != null)
			{
				value.SourceUnit = sourcePerson.CurrentUnit;
				if (value.SourceUnit != null)
				{
					value.SourceUnitClass = sourcePerson.CurrentUnit.UnitClass;
				}
			}
			if (value.SourceUnit != null && string.IsNullOrWhiteSpace(sourceDescription))
			{
				sourceDescription = value.SourceUnit.GetFriendlyName();
			}
			value.SourcePilotText = GetSourcePilotText(value.SourcePerson, value.SourceUnit);
			value.SourceText = sourceDescription;
			value.Text = message;
			value.Timeout = Time.time + delay + Mathf.Clamp(MsgTimePerChar * (float)(value.SourceText?.Length ?? 0), MinMsgDuration, MaxMsgDuration);
			value.ShowTime = Time.time + delay;
			pilotToRequestMap[key] = value;
			UpdateVisibility();
			return value;
		}

		public SpeechRequest GetRequestFromPilot(Person pilot)
		{
			SpeechRequest value = null;
			if (pilotToRequestMap.TryGetValue(pilot.UniqueId, out value))
			{
				return value;
			}
			return null;
		}

		public void RemoveRequestsFromPilot(Person pilot)
		{
			SpeechRequest value = null;
			if (pilotToRequestMap.TryGetValue(pilot.UniqueId, out value))
			{
				RemoveRequest(value);
			}
		}

		public void RemoveRequestsFromPilotInPast(Person pilot)
		{
			SpeechRequest value = null;
			if (pilotToRequestMap.TryGetValue(pilot.UniqueId, out value) && value.RequestTime < Time.time)
			{
				RemoveRequest(value);
			}
		}

		public void RemoveRequestsFromPilotWhereNotDeathMessage(Person pilot)
		{
			SpeechRequest value = null;
			if (pilotToRequestMap.TryGetValue(pilot.UniqueId, out value) && value.DialogEvent != EngineASX.Instance.DialogEvents.Destroyed)
			{
				RemoveRequest(value);
			}
		}

		private void DismissDialogButton_Activated()
		{
			if (activeRequest != null)
			{
				RemoveRequest(activeRequest);
			}
		}

		private void InGameMenuController_MenuChanged(InGameMenuControllerUI sender, InGameMenuBase oldMenu)
		{
			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			DialogRoot.gameObject.SetActive(IsShowingSpeech);
			SpeechIndicator.gameObject.SetActive(value: false);
		}

		private void SelectBestRequest()
		{
			ActiveRequest = SelectRequest();
		}

		private void ExpireOldRequests()
		{
			for (int i = 0; i < requests.Count; i++)
			{
				SpeechRequest speechRequest = requests[i];
				if (speechRequest.Expires && Time.time > speechRequest.Timeout)
				{
					RemoveRequest(speechRequest);
				}
			}
		}

		private SpeechRequest SelectRequest()
		{
			SpeechRequest result = null;
			float num = float.MaxValue;
			if (activeRequest != null && activeRequest.IsValid())
			{
				float distanceFromSource = activeRequest.GetDistanceFromSource(Hud);
				if (distanceFromSource < DefaultSpeechRangeUpper)
				{
					num = distanceFromSource;
					result = activeRequest;
				}
			}
			float a = num * ChangeRequestDist;
			for (int i = 0; i < requests.Count; i++)
			{
				SpeechRequest speechRequest = requests[i];
				if (speechRequest != activeRequest && speechRequest.CanShow() && speechRequest.IsValid())
				{
					if (speechRequest.IsImportant)
					{
						result = speechRequest;
						break;
					}
					float distanceFromSource2 = speechRequest.GetDistanceFromSource(Hud);
					float num2 = Mathf.Min(a, DefaultSpeechRangeLower);
					if (distanceFromSource2 < num2)
					{
						a = distanceFromSource2;
						result = speechRequest;
					}
				}
			}
			return result;
		}

		private void RemoveRequest(SpeechRequest s)
		{
			if (s.SourcePerson == null)
			{
				pilotToRequestMap.Remove(-1);
			}
			else
			{
				pilotToRequestMap.Remove(s.SourcePerson.UniqueId);
			}
			requests.Remove(s);
			if (s == activeRequest)
			{
				ActiveRequest = null;
			}
		}

		private void UpdateDisplay()
		{
			if (activeRequest != null)
			{
				DialogSourcePilotLabel.text = activeRequest.SourcePilotText;
				DialogSourceLabel.text = activeRequest.SourceText;
				DialogTextLabel.text = activeRequest.Text;
				if (UseAttitudeColors)
				{
					Color factionHostilityColor = Hud.Eng.GetFactionHostilityColor(activeRequest.SourceFaction, Hud.Eng.LocalFaction);
					DialogSourceLabel.color = factionHostilityColor;
					DialogSourcePilotLabel.color = factionHostilityColor;
				}
				ShipIcon.sprite = GetShipIconSprite(activeRequest.SourceUnitClass);
			}
			else
			{
				ShipIcon.sprite = null;
				DialogSourceLabel.text = null;
				DialogTextLabel.text = null;
			}
		}

		private Sprite GetShipIconSprite(UnitClass sourceUnitClass)
		{
			if ((bool)sourceUnitClass)
			{
				return EngineASX.Instance.EngineResources.GetUnitClassIconSpriteOrDefault(sourceUnitClass);
			}
			return null;
		}

		public SpeechRequest RequestSpeech(Person pilot, DialogEvent dialogEvent, CompiledDialogEventHandler eventHandler, float additionalDelay, DialogRequestArguments? args)
		{
			if (GetRequestFromPilot(pilot) == null)
			{
				SpeechRequest speechRequest = null;
				string message = GetMessage(eventHandler, args);
				float randomDelay = Hud.SpeechModel.GetRandomDelay();
				if (pilot.CurrentUnit != null && pilot.CurrentUnit.Components != null)
				{
					speechRequest = AddRequest(pilot, pilot.CurrentUnit.GetFriendlyName(), message, randomDelay + additionalDelay);
				}
				if (pilot != null)
				{
					speechRequest.SourceFaction = pilot.Faction;
				}
				speechRequest.DialogEvent = dialogEvent;
				speechRequest.PlayWhenSourceNull = speechRequest.DialogEvent.PlayWhenSourceNull;
				return speechRequest;
			}
			return null;
		}

		public string GetMessage(CompiledDialogEventHandler dialogEventHandler, DialogRequestArguments? args)
		{
			string text = dialogEventHandler.Messages.GetRandom();
			if (text != null)
			{
				if (args.HasValue && args.Value.KeyValues != null)
				{
					foreach (var keyValue in args.Value.KeyValues)
					{
						text = text.Replace("{" + keyValue.Item1 + "}", keyValue.Item2);
					}
				}
				return text;
			}
			return null;
		}
	}
}
