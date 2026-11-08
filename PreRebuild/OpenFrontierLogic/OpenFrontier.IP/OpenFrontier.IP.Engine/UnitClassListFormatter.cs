using System.Collections.Generic;
using System.Text;

namespace OpenFrontier.IP.Engine
{
	public static class UnitClassListFormatter
	{
		private struct UnitClassCountItem
		{
			public UnitClass UnitClass;

			public int Count;
		}

		private class ShipInfoComparer : IComparer<UnitClassCountItem>
		{
			public int Compare(UnitClassCountItem x, UnitClassCountItem y)
			{
				return -x.UnitClass.maxHealth.CompareTo(y.UnitClass.maxHealth);
			}
		}

		public static void FormatList(StringBuilder stringBuilder, UnitClass[] unitClasses, int startIndex)
		{
			FormatList(stringBuilder, unitClasses, startIndex, excludeCountForSingleItems: true, colorEncoding: false);
		}

		public static void FormatList(StringBuilder stringBuilder, UnitClass[] unitClasses, int startIndex, bool excludeCountForSingleItems, bool colorEncoding)
		{
			List<UnitClassCountItem> list = new List<UnitClassCountItem>(4);
			for (int i = startIndex; i < unitClasses.Length; i++)
			{
				UnitClass unitClass = unitClasses[i];
				int num = -1;
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j].UnitClass == unitClass)
					{
						num = j;
						break;
					}
				}
				if (num > -1)
				{
					UnitClassCountItem value = list[num];
					value.Count++;
					list[num] = value;
				}
				else
				{
					list.Add(new UnitClassCountItem
					{
						Count = 1,
						UnitClass = unitClass
					});
				}
			}
			if (list.Count > 0)
			{
				list.Sort(new ShipInfoComparer());
				for (int k = 0; k < list.Count; k++)
				{
					string text = list[k].UnitClass.GetClassAndSeriesName();
					if (colorEncoding)
					{
						text = UnityRichTextHelper.Color(text, GameController.Instance.GameSettings.TextUnitClassColor);
					}
					if (list[k].Count > 1 || !excludeCountForSingleItems)
					{
						stringBuilder.AppendLine($"{list[k].Count}x {text}");
					}
					else
					{
						stringBuilder.AppendLine(text);
					}
				}
			}
			else
			{
				stringBuilder.AppendLine("-");
			}
		}
	}
}
