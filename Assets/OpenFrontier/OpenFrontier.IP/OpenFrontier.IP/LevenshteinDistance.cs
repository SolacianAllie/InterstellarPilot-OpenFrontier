using System;

namespace OpenFrontier.IP
{
	public static class LevenshteinDistance
	{
		private const int maxStringLength = 256;

		private static int[,] d = new int[256, 256];

		public static int Dist(string s, string t)
		{
			int length = s.Length;
			int length2 = t.Length;
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
				d[num, 0] = num++;
			}
			int num2 = 0;
			while (num2 <= length2)
			{
				d[0, num2] = num2++;
			}
			for (int i = 1; i <= length; i++)
			{
				for (int j = 1; j <= length2; j++)
				{
					int num3 = ((t[j - 1] != s[i - 1]) ? 1 : 0);
					d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + num3);
				}
			}
			return d[length, length2];
		}
	}
}
