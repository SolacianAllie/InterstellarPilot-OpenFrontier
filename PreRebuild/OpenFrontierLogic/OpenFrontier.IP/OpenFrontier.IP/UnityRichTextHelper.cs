using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using UnityEngine;

namespace OpenFrontier.IP
{
	public static class UnityRichTextHelper
	{
		public static string GetColoredFriendlyNameForLocalFaction(Unit unit, bool shortName = false)
		{
			return ColorFromOpinionOfLocal(unit.GetFriendlyName(shortName), unit.Faction);
		}

		public static string ColorFromOpinionOfLocal(string text, Unit unit)
		{
			Color factionHostilityColor = EngineASX.Instance.GetFactionHostilityColor((unit != null) ? unit.Faction : null, EngineASX.Instance.LocalFaction);
			return Color(text, factionHostilityColor);
		}

		public static string ColorFromOpinionOfLocal(string text, Faction otherFaction)
		{
			Color factionHostilityColor = EngineASX.Instance.GetFactionHostilityColor(otherFaction, EngineASX.Instance.LocalFaction);
			return Color(text, factionHostilityColor);
		}

		public static string ColorFromOpinion(string text, Faction ourFaction, Faction otherFaction)
		{
			Color factionHostilityColor = EngineASX.Instance.GetFactionHostilityColor(otherFaction, ourFaction);
			return Color(text, factionHostilityColor);
		}

		public static string Color(string text, Color color)
		{
			string arg = ColorToHex(color);
			return $"<color=#{arg}>{text}</color>";
		}

		public static string Bold(string text)
		{
			return $"<b>{text}</b>";
		}

		private static string ColorToHex(Color color)
		{
			int num = (int)(color.r * 255f);
			int num2 = (int)(color.g * 255f);
			int num3 = (int)(color.b * 255f);
			return $"{num:X2}{num2:X2}{num3:X2}";
		}
	}
}
