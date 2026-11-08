using System;
using System.Text;
using UnityEngine;

namespace OpenFrontier.IP.Discord
{
	/// <summary>
	/// Maps an in-game unit class (or ship series) to the name of a
	/// Discord activity asset uploaded for it.
	/// </summary>
	[Serializable]
	public struct DiscordImageMapping
	{
		[Tooltip("The unit's class name or series name exactly as the game shows it - e.g. 'Orbital Farm', 'Sector HQ'.")]
		public string className;

		[Tooltip("The Discord activity asset key uploaded for it - e.g. 'factory', 'outpost'.")]
		public string assetKey;
	}

	/// <summary>
	/// Discord Social SDK configuration. Create one asset and fill in
	/// the Application ID (Client ID) of the game's Discord application
	/// from https://discord.com/developers/applications.
	/// Rich Presence stays offline while the ID is unset.
	/// </summary>
	[CreateAssetMenu(fileName = "DiscordPresenceConfig", menuName = "Open Frontier/Discord Presence Config")]
	public class DiscordPresenceConfig : ScriptableObject
	{
		[Tooltip("The Discord application's Client ID (Application ID).")]
		public ulong ApplicationId;

		[Tooltip("Maps in-game unit classes to Discord activity assets. Ship series need no entry (the asset key is derived from the series name, e.g. 'Drake' -> drake); stations and player-built structures do.")]
		public DiscordImageMapping[] imageMappings = Array.Empty<DiscordImageMapping>();

		/// <summary>
		/// Finds the activity asset key for a unit: an explicit mapping
		/// wins, otherwise the obvious convention applies (the class or
		/// series name lowercased without spaces - which is exactly how
		/// the uploaded ship icons are named). Returns null when nothing
		/// matches, so presence simply shows no small image.
		/// </summary>
		public string ResolveImageKey(string seriesName, string className)
		{
			if (imageMappings != null)
			{
				for (int i = 0; i < imageMappings.Length; i++)
				{
					string assetKey = imageMappings[i].assetKey;
					if (string.IsNullOrEmpty(assetKey))
					{
						continue;
					}
					if (Matches(imageMappings[i].className, seriesName)
						|| Matches(imageMappings[i].className, className))
					{
						return assetKey;
					}
				}
			}
			string derived = Normalize(seriesName);
			if (string.IsNullOrEmpty(derived))
			{
				derived = Normalize(className);
			}
			return string.IsNullOrEmpty(derived) ? null : derived;
		}

		private static bool Matches(string mapped, string candidate)
		{
			return !string.IsNullOrEmpty(mapped)
				&& !string.IsNullOrEmpty(candidate)
				&& string.Equals(mapped.Trim(), candidate.Trim(), StringComparison.OrdinalIgnoreCase);
		}

		private static string Normalize(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}
			var builder = new StringBuilder(name.Length);
			for (int i = 0; i < name.Length; i++)
			{
				char c = name[i];
				if (char.IsLetterOrDigit(c))
				{
					builder.Append(char.ToLowerInvariant(c));
				}
			}
			return builder.ToString();
		}
	}
}
