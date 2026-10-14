using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// The music, and what is known about it.
	/// </summary>
	/// <remarks>
	/// The timings come from MusicTimingTable, which is generated from the
	/// audio by Tools/music/generate_music_timings.py. Nothing here measures
	/// anything: by the time the game runs, the answer to "where does this
	/// track start coming down" is already written down, and the only thing
	/// left to do is load the clip.
	///
	/// Categories, and what plays in each:
	///
	///   Anarchic / Federal / Imperial / Allied / Neutral   space, by family
	///   Starport                                          the family that owns it
	///   Threat / Engagement                               combat, by intensity
	///   Hypersleep                                       the superluminal drive
	///   Menu                                             title and menus
	///
	/// A starport is stored under its own category with the family kept
	/// alongside, because there are four starports and eleven space tracks -
	/// they are not the same list at different lengths.
	/// </remarks>
	public static class MusicLibrary
	{
		/// <summary>
		/// Held back, deliberately, until capital ships arrive at a warzone.
		/// </summary>
		/// <remarks>
		/// The high intensity engagement intro is the sting that plays as a
		/// capital ship comes in. It is in the table because the file exists
		/// and the timings were measured like any other, and it is excluded
		/// here because it is not a section - it is the thing that happens
		/// before a section, and nothing in the game produces a capital ship
		/// yet.
		///
		/// Excluded by name rather than by deleting it, so it is still
		/// measured and still regenerates cleanly.
		/// </remarks>
		public const string ReservedCombatIntroName = "Combat High Intensity Engagement Intro";

		private static readonly Dictionary<string, AudioClip> clips =
			new Dictionary<string, AudioClip>();

		private static readonly List<MusicTimingTable.Entry> scratch =
			new List<MusicTimingTable.Entry>();

		/// <summary>Every track, as measured.</summary>
		public static List<MusicTimingTable.Entry> Tracks => MusicTimingTable.Tracks;

		/// <summary>
		/// Tracks in a category, in a stable order.
		/// </summary>
		/// <remarks>
		/// The order is the generator's, which is alphabetical by name - so
		/// combat sections come out 1, 2, 3 and the sequencer walks them in
		/// that order rather than shuffling. Ambient tracks are picked at
		/// random from this list instead.
		///
		/// RETURNS A SHARED BUFFER, VALID UNTIL THE NEXT CALL. The list is
		/// reused rather than allocated because this runs every frame and a
		/// fresh list per frame is a needless allocation in a game that is
		/// already memory-tight. The cost is that a caller which keeps the
		/// list across another query sees it change underneath - which is why
		/// the combat sequencer copies out of it rather than holding it. Copy
		/// if you need to keep it.
		/// </remarks>
		public static List<MusicTimingTable.Entry> InCategory(string category)
		{
			scratch.Clear();

			for (int i = 0; i < MusicTimingTable.Tracks.Count; i++)
			{
				MusicTimingTable.Entry entry = MusicTimingTable.Tracks[i];

				if (entry.Category == category)
				{
					scratch.Add(entry);
				}
			}

			return scratch;
		}

		/// <summary>
		/// The starport track for a family, if that family has one.
		/// </summary>
		/// <remarks>
		/// Null for Neutral, because there is no Neutral starport. A station
		/// in unowned space falls back to Neutral ambience, which is the
		/// same answer the space state gives for that sector - so docking
		/// somewhere nobody owns needs no special case anywhere.
		/// </remarks>
		public static MusicTimingTable.Entry StarportOf(MusicFamily family)
		{
			string wanted = MusicFactionClassifier.NameOf(family);

			for (int i = 0; i < MusicTimingTable.Tracks.Count; i++)
			{
				MusicTimingTable.Entry entry = MusicTimingTable.Tracks[i];

				if (entry.Category == "Starport" && entry.Family == wanted)
				{
					return entry;
				}
			}

			return null;
		}

		/// <summary>
		/// The main menu track.
		/// </summary>
		/// <remarks>
		/// Not a random pick from the Menu category, because the category
		/// holds exactly one track and randomising it would be a way of
		/// spelling "there might be others" while there are not.
		///
		/// The menu plays Starbound and nothing else, and loops it. That was
		/// already true - EngineMusicPlayerController hardcoded
		/// "music_starbound" for the menu, looped it, and played no other
		/// track there - and it is now true of the new system too. The first
		/// run of this played "E D Menus" instead, because two loose files in
		/// the music folder root were being classified as Menu by the same
		/// rule that classifies a folder, and they have since been moved to
		/// Unassigned, where nothing plays them until somebody says what they
		/// are for.
		/// </remarks>
		public static MusicTimingTable.Entry MenuTrack()
		{
			for (int i = 0; i < MusicTimingTable.Tracks.Count; i++)
			{
				MusicTimingTable.Entry entry = MusicTimingTable.Tracks[i];

				if (entry.Category == "Menu")
				{
					return entry;
				}
			}

			return null;
		}

		/// <summary>Every playable combat section of one kind and intensity.</summary>
		/// <remarks>
		/// The reserved intro is filtered out here rather than at each call
		/// site, because a sequencer that accidentally included it would play a
		/// 22 second sting in the middle of a firefight.
		/// </remarks>
		public static List<MusicTimingTable.Entry> CombatSections(string category, string intensity)
		{
			scratch.Clear();

			for (int i = 0; i < MusicTimingTable.Tracks.Count; i++)
			{
				MusicTimingTable.Entry entry = MusicTimingTable.Tracks[i];

				if (entry.Category != category || !entry.IsCombatSection)
				{
					continue;
				}

				if (entry.Name == ReservedCombatIntroName)
				{
					continue;
				}

				// The intensity is in the name, because that is where it is in
				// the file: "Combat High Intensity Engagement Section 3".
				if (!entry.Name.Contains(intensity))
				{
					continue;
				}

				scratch.Add(entry);
			}

			return scratch;
		}

		/// <summary>
		/// A track from a category at random, avoiding the one playing.
		/// </summary>
		/// <remarks>
		/// Order is entirely random, which means the same track can come round
		/// again later - there is no least-played-first weighting here. That is
		/// deliberate and it is the opposite of what the old jukebox did, which
		/// counted plays and always picked the least-played. With eight to
		/// twelve tracks per family, strict no-repeat means the obvious
		/// rotation you can hear coming; random means a repeat can land in the
		/// middle of a visit and go unnoticed.
		///
		/// Returns null when the category is empty, which the caller must
		/// handle: silence is the right answer to "there is no music for this
		/// and nothing is playing", and the caller can keep whatever it had.
		/// </remarks>
		public static MusicTimingTable.Entry RandomFrom(
			string category, MusicTimingTable.Entry notThis)
		{
			List<MusicTimingTable.Entry> candidates = InCategory(category);

			if (candidates.Count == 0)
			{
				return null;
			}

			if (candidates.Count == 1)
			{
				return candidates[0];
			}

			for (int attempt = 0; attempt < 8; attempt++)
			{
				MusicTimingTable.Entry pick =
					candidates[Random.Range(0, candidates.Count)];

				if (pick != notThis)
				{
					return pick;
				}
			}

			// Every attempt hit the one playing, which needs eight categories
			// of one. Not possible, but returning something beats returning
			// null and silencing the game.
			return candidates[0];
		}

		/// <summary>The clip for an entry, loaded once and kept.</summary>
		/// <remarks>
		/// Resources.Load with the path the generator recorded, so the two
		/// cannot drift: the script writes "Music/Allied Space/Allied Space
		/// Alioth" and this asks for exactly that.
		/// </remarks>
		public static AudioClip ClipFor(MusicTimingTable.Entry entry)
		{
			if (entry == null)
			{
				return null;
			}

			if (clips.TryGetValue(entry.ResourcePath, out AudioClip cached) && cached != null)
			{
				return cached;
			}

			AudioClip clip = Resources.Load<AudioClip>(entry.ResourcePath);

			if (clip == null)
			{
				// Said once per path rather than silently playing nothing,
				// because a missing clip and an empty category look identical
				// from the outside and mean very different things.
				Debug.LogWarning("Music: no clip at Resources/" + entry.ResourcePath
					+ " (track '" + entry.Name + "'). Nothing will play for it.");
			}
			else
			{
				clips[entry.ResourcePath] = clip;
			}

			return clip;
		}

		/// <summary>How many clips are loaded, for the diagnostic overlay.</summary>
		public static int LoadedClipCount => clips.Count;

		/// <summary>Forgets loaded clips. Used when leaving a game.</summary>
		public static void ClearClipCache()
		{
			clips.Clear();
		}
	}
}