using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.IO;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Bars
{
	public class BarNamesController : MonoBehaviour
	{
		public TextAsset NamesFile;

		private List<BarName> nextBarNames = new List<BarName>(10);

		private List<BarName> allNames = new List<BarName>(30);

		public int CacheCount = 20;

		public void LoadNames()
		{
			allNames = (from e in IOHelper.ReadAndSplitTextLines(NamesFile)
				where !string.IsNullOrWhiteSpace(e)
				select ParseBarName(e)).ToList();
		}

		private BarName ParseBarName(string e)
		{
			string[] array = e.Split(",");
			return new BarName(array[0], array[1]);
		}

		public void CacheNewNames()
		{
			nextBarNames.Clear();
			HashSet<string> namesInUse = GetNamesInUse().ToHashSet();
			List<BarName> list = allNames.Where((BarName e) => !namesInUse.Contains(e.Name)).ToList();
			while (nextBarNames.Count < CacheCount && list.Count > 0)
			{
				int index = Random.Range(0, list.Count);
				BarName item = list[index];
				nextBarNames.Add(item);
				list.RemoveAt(index);
			}
		}

		public List<string> GetNamesInUse()
		{
			List<string> list = new List<string>(20);
			foreach (Sector sector in EngineASX.Instance.Sectors)
			{
				List<Unit> unitsByType = sector.GetUnitsByType(UnitType.Station);
				if (unitsByType == null)
				{
					continue;
				}
				foreach (Unit item in unitsByType)
				{
					if (item.UnitClass.StationPurpose == StationPurpose.Bar)
					{
						list.Add(item.UnitName);
					}
				}
			}
			return list;
		}

		public BarName? PeekNextUniqueBarName()
		{
			if (nextBarNames.Count == 0)
			{
				CacheNewNames();
			}
			if (nextBarNames.Count > 0)
			{
				return nextBarNames[nextBarNames.Count - 1];
			}
			return null;
		}

		public BarName? GetAndRemoveUniqueBarName()
		{
			if (nextBarNames.Count == 0)
			{
				CacheNewNames();
			}
			if (nextBarNames.Count > 0)
			{
				BarName value = nextBarNames[nextBarNames.Count - 1];
				nextBarNames.RemoveAt(nextBarNames.Count - 1);
				return value;
			}
			return null;
		}
	}
}
