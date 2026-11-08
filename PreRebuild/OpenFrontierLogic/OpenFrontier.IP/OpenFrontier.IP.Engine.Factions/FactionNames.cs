using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine.Factions.FactionNaming;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine.Factions
{
	public class FactionNames : MonoBehaviour
	{
		public struct FactionName
		{
			public int Id;

			public string Value;

			public FactionType FactionTypeFlags;

			public bool Enabled;

			public float MinVirtue;

			public float MaxVirtue;
		}

		public struct FactionSuffix
		{
			public int Id;

			public string Value;

			public string ShortName;

			public FactionType FactionTypeFlags;

			public float Rarity;

			public bool Enabled;

			public float MinVirtue;

			public float MaxVirtue;
		}

		private List<FactionName> names;

		private List<FactionSuffix> suffixes;

		private Dictionary<int, FactionName> namesById = new Dictionary<int, FactionName>(200);

		private Dictionary<int, FactionSuffix> suffixesById = new Dictionary<int, FactionSuffix>(200);

		public TextAsset NamesFile;

		public TextAsset SuffixesFile;

		private bool hasInit;

		private HashSet<int> reservedSuffixIds = new HashSet<int>();

		private List<int> unassignedFactionNameIds = new List<int>();

		private HashSet<int> reservedFactionNameIds = new HashSet<int>();

		private int[] suffixIdCache;

		public int ReservedNameCount => reservedFactionNameIds.Count;

		public int FreeNameCount => unassignedFactionNameIds.Count;

		public IEnumerable<FactionName> Names => names;

		public void Init()
		{
			if (hasInit)
			{
				return;
			}
			FactionNamesImporter factionNamesImporter = new FactionNamesImporter();
			names = factionNamesImporter.ReadNames(NamesFile);
			foreach (FactionName name in names)
			{
				if (!namesById.ContainsKey(name.Id))
				{
					namesById.Add(name.Id, name);
				}
			}
			suffixes = factionNamesImporter.ReadSuffixes(SuffixesFile);
			foreach (FactionSuffix suffix in suffixes)
			{
				if (!suffixesById.ContainsKey(suffix.Id))
				{
					suffixesById.Add(suffix.Id, suffix);
				}
			}
			suffixIdCache = new int[suffixes.Count];
			unassignedFactionNameIds.Clear();
			foreach (FactionName item in names.Where((FactionName e) => e.Enabled))
			{
				unassignedFactionNameIds.Add(item.Id);
			}
			hasInit = true;
		}

		public void ClearAndRandomize(System.Random random)
		{
			unassignedFactionNameIds.Shuffle(random);
			reservedFactionNameIds.Clear();
		}

		public bool TryGenerateAndAssignName(Faction faction, FactionType factionType)
		{
			if (!TryGenerateAndAssignName(faction, factionType, ensureUnique: true))
			{
				EngineASX.Instance.DebugInfo.NumTimesFailedToGiveFactionUniqueName++;
				return TryGenerateAndAssignName(faction, factionType, ensureUnique: false);
			}
			return true;
		}

		public bool TryGenerateAndAssignName(Faction faction, FactionType factionType, bool ensureUnique)
		{
			faction.ClearGeneratedName();
			int foundId = -1;
			if (!ensureUnique || !GetAndReserveUniqueFactionName(factionType, faction.Virtue, out var _, out foundId))
			{
				foundId = GetNonUniqueRandomNameId(factionType, faction.Virtue);
			}
			if (foundId > -1)
			{
				string value = namesById[foundId].Value;
				int randomSuffixId = GetRandomSuffixId(ensureUnique, value, factionType, faction.Virtue);
				string nameById = GetNameById(foundId, randomSuffixId);
				string shortNameById = GetShortNameById(foundId, randomSuffixId);
				if (!string.IsNullOrEmpty(nameById) && !string.IsNullOrEmpty(shortNameById))
				{
					faction.Name = nameById;
					faction.ShortName = shortNameById;
					faction.GeneratedNameId = foundId;
					faction.GeneratedSuffixId = randomSuffixId;
					return true;
				}
			}
			return false;
		}

		public void SetNameAndShortNameFromGeneratedValues(Faction faction)
		{
			faction.Name = GetNameById(faction.GeneratedNameId, faction.GeneratedSuffixId);
			faction.ShortName = GetShortNameById(faction.GeneratedNameId, faction.GeneratedSuffixId);
		}

		public bool GetAndReserveUniqueFactionName(FactionType factionTypeFlags, float factionVirtue, out string foundName, out int foundId)
		{
			foundName = null;
			foundId = -1;
			if (names.Count > 0 && unassignedFactionNameIds.Count > 0)
			{
				for (int num = unassignedFactionNameIds.Count - 1; num >= 0; num--)
				{
					int num2 = unassignedFactionNameIds[num];
					FactionName factionName = namesById[num2];
					if (factionName.Enabled && (factionTypeFlags & factionName.FactionTypeFlags) == factionTypeFlags && factionVirtue >= factionName.MinVirtue && factionVirtue <= factionName.MaxVirtue)
					{
						foundName = factionName.Value;
						foundId = factionName.Id;
						unassignedFactionNameIds.RemoveAt(num);
						reservedFactionNameIds.Add(num2);
						return true;
					}
				}
			}
			return false;
		}

		private int GetNonUniqueRandomNameId(FactionType factionTypeFlags, float factionVirtue = 0.5f)
		{
			if (names.Count > 0)
			{
				int num = UnityEngine.Random.Range(0, names.Count);
				int num2 = num;
				do
				{
					FactionName factionName = names[num];
					if (factionName.Enabled && (factionTypeFlags & factionName.FactionTypeFlags) == factionTypeFlags && factionVirtue >= factionName.MinVirtue && factionVirtue <= factionName.MaxVirtue)
					{
						return factionName.Id;
					}
					num++;
					if (num >= names.Count)
					{
						num = 0;
					}
				}
				while (num != num2);
			}
			return -1;
		}

		private int GetRandomSuffixId(bool ensureUnique, string factionName, FactionType factionTypeFlags, float factionVirtue = 0.5f)
		{
			int num = 0;
			if (suffixes.Count > 0)
			{
				foreach (FactionSuffix suffix in suffixes)
				{
					if (suffix.Enabled && (!ensureUnique || !reservedSuffixIds.Contains(suffix.Id)) && (factionTypeFlags & suffix.FactionTypeFlags) == factionTypeFlags && factionVirtue >= suffix.MinVirtue && factionVirtue <= suffix.MaxVirtue && !factionName.Contains(suffix.Value, StringComparison.InvariantCultureIgnoreCase))
					{
						suffixIdCache[num++] = suffix.Id;
					}
				}
			}
			if (num > 0)
			{
				return suffixIdCache[UnityEngine.Random.Range(0, num)];
			}
			return -1;
		}

		public string GetNameById(int nameId, int suffixId)
		{
			if (namesById.TryGetValue(nameId, out var value) && suffixesById.TryGetValue(suffixId, out var value2))
			{
				return value.Value + " " + value2.Value;
			}
			return null;
		}

		public string GetShortNameById(int nameId, int suffixId)
		{
			if (namesById.TryGetValue(nameId, out var value))
			{
				return value.Value;
			}
			return null;
		}

		public string GetSuffixById(int suffixId)
		{
			if (suffixesById.TryGetValue(suffixId, out var value))
			{
				return value.Value;
			}
			return null;
		}

		public string GetShortSuffixById(int suffixId)
		{
			if (suffixesById.TryGetValue(suffixId, out var value))
			{
				return value.ShortName ?? value.Value;
			}
			return null;
		}

		public bool IsAssigned(int id)
		{
			return reservedFactionNameIds.Contains(id);
		}

		public void ReserveName(int id)
		{
			if (!reservedFactionNameIds.Contains(id))
			{
				reservedFactionNameIds.Add(id);
				unassignedFactionNameIds.Remove(id);
			}
		}

		public void FreeName(int id)
		{
			if (IsAssigned(id))
			{
				reservedFactionNameIds.Remove(id);
				unassignedFactionNameIds.Add(id);
			}
		}

		public void ReserveSuffix(int generatedSuffixId)
		{
			if (!reservedSuffixIds.Contains(generatedSuffixId))
			{
				reservedSuffixIds.Add(generatedSuffixId);
			}
		}

		public void FreeSuffix(int generatedNameId)
		{
			reservedSuffixIds.Remove(generatedNameId);
		}
	}
}
