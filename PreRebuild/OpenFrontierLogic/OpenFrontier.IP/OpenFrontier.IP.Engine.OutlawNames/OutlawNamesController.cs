using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.IO;
using OpenFrontier.IP.OutlawNamesGenerator;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenFrontier.IP.Engine.OutlawNames
{
	public class OutlawNamesController : MonoBehaviour
	{
		public TextAsset AdjectivesFile;

		public TextAsset GroupNamesFile;

		public TextAsset NameListFile;

		private List<GeneratedPirateName> nextNames = new List<GeneratedPirateName>(10);

		public int CacheCount = 20;

		private PirateNameData pirateNameData;

		public PirateNameData PirateNameData => pirateNameData;

		public void LoadNames()
		{
			pirateNameData = Load();
		}

		private PirateNameData Load()
		{
			PirateNameData pirateNameData = new PirateNameData();
			int count = 0;
			pirateNameData.Adjectives = LoadLines(AdjectivesFile).ToArray();
			pirateNameData.GroupNames = LoadLines(GroupNamesFile).Skip(1).Select((string e) =>
			{
				string[] array = e.Split(",");
				bool result = false;
				bool.TryParse(array[1], out result);
				return new PirateGroupName
				{
					Name = array[0],
					Gender = Enum.Parse<GenderChoice>(array[2]),
					CanMixWithCount = result
				};
			}).ToList();
			List<PirateName> names = LoadLines(NameListFile).Skip(1).Select((string e) =>
			{
				string[] array = e.Split(",");
				if (array.Length != 5)
				{
					throw new Exception($"Invalid data on line {count + 2}");
				}
				bool result = false;
				bool.TryParse(array[4], out result);
				count++;
				return new PirateName
				{
					SingularName = array[0],
					PluralName = array[1],
					Gender = Enum.Parse<GenderChoice>(array[2]),
					AllowNameAfterCount = result,
					PrefixType = Enum.Parse<PirateNamePrefixType>(array[3])
				};
			}).ToList();
			pirateNameData.Names = names;
			return pirateNameData;
		}

		private IEnumerable<string> LoadLines(TextAsset textAsset)
		{
			return (from e in IOHelper.ReadAndSplitTextLines(textAsset)
				where !string.IsNullOrWhiteSpace(e)
				select e).ToList();
		}

		public void CacheNewNames()
		{
			nextNames.Clear();
			HashSet<string> namesInUse = GetNamesInUse().ToHashSet();
			List<GeneratedPirateName> list = (from e in new PirateNameGenerator().Generate(pirateNameData, CacheCount)
				where !namesInUse.Contains(e.Name)
				select e).ToList();
			while (nextNames.Count < CacheCount && list.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, list.Count);
				GeneratedPirateName item = list[index];
				nextNames.Add(item);
				list.RemoveAt(index);
			}
		}

		public List<string> GetNamesInUse()
		{
			List<string> list = new List<string>(20);
			foreach (Faction faction in EngineASX.Instance.Factions)
			{
				if (faction.FactionType == FactionType.Outlaw && !faction.IsFreelancer)
				{
					list.Add(faction.Name);
				}
			}
			return list;
		}

		public GeneratedPirateName PeekNextUniqueName()
		{
			if (nextNames.Count == 0)
			{
				CacheNewNames();
			}
			if (nextNames.Count > 0)
			{
				return nextNames[nextNames.Count - 1];
			}
			return null;
		}

		public GeneratedPirateName GetAndRemoveUniqueName()
		{
			if (nextNames.Count == 0)
			{
				CacheNewNames();
			}
			if (nextNames.Count > 0)
			{
				GeneratedPirateName result = nextNames[nextNames.Count - 1];
				nextNames.RemoveAt(nextNames.Count - 1);
				return result;
			}
			return null;
		}
	}
}
