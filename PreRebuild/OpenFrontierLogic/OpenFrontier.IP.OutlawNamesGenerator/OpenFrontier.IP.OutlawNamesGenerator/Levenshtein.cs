using System;

namespace OpenFrontier.IP.OutlawNamesGenerator
{
	public static class Levenshtein
	{
		public static int Compute(string s, string t)
		{
			int length = s.Length;
			int length2 = t.Length;
			int[,] array = new int[length + 1, length2 + 1];
			if (length == 0)
			{
				return length2;
			}
			if (length2 == 0)
			{
				return length;
			}
			int num = 0;
			while (num <= length)
			{
				array[num, 0] = num++;
			}
			int num2 = 0;
			while (num2 <= length2)
			{
				array[0, num2] = num2++;
			}
			for (int i = 1; i <= length; i++)
			{
				for (int j = 1; j <= length2; j++)
				{
					int num3 = ((t[j - 1] != s[i - 1]) ? 1 : 0);
					array[i, j] = Math.Min(Math.Min(array[i - 1, j] + 1, array[i, j - 1] + 1), array[i - 1, j - 1] + num3);
				}
			}
			return array[length, length2];
		}
	}
}
