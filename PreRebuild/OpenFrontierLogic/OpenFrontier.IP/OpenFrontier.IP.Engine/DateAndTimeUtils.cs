using System;
using System.Text;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class DateAndTimeUtils
	{
		public const float SecondsInOneGameDay = 1800f;

		public string DateFormat = "yyyy-MM-dd HH:mm";

		public string DateFormatWithArg = "{0:yyyy-MM-dd HH:mm}";

		private EngineASX engine;

		public double GameWorldElapsedSeconds => engine.ScenarioElapsedTime * (double)engine.GameSettings.GameTimeToRealTimeConversion;

		public DateTime GameWorldStartDate => engine.World.GetScenarioStartDateTime();

		public string FormattedGameWorldDate => GetGameWorldDateTime().ToString(DateFormat);

		public DateAndTimeUtils(EngineASX engine)
		{
			this.engine = engine;
		}

		public double GetGameWorldElapsedSeconds(double realSeconds)
		{
			return realSeconds * (double)engine.GameSettings.GameTimeToRealTimeConversion;
		}

		public TimeSpan GetGameWorldTimespanFromSeconds(double seconds, double otherSeconds)
		{
			return GetGameWorldTimespanFromSeconds(Math.Abs(seconds - otherSeconds));
		}

		public TimeSpan GetGameWorldTimespanFromSeconds(double readSeconds)
		{
			return TimeSpan.FromSeconds(GetGameWorldSeconds(readSeconds));
		}

		public double GetGameWorldSeconds(double realSeconds)
		{
			return realSeconds * (double)engine.GameSettings.GameTimeToRealTimeConversion;
		}

		public string GetShortTimespanDescriptionFromRealSeconds(double realSeconds)
		{
			double gameWorldSeconds = GetGameWorldSeconds(realSeconds);
			return GetShortTimespanDescription(TimeSpan.FromSeconds(gameWorldSeconds));
		}

		public string GetShortTimespanDescription(TimeSpan timeSpan)
		{
			int num = (int)timeSpan.TotalDays;
			if (num > 0)
			{
				if (num == 1)
				{
					return "1 day";
				}
				return $"{num:N0} days";
			}
			int num2 = (int)timeSpan.TotalHours;
			if (num2 > 0)
			{
				if (num2 == 1)
				{
					return "1 hour";
				}
				return $"{num2:N0} hours";
			}
			int num3 = (int)timeSpan.TotalMinutes;
			if (num3 > 0)
			{
				if (num3 == 1)
				{
					return "1 min";
				}
				return $"{num3:N0} mins";
			}
			int num4 = (int)timeSpan.TotalSeconds;
			if (num4 > 0)
			{
				if (num4 == 1)
				{
					return "1 sec";
				}
				return $"{num4:N0} secs";
			}
			return "now";
		}

		public int GetGameWorldElapsedDays()
		{
			return (int)TimeSpan.FromSeconds(GameWorldElapsedSeconds).TotalDays;
		}

		public int GetGameWorldDay()
		{
			int b = Mathf.CeilToInt((float)TimeSpan.FromSeconds(GameWorldElapsedSeconds).TotalDays);
			return Mathf.Max(1, b);
		}

		public int GetGameWorldDay(double realSecondsElapsed)
		{
			int b = Mathf.CeilToInt((float)TimeSpan.FromSeconds(GetGameWorldElapsedSeconds(realSecondsElapsed)).TotalDays);
			return Mathf.Max(1, b);
		}

		public DateTime GetGameWorldDateTimeFromElapsedRealSeconds(double realSeconds, DateTime gameStartDate)
		{
			double gameWorldElapsedSeconds = GetGameWorldElapsedSeconds(realSeconds);
			return gameStartDate.AddSeconds(gameWorldElapsedSeconds);
		}

		public DateTime GetGameWorldDateTime()
		{
			double gameWorldElapsedSeconds = GameWorldElapsedSeconds;
			return GetGameWorldDateTime(gameWorldElapsedSeconds);
		}

		public DateTime GetGameWorldDateTime(double gameWorldElapsedSeconds)
		{
			return GameWorldStartDate.AddSeconds(gameWorldElapsedSeconds);
		}

		public string GetFormattedGameWorldDateAndAgeFromRealElapsedSeconds(double realElapsedSeconds)
		{
			TimeSpan gameWorldTimespanFromSeconds = GetGameWorldTimespanFromSeconds(EngineASX.Instance.ScenarioElapsedTime - realElapsedSeconds);
			string shortTimespanDescription = GetShortTimespanDescription(gameWorldTimespanFromSeconds);
			string formattedEngineTimeStampFromRealElapsedSeconds = GetFormattedEngineTimeStampFromRealElapsedSeconds(realElapsedSeconds);
			return formattedEngineTimeStampFromRealElapsedSeconds + " " + shortTimespanDescription + " " + ((gameWorldTimespanFromSeconds.TotalSeconds > 0.0) ? "ago" : string.Empty);
		}

		public void GetFormattedGameWorldDateNonAlloc(StringBuilder stringBuilder)
		{
			DateTime gameWorldDateTime = GetGameWorldDateTime();
			stringBuilder.AppendFormat(DateFormatWithArg, gameWorldDateTime);
		}

		public void GetFormattedGameWorldDateAndDayNonAlloc(StringBuilder stringBuilder)
		{
			DateTime gameWorldDateTime = GetGameWorldDateTime();
			stringBuilder.AppendFormat(DateFormatWithArg, gameWorldDateTime);
			int gameWorldDay = EngineASX.Instance.DateTimeUtils.GetGameWorldDay();
			stringBuilder.AppendFormat(" (Day {0})", gameWorldDay);
		}

		public string GetFormattedEngineTimeStampFromRealElapsedSeconds(double realElapsedSeconds)
		{
			return GetGameWorldDateTime(GetGameWorldSeconds(realElapsedSeconds)).ToString(DateFormat);
		}
	}
}
