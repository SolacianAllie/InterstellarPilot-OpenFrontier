using UnityEngine;

namespace OpenFrontier.IP
{
	public static class PlayerPrefsHelper
	{
		public static float SafeGetFloat(string key, float defaultValue)
		{
			float num = PlayerPrefs.GetFloat(key, defaultValue);
			if (float.IsNaN(num))
			{
				return defaultValue;
			}
			return num;
		}
	}
}
