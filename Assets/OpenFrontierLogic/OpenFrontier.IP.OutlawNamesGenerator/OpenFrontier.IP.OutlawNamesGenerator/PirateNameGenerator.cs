using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Common;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.OutlawNamesGenerator
{
	public class PirateNameGenerator
	{
		private static StringBuilder stringBuilder = new StringBuilder();

		private static TextInfo textInfo = new CultureInfo("en-GB", useUserOverride: false).TextInfo;

		public static GeneratedPirateName GenerateName(PirateNameData pirateNameData, int? personCount, GenderChoice genderChoice, float probabilityOfAdjective)
		{
			GeneratedPirateName generatedPirateName = new GeneratedPirateName();
			stringBuilder.Length = 0;
			string value = null;
			if (personCount.HasValue)
			{
				value = textInfo.ToTitleCase(pirateNameData.CountNames[personCount.Value - 1] ?? "");
			}
			PirateName pirateName = pirateNameData.Names.Where((PirateName e) => (!personCount.HasValue || e.AllowNameAfterCount) && (e.Gender == genderChoice || e.Gender == GenderChoice.Unspecified)).GetRandom();
			string text = null;
			if (pirateName.PrefixType == PirateNamePrefixType.Force || (pirateName.PrefixType == PirateNamePrefixType.Allow && UnityEngine.Random.value < probabilityOfAdjective))
			{
				text = pirateNameData.Adjectives.Where((string e) => CanUseAdjective(e, pirateName)).GetRandom();
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.Append(text ?? "");
			}
			string value2 = null;
			bool flag = UnityEngine.Random.value < 0.35f;
			if ((genderChoice != GenderChoice.Unspecified && pirateName.Gender == GenderChoice.Unspecified) || (!pirateName.SingularName.Contains("-") & flag))
			{
				PirateGroupName random = pirateNameData.GroupNames.Where((PirateGroupName e) => (!personCount.HasValue || e.CanMixWithCount) && CanMixGroupGenderWithNameGender(genderChoice, pirateName.Gender, e.Gender)).GetRandom();
				if (random == null)
				{
					if (!string.IsNullOrWhiteSpace(text))
					{
						stringBuilder.Append(" ");
					}
					stringBuilder.Append(pirateName.PluralName);
				}
				else
				{
					value2 = random.Name;
					if (!string.IsNullOrWhiteSpace(text))
					{
						stringBuilder.Append("-");
						stringBuilder.Append(pirateName.SingularName.ToLower());
						stringBuilder.Append(" " + random.Name);
					}
					else
					{
						stringBuilder.Append(pirateName.SingularName);
						stringBuilder.Append(" " + random.Name);
					}
				}
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					stringBuilder.Append(" ");
				}
				stringBuilder.Append(pirateName.PluralName);
			}
			if ((string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(value2)) && pirateName.PluralName.Length < 10)
			{
				generatedPirateName.ShortName = pirateName.PluralName;
			}
			else
			{
				generatedPirateName.ShortName = CreateAbbeviatedShortName(stringBuilder.ToString());
			}
			if (!string.IsNullOrWhiteSpace(value))
			{
				stringBuilder.Insert(0, " ");
				stringBuilder.Insert(0, value);
			}
			generatedPirateName.Name = stringBuilder.ToString();
			return generatedPirateName;
		}

		private static string CreateAbbeviatedShortName(string name)
		{
			if (name.Contains(' ') || name.Contains('-'))
			{
				return new string((from e in name.Split(new char[2] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
					select char.ToUpper(e[0])).ToArray());
			}
			return name;
		}

		private static bool CanMixGroupGenderWithNameGender(GenderChoice preference, GenderChoice nameGender, GenderChoice groupGender)
		{
			if (preference == GenderChoice.Unspecified)
			{
				return groupGender == GenderChoice.Unspecified;
			}
			if (nameGender == GenderChoice.Unspecified)
			{
				return groupGender == preference;
			}
			return groupGender == GenderChoice.Unspecified;
		}

		private static bool CanUseAdjective(string prefix, PirateName pirateName)
		{
			if (prefix.Length >= 3 && pirateName.SingularName.Length >= 3 && string.Equals(prefix.Substring(0, 3), pirateName.SingularName.Substring(0, 3), StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}
			return true;
		}

		public List<GeneratedPirateName> Generate(PirateNameData pirateNameData, int numberToGenerate = 64)
		{
			float probabilityOfAdjective = 0.4f;
			float num = 0.15f;
			int minInclusive = 2;
			int maxExclusive = 5;
			List<GeneratedPirateName> list = new List<GeneratedPirateName>();
			HashSet<string> hashSet = new HashSet<string>();
			_ = new CultureInfo("en-GB", useUserOverride: false).TextInfo;
			while (list.Count < numberToGenerate)
			{
				GeneratedPirateName generatedPirateName = GenerateName(pirateNameData, (UnityEngine.Random.value < num) ? new int?(UnityEngine.Random.Range(minInclusive, maxExclusive)) : ((int?)null), GenderChoice.Unspecified, probabilityOfAdjective);
				if (!hashSet.Contains(generatedPirateName.Name))
				{
					hashSet.Add(generatedPirateName.Name);
					list.Add(generatedPirateName);
				}
			}
			return list;
		}
	}
}
