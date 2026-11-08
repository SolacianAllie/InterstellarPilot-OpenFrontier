using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.IO;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using Random = System.Random;

namespace OpenFrontier.IP.Engine
{
	public class ShipNames : MonoBehaviour
	{
		private ShipName[] names;

		private Dictionary<int, ShipName> namesbyId = new Dictionary<int, ShipName>(2000);

		public TextAsset NamesFile;

		private List<int> unassignedShipNameIds = new List<int>();

		private HashSet<int> reservedShipNameIds = new HashSet<int>();

		public int ReservedNameCount => reservedShipNameIds.Count;

		public int FreeNameCount => unassignedShipNameIds.Count;

		public void Load()
		{
			LoadNamesFromFile();
			unassignedShipNameIds.Clear();
			foreach (ShipName item in names.Where((ShipName e) => e.Enabled))
			{
				unassignedShipNameIds.Add(item.Id);
				namesbyId.Add(item.Id, item);
			}
		}

		public void ClearAndRandomize(System.Random random)
		{
			unassignedShipNameIds.Shuffle(random);
			reservedShipNameIds.Clear();
		}

		public bool IsReserved(int id)
		{
			return reservedShipNameIds.Contains(id);
		}

		public bool GetAndReserveUniqueShipName(ShipNameNeutraility neutralityFlags, ShipNameUsage usageFlags, out string foundName, out int foundId)
		{
			foundName = null;
			foundId = -1;
			if (names.Length != 0 && unassignedShipNameIds.Count > 0)
			{
				for (int num = unassignedShipNameIds.Count - 1; num >= 0; num--)
				{
					int num2 = unassignedShipNameIds[num];
					ShipName shipName = namesbyId[num2];
					if ((neutralityFlags & shipName.Neutraility) != 0 && (usageFlags & shipName.Usage) != 0)
					{
						foundName = shipName.Name;
						foundId = shipName.Id;
						unassignedShipNameIds.RemoveAt(num);
						reservedShipNameIds.Add(num2);
						return true;
					}
				}
			}
			return false;
		}

		public string GetNonUniqueShipName(ShipNameNeutraility neutralityFlags, ShipNameUsage usageFlags)
		{
			if (names.Length != 0)
			{
				int num = UnityEngine.Random.Range(0, names.Length);
				int num2 = num;
				bool flag = false;
				while (!flag)
				{
					if ((neutralityFlags & names[num2].Neutraility) != 0 && (usageFlags & names[num2].Usage) != 0)
					{
						return names[num2].Name;
					}
					num2++;
					if (num2 == names.Length)
					{
						num2 = 0;
					}
					if (num2 == num)
					{
						break;
					}
				}
			}
			return null;
		}

		public void ReserveShipName(int id)
		{
			if (!IsReserved(id))
			{
				reservedShipNameIds.Add(id);
				unassignedShipNameIds.Remove(id);
			}
		}

		public void UnreserveShipName(int id)
		{
			if (id > -1 && id < names.Length && IsReserved(id))
			{
				reservedShipNameIds.Remove(id);
				unassignedShipNameIds.Add(id);
			}
		}

		public string GetShipNameById(int id)
		{
			if (namesbyId.TryGetValue(id, out var value))
			{
				return value.Name;
			}
			return null;
		}

		private void LoadNamesFromFile()
		{
			if (!(NamesFile != null))
			{
				return;
			}
			List<ShipName> list = new List<ShipName>(2000);
			string[] array = IOHelper.ReadAndSplitTextLines(NamesFile);
			for (int i = 1; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(',');
				if (array2.Length == 5)
				{
					ShipName item = new ShipName
					{
						Id = int.Parse(array2[0]),
						Name = array2[1]
					};
					bool result = true;
					if (!bool.TryParse(array2[2], out result))
					{
						result = true;
					}
					item.Enabled = result;
					int result2 = 0;
					if (int.TryParse(array2[3], out result2))
					{
						item.Neutraility = (ShipNameNeutraility)result2;
					}
					else
					{
						item.Neutraility = ShipNameNeutraility.Neutral;
					}
					int result3 = 0;
					if (int.TryParse(array2[4], out result3))
					{
						item.Usage = (ShipNameUsage)result3;
					}
					else
					{
						item.Usage = ShipNameUsage.Unspecified;
					}
					list.Add(item);
				}
				else
				{
					Debug.LogWarning("Skipping ship name: " + array[i]);
				}
			}
			names = list.ToArray();
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"Loaded {names.Length} ship names", this, 1);
			}
		}

		public bool ContainsShipName(string value)
		{
			return names.Any((ShipName e) => string.Equals(value, e.Name, StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
