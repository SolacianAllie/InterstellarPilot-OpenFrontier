using System.Text;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class TextFormattingHelper
	{
		private static StringBuilder stringBuilder = new StringBuilder();

		public static string UnlimitedCargoText => "999";

		public static string GetColouredSceneNameAndDistance(Sector scene, Sector currentScene)
		{
			return UnityRichTextHelper.Color(GetSectorNameAndDistance(scene, currentScene), EngineASX.Instance.GameSettings.TextSceneColor);
		}

		public static string GetColouredSceneName(Sector scene)
		{
			return UnityRichTextHelper.Color((scene != null) ? scene.Name : "[Unknown sector]", GameController.Instance.GameSettings.TextSceneColor);
		}

		public static string FormatCargoAmount(int amount)
		{
			return FormatNumber(amount);
		}

		public static string FormatCargoVolume(float volume)
		{
			return volume.ToString("N1");
		}

		public static string FormatHullValue(float value)
		{
			return value.ToString("N0");
		}

		public static string FormatNumber(int num)
		{
			return num.ToString("N0");
		}

		public static string FormatNumber(float num)
		{
			return num.ToString("N0");
		}

		public static string FormatCredits(int money, bool includeSuffix = false)
		{
			if (includeSuffix)
			{
				return money.ToString("N0") + " Cr";
			}
			return money.ToString("N0");
		}

		public static string FormatCredits(long money, bool includeSuffix = false)
		{
			if (includeSuffix)
			{
				return money.ToString("N0") + " Cr";
			}
			return money.ToString("N0");
		}

		public static string FormatCreditsWithDashForZero(int money, bool includeSuffix = false)
		{
			if (money != 0)
			{
				return FormatCredits(money, includeSuffix);
			}
			return "-";
		}

		public static string GetSectorNameAndDistance(Sector sector, Sector currentSector)
		{
			if (currentSector != null)
			{
				int jumpDistanceTo = currentSector.GetJumpDistanceTo(sector);
				return GetSectorNameAndDistance(sector, jumpDistanceTo);
			}
			return sector.Name;
		}

		public static string GetSectorNameAndDistance(Sector sector, int? gateDist)
		{
			stringBuilder.Length = 0;
			stringBuilder.Append(sector.Name);
			if (gateDist.HasValue && gateDist > 0)
			{
				stringBuilder.Append(" (");
				stringBuilder.Concat(gateDist.Value);
				stringBuilder.Append(")");
			}
			return stringBuilder.ToString();
		}

		public static void FormatDistance(float dist, StringBuilder stringBuilder)
		{
			if (dist >= 1000f)
			{
				AppendRounded1(stringBuilder, dist / 1000f);
				stringBuilder.Append("k");
			}
			else
			{
				int int_val = (int)dist;
				stringBuilder.Concat(int_val);
			}
		}

		public static string FormatDistance(float dist)
		{
			return FormatNumberThousands(dist);
		}

		public static StringBuilder FormatDistanceNonAlloc(float dist)
		{
			return FormatNumberThousandsNonAlloc(dist);
		}

		public static StringBuilder FormatSpeedNonAlloc(float speed)
		{
			return FormatNumberThousandsNonAlloc(speed);
		}

		public static string FormatNumberThousands(float value)
		{
			stringBuilder.Length = 0;
			if (value >= 1000f)
			{
				AppendRounded1(stringBuilder, value / 1000f);
				stringBuilder.Append("k");
			}
			else
			{
				int int_val = (int)value;
				stringBuilder.Concat(int_val);
			}
			return stringBuilder.ToString();
		}

		public static StringBuilder FormatNumberThousandsNonAlloc(float value)
		{
			stringBuilder.Length = 0;
			if (value >= 1000f)
			{
				AppendRounded1(stringBuilder, value / 1000f);
				stringBuilder.Append("k");
			}
			else
			{
				int int_val = (int)value;
				stringBuilder.Concat(int_val);
			}
			return stringBuilder;
		}

		public static void AppendRounded1(StringBuilder stringBuilder, float val)
		{
			int num = (int)val;
			float num2 = val - (float)num;
			stringBuilder.Concat(num);
			stringBuilder.Append(".");
			int int_val = (int)(num2 * 10f);
			stringBuilder.Concat(int_val);
		}

		public static string WarningColor(string text, EngineASX engine)
		{
			return UnityRichTextHelper.Color(text, engine.TextWarningColor);
		}

		public static string FormatSectorPosition(Vector3 localSectorPosition)
		{
			return $"X: {localSectorPosition.x / 1000f:N2}k Y: {localSectorPosition.z / 1000f:N2}k";
		}

		public static void FormatSectorPositionNonAlloc(Vector3 localSectorPosition, StringBuilder stringBuilder)
		{
			stringBuilder.AppendFormat("X: {0:N2}k Y: {1:N2}k", localSectorPosition.x / 1000f, localSectorPosition.z / 1000f);
		}

		public static string FormatSectorAndPosition(Sector sector, Vector3 sectorPosition)
		{
			string text = FormatSectorPosition(sectorPosition);
			return sector.Name + ", {" + text + "}";
		}
	}
}
