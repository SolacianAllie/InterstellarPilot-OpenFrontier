using System;
using UnityEngine;

namespace OpenFrontier.IP
{
	public static class Versioning
	{
		private static Version version;

		// Open Frontier: the version comes from the project's bundle version
		// (Application.version / PlayerSettings), so the displayed and saved
		// version always matches the build - one number to bump.
		public static Version Version
		{
			get
			{
				if (version == null)
				{
					version = Parse(Application.version);
				}
				return version;
			}
		}

		private static Version Parse(string text)
		{
			try
			{
				return new Version(text);
			}
			catch (Exception)
			{
				return new Version(0, 0, 1);
			}
		}

		public static bool IsAlphaVersion = true;

		public static bool IsBetaVersion = false;

		public static bool IsLegacyVersion = false;

		// The tag shown next to the version number (menu display); empty for
		// release builds. If several flags are set, Alpha > Beta > Legacy.
		public static string StageTag
		{
			get
			{
				if (IsAlphaVersion)
				{
					return " Alpha";
				}
				if (IsBetaVersion)
				{
					return " Beta";
				}
				if (IsLegacyVersion)
				{
					return " Legacy";
				}
				return "";
			}
		}
	}
}
