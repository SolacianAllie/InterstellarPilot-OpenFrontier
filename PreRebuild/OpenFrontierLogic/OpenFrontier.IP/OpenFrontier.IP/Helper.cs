using System.Text;
using UnityEngine;

namespace OpenFrontier.IP
{
	public static class Helper
	{
		private static StringBuilder str = new StringBuilder();

		public static string Pluralise(string str, int count)
		{
			if (count != 1)
			{
				return str + "s";
			}
			return str;
		}

		public static ulong PairId(int x, int y)
		{
			if (x <= y)
			{
				return (ulong)((uint)x | ((long)y << 32));
			}
			return (ulong)((uint)y | ((long)x << 32));
		}

		public static ulong OrderedPairId(int x, int y)
		{
			return (ulong)((uint)x | ((long)y << 32));
		}

		public static float ToRoundedInteger(float value, float rounding)
		{
			return (float)(int)(value / rounding) * rounding;
		}

		public static int BoolToInt(bool val)
		{
			if (!val)
			{
				return 0;
			}
			return 1;
		}

		public static bool IntToBool(int val)
		{
			return val != 0;
		}

		public static int BoolTo01(bool val)
		{
			if (val)
			{
				return 1;
			}
			return 0;
		}

		public static Color ColorFromRgb(float r, float g, float b)
		{
			return new Color(r / 255f, g / 255f, b / 255f);
		}

		public static Color ColorFromRgba(float r, int g, float b, float a)
		{
			return new Color(r / 255f, (float)g / 255f, b / 255f, a / 255f);
		}

		public static string BoolToYesNo(bool p)
		{
			if (!p)
			{
				return "No";
			}
			return "Yes";
		}
	}
}
