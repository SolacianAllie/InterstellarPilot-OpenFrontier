using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP
{
	public static class Drawing
	{
		public static Color MultiLevelColorLerp(IList<Color> colors, float val)
		{
			if (val <= 0f)
			{
				return colors[0];
			}
			if (val >= 1f || colors.Count == 1)
			{
				return colors[colors.Count - 1];
			}
			int num = (int)(val * (float)(colors.Count - 1));
			float num2 = 1f / (float)(colors.Count - 1);
			return Color.Lerp(colors[num], colors[num + 1], (val - (float)num * num2) / num2);
		}
	}
}
