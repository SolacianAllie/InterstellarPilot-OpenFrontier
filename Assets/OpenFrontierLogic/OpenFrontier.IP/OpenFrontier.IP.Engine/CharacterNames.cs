using System.Collections.Generic;
using OpenFrontier.IP.Engine.CharacterNaming;
using OpenFrontier.IP.IO;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CharacterNames : MonoBehaviour
	{
		private struct CharacterFirstName
		{
			public string Value;

			public bool IsMale;

			public CountryOrigin Country;
		}

		private struct CharacterLastName
		{
			public string Value;

			public bool IsMale;

			public CountryOrigin Country;
		}

		private class CountryOfOriginWeighting : IWeighted
		{
			public CountryOrigin CountryOfOrigin { get; set; }

			public float Weight { get; set; }
		}

		private List<CharacterFirstName> firstNames = new List<CharacterFirstName>(200);

		private List<CharacterLastName> lastNames = new List<CharacterLastName>(200);

		private Dictionary<CountryOrigin, Dictionary<bool, List<int>>> firstNamesByCountryAndGender = new Dictionary<CountryOrigin, Dictionary<bool, List<int>>>();

		private Dictionary<CountryOrigin, List<int>> lastNamesByCountry = new Dictionary<CountryOrigin, List<int>>();

		public TextAsset FirstNamesFile;

		public TextAsset LastNamesFile;

		private List<CountryOfOriginWeighting> countryOrigins = new List<CountryOfOriginWeighting>();

		private bool hasInit;

		public void Init()
		{
			if (!hasInit)
			{
				ReadFirstNames(FirstNamesFile, firstNames);
				ReadLastNames(LastNamesFile, lastNames);
				countryOrigins.Add(new CountryOfOriginWeighting
				{
					CountryOfOrigin = CountryOrigin.English,
					Weight = 2f
				});
				countryOrigins.Add(new CountryOfOriginWeighting
				{
					CountryOfOrigin = CountryOrigin.Italian,
					Weight = 2f
				});
				countryOrigins.Add(new CountryOfOriginWeighting
				{
					CountryOfOrigin = CountryOrigin.Spanish,
					Weight = 2f
				});
				countryOrigins.Add(new CountryOfOriginWeighting
				{
					CountryOfOrigin = CountryOrigin.German,
					Weight = 2f
				});
				CompileNamesAndRandomize();
				hasInit = true;
			}
		}

		private void CompileNamesAndRandomize()
		{
			foreach (CountryOfOriginWeighting countryOrigin in countryOrigins)
			{
				firstNamesByCountryAndGender.Add(countryOrigin.CountryOfOrigin, new Dictionary<bool, List<int>>(100));
				bool[] array = new bool[2] { true, false };
				foreach (bool key in array)
				{
					firstNamesByCountryAndGender[countryOrigin.CountryOfOrigin].Add(key, new List<int>(50));
				}
				lastNamesByCountry.Add(countryOrigin.CountryOfOrigin, new List<int>(100));
			}
			for (int j = 0; j < firstNames.Count; j++)
			{
				CharacterFirstName characterFirstName = firstNames[j];
				firstNamesByCountryAndGender[firstNames[j].Country][characterFirstName.IsMale].Add(j);
			}
			for (int k = 0; k < lastNames.Count; k++)
			{
				lastNamesByCountry[lastNames[k].Country].Add(k);
			}
		}

		public bool TryGenerateName(bool isMale, out int firstNameIndex, out int lastNameIndex)
		{
			CountryOfOriginWeighting randomWeighted = countryOrigins.GetRandomWeighted();
			firstNameIndex = GetFirstNameIndex(randomWeighted.CountryOfOrigin, isMale);
			lastNameIndex = GetLastNameIndex(randomWeighted.CountryOfOrigin);
			if (firstNameIndex > -1 && lastNameIndex > -1)
			{
				return true;
			}
			return false;
		}

		public void GenerateName(Person person)
		{
			if (person.HasGeneratedName)
			{
				Debug.LogWarning($"Person {person} already has a name. Name will be overwritten", this);
			}
			int firstNameIndex = -1;
			int lastNameIndex = -1;
			if (TryGenerateName(person.IsMale, out firstNameIndex, out lastNameIndex))
			{
				person.GeneratedFirstNameId = firstNameIndex;
				person.GeneratedLastNameId = lastNameIndex;
				person.HasGeneratedName = true;
				person.RefreshName();
			}
		}

		private int GetFirstNameIndex(CountryOrigin countryOrigin, bool male)
		{
			if (firstNames.Count > 0)
			{
				List<int> value = null;
				Dictionary<bool, List<int>> value2 = null;
				if (firstNamesByCountryAndGender.TryGetValue(countryOrigin, out value2) && value2.TryGetValue(male, out value) && value.Count > 0)
				{
					int num = Random.Range(0, value.Count);
					int num2 = num;
					do
					{
						int num3 = value[num2];
						if (firstNames[num3].IsMale == male)
						{
							return num3;
						}
						num2++;
						if (num2 >= value.Count)
						{
							num2 = 0;
						}
					}
					while (num2 != num);
					return -1;
				}
			}
			return -1;
		}

		private int GetLastNameIndex(CountryOrigin countryOrigin)
		{
			if (firstNames.Count > 0)
			{
				List<int> value = null;
				if (lastNamesByCountry.TryGetValue(countryOrigin, out value) && value.Count > 0)
				{
					return value.GetRandom();
				}
			}
			return -1;
		}

		public string GetName(int firstNameIndex, int lastNameIndex)
		{
			if (firstNameIndex > -1 && lastNameIndex > -1 && firstNameIndex < firstNames.Count && lastNameIndex < lastNames.Count)
			{
				return firstNames[firstNameIndex].Value + " " + lastNames[lastNameIndex].Value;
			}
			return null;
		}

		private CountryOrigin GetCountryOfOriginFromText(string text)
		{
			return text switch
			{
				"ENG" => CountryOrigin.English, 
				"ITA" => CountryOrigin.Italian, 
				"ESP" => CountryOrigin.Spanish, 
				"GER" => CountryOrigin.German, 
				_ => CountryOrigin.Unknown, 
			};
		}

		private void ReadFirstNames(TextAsset t, List<CharacterFirstName> names)
		{
			string[] array = IOHelper.ReadAndSplitTextLines(t);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(',');
				CountryOrigin countryOfOriginFromText = GetCountryOfOriginFromText(array2[1]);
				if (countryOfOriginFromText != CountryOrigin.Unknown)
				{
					CharacterFirstName item = new CharacterFirstName
					{
						Value = array2[0],
						Country = countryOfOriginFromText,
						IsMale = (array2[2] != "F")
					};
					names.Add(item);
				}
				else
				{
					Debug.LogError("Found unknown country of origin in names: " + array2[1]);
				}
			}
		}

		private void ReadLastNames(TextAsset t, List<CharacterLastName> names)
		{
			string[] array = IOHelper.ReadAndSplitTextLines(t);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(',');
				CountryOrigin countryOfOriginFromText = GetCountryOfOriginFromText(array2[1]);
				if (countryOfOriginFromText != CountryOrigin.Unknown)
				{
					CharacterLastName item = new CharacterLastName
					{
						Value = array2[0],
						Country = countryOfOriginFromText
					};
					names.Add(item);
				}
				else
				{
					Debug.LogError("Found unknown country of origin in names: " + array2[1]);
				}
			}
		}
	}
}
