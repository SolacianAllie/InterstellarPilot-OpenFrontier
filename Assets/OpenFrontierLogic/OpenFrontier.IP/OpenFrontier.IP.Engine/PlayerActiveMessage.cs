using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class PlayerActiveMessage
	{
		public bool AllowDelete = true;

		public double EngineTimeStamp;

		public string FromText;

		public MessageTemplate MessageTemplate;

		public string MessageText;

		public bool Opened;

		public Unit SenderUnit;

		public Sector SenderUnitSector;

		public Vector3 SenderUnitSectorPosition;

		public string SubjectText;

		public Unit SubjectUnit;

		public Sector SubjectUnitSector;

		public Vector3 SubjectUnitSectorPosition;

		public string ToText;

		public int UniqueId;

		public const string PlayerEnvVar = "#player#";

		public void SetSubjectUnitAndPosition(Unit unit)
		{
			SubjectUnit = unit;
			if (SubjectUnit != null)
			{
				SubjectUnitSector = SubjectUnit.Sector;
				SubjectUnitSectorPosition = SubjectUnit.SectorPosition;
			}
			else
			{
				SubjectUnitSector = null;
				SubjectUnitSectorPosition = Vector3.zero;
			}
		}

		public void SetSenderUnitAndPosition(Unit unit)
		{
			SenderUnit = unit;
			if (SenderUnit != null)
			{
				SenderUnitSector = SenderUnit.Sector;
				SenderUnitSectorPosition = SenderUnit.SectorPosition;
			}
			else
			{
				SenderUnitSector = null;
				SenderUnitSectorPosition = Vector3.zero;
			}
		}

		public string GetToText()
		{
			if (MessageTemplate != null)
			{
				return MessageTemplate.ToText;
			}
			return ToText;
		}

		public string GetFromText()
		{
			if (MessageTemplate != null)
			{
				return MessageTemplate.FromText;
			}
			return FromText;
		}

		public string GetMessageText()
		{
			if (MessageTemplate != null)
			{
				return MessageTemplate.MessageText;
			}
			return MessageText;
		}

		public string GetSubjectText()
		{
			if (MessageTemplate != null)
			{
				return MessageTemplate.SubjectText;
			}
			return SubjectText;
		}

		public string GetFriendlyFromText()
		{
			Unit senderUnit = SenderUnit;
			string fromText = GetFromText();
			if (!string.IsNullOrEmpty(fromText))
			{
				return fromText;
			}
			if (senderUnit != null && senderUnit.Components != null && senderUnit.Components.PilotPerson != null)
			{
				return senderUnit.Components.PilotPerson.GetShortNameAndFaction();
			}
			return "Unknown";
		}

		public string GetFriendlyToText()
		{
			string toText = GetToText();
			if (!string.IsNullOrEmpty(toText))
			{
				return toText.Replace("#player#", EngineASX.Instance.LocalPlayer.Person.GetNameAndTitleOrFullRank(shortName: false));
			}
			return "-";
		}

		public string GetFriendlySubjectText()
		{
			string subjectText = GetSubjectText();
			if (!string.IsNullOrEmpty(subjectText))
			{
				return subjectText;
			}
			return "[No Subject]";
		}
	}
}
