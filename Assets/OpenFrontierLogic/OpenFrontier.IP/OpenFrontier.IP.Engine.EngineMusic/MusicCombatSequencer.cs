using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// Walks the sections of a combat track in order and wraps.
	/// </summary>
	/// <remarks>
	/// Combat music is sectional, not looping. Low and Medium Engagement have
	/// six and eight sections and Threat has three, so a fight plays one
	/// section and then moves to the next, rather than the same minute
	/// repeating until the fighting stops. High Threat has only the one
	/// section and therefore loops it, which is accepted for now: a fight with
	/// twenty hostiles in it where none of them are shooting at you is both
	/// rare and long, and it is the one case where the music will feel
	/// repetitive.
	///
	/// Sections are taken in numbered order and the list wraps at the end. The
	/// alternative - reshuffling - would mean the same fight never sounds the
	/// same twice, and with six sections there is no need for that.
	///
	/// Changing intensity or between Threat and Engagement resets to the
	/// first section, because the music is changing character and carrying a
	/// cursor across that would mean starting a low fight halfway through a
	/// section written for a different one.
	/// </remarks>
	public sealed class MusicCombatSequencer
	{
		private readonly List<MusicTimingTable.Entry> sections = new List<MusicTimingTable.Entry>();

		private string category;
		private string intensity;
		private int index;

		public MusicTimingTable.Entry Current => index >= 0 && index < sections.Count
			? sections[index]
			: null;

		public int SectionCount => sections.Count;

		/// <summary>Which section of which kind is playing, for logging.</summary>
		public string Describe()
		{
			if (sections.Count == 0)
			{
				return "no combat sections for " + intensity + " " + category;
			}

			return intensity + " " + category + " section "
				+ (index + 1) + " of " + sections.Count + " ('" + Current.Name + "')";
		}

		/// <summary>
		/// Points the sequencer at a kind and intensity, if it has moved.
		/// </summary>
		/// <returns>True when it changed, so the caller knows to restart.</returns>
		public bool SetKind(string newCategory, string newIntensity)
		{
			if (newCategory == category && newIntensity == intensity)
			{
				return false;
			}

			category = newCategory;
			intensity = newIntensity;
			index = 0;

			List<MusicTimingTable.Entry> found =
				MusicLibrary.CombatSections(newCategory, newIntensity);

			sections.Clear();

			for (int i = 0; i < found.Count; i++)
			{
				sections.Add(found[i]);
			}

			if (sections.Count == 0)
			{
				Debug.LogWarning("Music: no " + newIntensity + " " + newCategory
					+ " sections exist, so there is no combat music for this.");

				return true;
			}

			Debug.Log("Music: combat music is now " + Describe()
				+ (sections.Count == 1 ? " - it loops, there is only one." : "."));

			return true;
		}

		/// <summary>
		/// The section to play now, having already accounted for the last one.
		/// </summary>
		/// <remarks>
		/// Called when the current track ends. Returns the next section, or the
		/// same one again when there is only the one - which is the High
		/// Threat case, and the reason a single-section list must not be
		/// treated as an error.
		/// </remarks>
		public MusicTimingTable.Entry Advance()
		{
			if (sections.Count == 0)
			{
				return null;
			}

			index++;

			if (index >= sections.Count)
			{
				index = 0;
			}

			return sections[index];
		}
	}
}