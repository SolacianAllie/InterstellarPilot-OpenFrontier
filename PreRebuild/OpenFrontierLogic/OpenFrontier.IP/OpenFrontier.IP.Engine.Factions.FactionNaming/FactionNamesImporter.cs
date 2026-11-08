using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.IO;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Factions.FactionNaming
{
	public class FactionNamesImporter
	{
		public List<FactionNames.FactionName> ReadNames(TextAsset t)
		{
			string[] array = IOHelper.ReadAndSplitTextLines(t);
			List<FactionNames.FactionName> list = new List<FactionNames.FactionName>(300);
			for (int i = 0; i < array.Length; i++)
			{
				if (i > 0)
				{
					string[] values = array[i].Split(',');
					FactionNames.FactionName? factionName = ReadFactionName(values, i);
					if (factionName.HasValue)
					{
						list.Add(factionName.Value);
					}
				}
			}
			return list;
		}

		private FactionNames.FactionName? ReadFactionName(string[] values, int lineNumberForLogging)
		{
			try
			{
				int id = int.Parse(values[0]);
				string value = values[1].Trim();
				bool result = true;
				if (!bool.TryParse(values[2], out result))
				{
					result = true;
				}
				float result2 = 0f;
				if (!float.TryParse(values[3], out result2))
				{
					result2 = 0f;
				}
				float result3 = 0f;
				if (!float.TryParse(values[4], out result3))
				{
					result3 = 0f;
				}
				if (!string.IsNullOrEmpty(value))
				{
					FactionType factionTypeFlags = TryParseFactionFlags(values, 5);
					return new FactionNames.FactionName
					{
						Id = id,
						Value = value,
						FactionTypeFlags = factionTypeFlags,
						Enabled = result,
						MinVirtue = result2,
						MaxVirtue = result3
					};
				}
			}
			catch (Exception innerException)
			{
				Debug.LogException(new Exception($"Could not read faction name at line {lineNumberForLogging}", innerException));
			}
			return null;
		}

		private FactionType TryParseFactionFlags(string[] values, int startColumnIndex)
		{
			FactionType factionType = FactionType.None;
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex], factionType, FactionType.Trader);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 1], factionType, FactionType.Scavenger);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 2], factionType, FactionType.Miner);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 3], factionType, FactionType.BountyHunter);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 4], factionType, FactionType.StationBuilder);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 5], factionType, FactionType.Empire);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 6], factionType, FactionType.Bandit);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 7], factionType, FactionType.PassengerTransport);
			factionType |= TryParseFactionTypeFlag(values[startColumnIndex + 8], factionType, FactionType.Explorer);
			return factionType | TryParseFactionTypeFlag(values[startColumnIndex + 9], factionType, FactionType.Generic);
		}

		public List<FactionNames.FactionSuffix> ReadSuffixes(TextAsset textAsset)
		{
			string[] array = IOHelper.ReadAndSplitTextLines(textAsset);
			List<FactionNames.FactionSuffix> list = new List<FactionNames.FactionSuffix>(50);
			for (int i = 0; i < array.Length; i++)
			{
				if (i > 0)
				{
					FactionNames.FactionSuffix? factionSuffix = ReadFactionSuffix(array[i], i);
					if (factionSuffix.HasValue)
					{
						list.Add(factionSuffix.Value);
					}
				}
			}
			return list;
		}

		private FactionNames.FactionSuffix? ReadFactionSuffix(string line, int lineNumberForLogging)
		{
			try
			{
				string[] array = line.Split(',');
				int id = int.Parse(array[0]);
				string value = array[1].Trim();
				bool result = true;
				if (!bool.TryParse(array[3], out result))
				{
					result = true;
				}
				float result2 = 0f;
				float.TryParse(array[4], out result2);
				float result3 = 0f;
				float.TryParse(array[5], out result3);
				float result4 = 1f;
				float.TryParse(array[6], out result4);
				FactionType factionTypeFlags = TryParseFactionFlags(array, 7);
				return new FactionNames.FactionSuffix
				{
					Id = id,
					Value = value,
					ShortName = array[2],
					Rarity = result4,
					FactionTypeFlags = factionTypeFlags,
					MinVirtue = result2,
					MaxVirtue = result3,
					Enabled = result
				};
			}
			catch (Exception innerException)
			{
				Debug.LogException(new Exception($"Could not read faction suffix at line {lineNumberForLogging}", innerException));
			}
			return null;
		}

		private FactionType TryParseFactionTypeFlag(string text, FactionType existingFlags, FactionType newFlag)
		{
			if (TryParseBoolFromIntString(text))
			{
				return existingFlags | newFlag;
			}
			return existingFlags;
		}

		private bool TryParseBoolFromIntString(string text)
		{
			int result = 0;
			if (int.TryParse(text, out result))
			{
				return result == 1;
			}
			return false;
		}
	}
}
