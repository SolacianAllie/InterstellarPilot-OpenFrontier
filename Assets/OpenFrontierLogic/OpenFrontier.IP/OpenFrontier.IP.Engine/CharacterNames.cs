using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.CharacterNaming;
using OpenFrontier.IP.IO;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

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

		private struct CharacterEpithet
		{
			public string Value;

			/// <summary>
			/// True when the epithet applies to any gender rather than only one.
			/// "the Baroness" must never be handed to a male pilot.
			/// </summary>
			public bool AnyGender;

			public bool IsMale;
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

		public TextAsset EpithetsFile;

		/// <summary>
		/// Fraction of pilots given an epithet instead of a surname.
		/// </summary>
		/// <remarks>
		/// Deliberately low. An epithet-bearing pilot reads "Jeff the Bagel
		/// Thief" - no surname, because a nickname replaces one - so these land
		/// as the memorable exception rather than the rule.
		///
		/// 0.28 also keeps total name variety level. A plain pilot draws
		/// given x surname; an epithet-bearing one draws given x epithet
		/// instead, because the surname is dropped. With 577 given and 350
		/// surnames, the epithet pool has to hold around 365 rows to break
		/// even - below that the pool gets smaller, not bigger.
		/// </remarks>
		private const float EpithetChance = 0.28f;

		private List<CharacterEpithet> epithets = new List<CharacterEpithet>(400);

		private List<CountryOfOriginWeighting> countryOrigins = new List<CountryOfOriginWeighting>();

		private bool hasInit;

		public void Init()
		{
			if (!hasInit)
			{
				ReadFirstNames(FirstNamesFile, firstNames);
				ReadLastNames(LastNamesFile, lastNames);
				ReadEpithets(EpithetsFile, epithets);
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

		/// <summary>
		/// Stable per-pair value in [0,1). Deterministic, so a pilot keeps the
		/// same epithet for the life of a save and old saves acquire one without
		/// anything new being written.
		/// </summary>
		/// <remarks>
		/// Derived from firstNameIndex and lastNameIndex only, because those are
		/// the two values actually persisted: SaveGameWriter writes both in every
		/// version and SaveGameModelImporter170 reads them back into
		/// person.generatedFirstNameId / generatedLastNameId. A saved epithet id
		/// would need a new field in the save format and would invalidate every
		/// existing world. Hashing the pair costs nothing and needs no schema
		/// change.
		///
		/// UnityEngine.Random is deliberately NOT used - it is global mutable
		/// state, and the answer has to be the same every time the same pair is
		/// asked about, from any call site, in any order.
		/// </remarks>
		private static float StableHash01(int firstNameIndex, int lastNameIndex)
		{
			unchecked
			{
				uint h = (uint)(firstNameIndex + 1) * 2654435761u;
				h ^= (uint)(lastNameIndex + 1) * 2246822519u;
				h ^= h >> 15;
				h *= 2654435761u;
				h ^= h >> 13;
				return (h & 0xFFFFFFu) / 16777216f;
			}
		}

		/// <summary>
		/// Which epithet, if any, belongs to this pair of name indices.
		/// </summary>
		/// <param name="isMale">Gender of the chosen given name, so a
		/// gender-restricted epithet such as "the Baroness" cannot land on a
		/// male pilot.</param>
		private int GetEpithetIndex(int firstNameIndex, int lastNameIndex, bool isMale)
		{
			if (epithets.Count == 0)
			{
				return -1;
			}

			float h = StableHash01(firstNameIndex, lastNameIndex);
			if (h >= EpithetChance)
			{
				return -1;
			}

			// A second, independent draw picks which one, so the gate and the
			// choice are not correlated with each other.
			int pick = (int)((h / EpithetChance) * epithets.Count) % epithets.Count;
			CharacterEpithet candidate = epithets[pick];
			if (!candidate.AnyGender && candidate.IsMale != isMale)
			{
				// Restricted and wrong gender: walk forward to the next entry
				// that fits rather than dropping the epithet entirely.
				for (int i = 1; i < epithets.Count; i++)
				{
					CharacterEpithet alt = epithets[(pick + i) % epithets.Count];
					if (alt.AnyGender || alt.IsMale == isMale)
					{
						return (pick + i) % epithets.Count;
					}
				}
			}
			return pick;
		}

		/// <summary>
		/// Whether this pair of indices carries an epithet rather than a surname.
		/// </summary>
		public bool HasEpithet(int firstNameIndex, int lastNameIndex)
		{
			return firstNameIndex > -1 && firstNameIndex < firstNames.Count &&
				GetEpithetIndex(firstNameIndex, lastNameIndex,
					firstNames[firstNameIndex].IsMale) >= 0;
		}

		public string GetName(int firstNameIndex, int lastNameIndex)
		{
			if (firstNameIndex > -1 && lastNameIndex > -1 && firstNameIndex < firstNames.Count && lastNameIndex < lastNames.Count)
			{
				string given = firstNames[firstNameIndex].Value;
				string surname = lastNames[lastNameIndex].Value;
				int epithetIndex = GetEpithetIndex(firstNameIndex, lastNameIndex,
					firstNames[firstNameIndex].IsMale);
				if (epithetIndex >= 0)
				{
					// Quoted, and the SURNAME IS KEPT:
					//     Todd "The Blaster" McQuinn
					// An earlier version dropped the surname, on the reasoning
					// that a nickname replaces one. That was wrong twice over -
					// it cost the pool its largest dimension, and it lost the
					// part that makes a nickname read as a nickname. Keeping it
					// turns epithet names into given x surname x epithet rather
					// than given x epithet, which is the difference between a
					// pool that stays level and one that grows by three orders
					// of magnitude.
					return given + " \"" + CapitaliseEpithet(epithets[epithetIndex].Value)
						+ "\" " + surname;
				}
				return given + " " + surname;
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

		/// <summary>
		/// Reads "Epithet,Gender".
		/// </summary>
		/// <remarks>
		/// Unlike the name files this one has a header and is skipped, and
		/// unlike ReadLastNames an unknown gender is a hard drop rather than a
		/// logged one: a missing epithet is cosmetic, so it is better to lose the
		/// row than to spam the log once per pilot.
		/// </remarks>
		private void ReadEpithets(TextAsset t, List<CharacterEpithet> into)
		{
			if (t == null)
			{
				return;
			}

			foreach (string line in IOHelper.ReadAndSplitTextLines(t))
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				string[] parts = line.Split(',');
				if (parts.Length < 2)
				{
					continue;
				}

				if (Enum.TryParse(parts[1], out GenderChoice gender))
				{
					into.Add(new CharacterEpithet
					{
						Value = parts[0],
						IsMale = gender != GenderChoice.Female,
						AnyGender = gender == GenderChoice.Unspecified
					});
				}
			}
		}

		/// <summary>
		/// "the Bagel Thief" -&gt; "The Bagel Thief".
		/// </summary>
		/// <remarks>
		/// The pool stores them lowercase because that is how they read standing
		/// alone as a nickname. Inside quotation marks a leading article wants
		/// capitalising, or it looks like the quote continues into the name.
		/// </remarks>
		private static string CapitaliseEpithet(string epithet)
		{
			if (epithet != null && epithet.StartsWith("the ", StringComparison.Ordinal))
			{
				return "The " + epithet.Substring(4);
			}
			return epithet;
		}

		/// <summary>
		/// Short form for an epithet-bearing pilot, or null when the pilot has
		/// an ordinary name and the caller's own shortening applies.
		/// </summary>
		/// <remarks>
		/// Person.ShortenName initialises every word but the last, which would
		/// turn Todd "The Blaster" McQuinn into T. "The Blaster" McQuinn - the
		/// epithet would be the only thing left standing in a tight label. So the
		/// epithet is simply dropped here and the given name kept whole:
		/// "Todd McQuinn".
		/// </remarks>
		public string GetShortName(int firstNameIndex, int lastNameIndex)
		{
			if (firstNameIndex < 0 || firstNameIndex >= firstNames.Count ||
				lastNameIndex < 0 || lastNameIndex >= lastNames.Count)
			{
				return null;
			}

			bool isMale = firstNames[firstNameIndex].IsMale;
			return GetEpithetIndex(firstNameIndex, lastNameIndex, isMale) >= 0
				? firstNames[firstNameIndex].Value + " " + lastNames[lastNameIndex].Value
				: null;
		}
	}
}
