using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// Draws combat sections from a shuffled bag, and reshuffles when empty.
	/// </summary>
	/// <remarks>
	/// Combat music is sectional, not looping. Low and Medium Engagement have
	/// six and eight sections and Threat has three, so a fight plays one
	/// section and then moves to another rather than the same minute
	/// repeating until the fighting stops. High Threat has only the one
	/// section and therefore repeats it, which is accepted: a fight with
	/// twenty hostiles in it where none of them are shooting at you is both
	/// rare and long, and it is the one case where the music will feel
	/// repetitive.
	///
	/// IT USED TO WALK THEM IN NUMBERED ORDER, AND THAT WAS WRONG. Numbered
	/// order makes a fight a fixed sequence - 1, 2, 3, 4 - so after two
	/// fights the player has learned it and can hear where a fight is in its
	/// own music. The sections were composed to combine in a deliberate
	/// order, and playing them that way turns a composed sequence into
	/// something the player can predict, which is the exact thing sectional
	/// music exists to avoid.
	///
	/// SHUFFLED, NOT UNIFORMLY RANDOM. A reshuffle plays every section once
	/// before any repeats - the suite, in a new order each time - rather than
	/// drawing blind every time. With six sections, blind drawing means the
	/// same one can land twice running fairly often, and two adjacent
	/// sections of the same track sound like a skip rather than a change. A
	/// bag cannot do that by construction, and hearing all six before any
	/// repeat is worth more than a purer distribution nobody can hear.
	///
	/// Changing intensity, or between Threat and Engagement, reshuffles
	/// rather than carrying the cursor: the music is changing character, and
	/// resuming a shuffle halfway would mean arriving at the fifth section of
	/// a set you have heard two of.
	/// </remarks>
	public sealed class MusicCombatSequencer
	{
		private readonly List<MusicTimingTable.Entry> sections = new List<MusicTimingTable.Entry>();

		private string category;
		private string intensity;

		/// <summary>Where we are in the bag.</summary>
		private int cursor;

		/// <summary>How many of this pass have been drawn.</summary>
		private int playedThisPass;

		/// <summary>The last one handed out, to keep a reshuffle off it.</summary>
		private MusicTimingTable.Entry lastDrawn;

		/// <summary>The section playing now.</summary>
		/// <remarks>
		/// Held rather than derived from the cursor, because the cursor is
		/// about the bag and not about what is sounding. Deriving it meant
		/// the cursor had to stay pointing at the current entry, which is
		/// what made the wrap arithmetic fiddly and briefly wrong.
		/// </remarks>
		private MusicTimingTable.Entry current;

		/// <summary>The section playing now, or the one to play first.</summary>
		public MusicTimingTable.Entry Current => current;

		public int SectionCount => sections.Count;

		/// <summary>Which section of which kind is playing, for logging.</summary>
		public string Describe()
		{
			if (sections.Count == 0)
			{
				return "no combat sections for " + intensity + " " + category;
			}

			return intensity + " " + category + " ('" + current.Name + "'), "
				+ playedThisPass + " of " + sections.Count
				+ " drawn this pass, at random"
				+ (sections.Count == 1 ? " - it loops, there is only one." : ".");
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

			List<MusicTimingTable.Entry> found =
				MusicLibrary.CombatSections(newCategory, newIntensity);

			sections.Clear();

			for (int i = 0; i < found.Count; i++)
			{
				sections.Add(found[i]);
			}

			if (sections.Count == 0)
			{
				cursor = 0;
				playedThisPass = 0;
				current = null;

				Debug.LogWarning("Music: no " + newIntensity + " " + newCategory
					+ " sections exist, so there is no combat music for this.");

				return true;
			}

			lastDrawn = null;
			Shuffle();
			current = Draw();

			Debug.Log("Music: combat music is now " + Describe());

			return true;
		}

		/// <summary>
		/// The next section to play, drawn at random.
		/// </summary>
		/// <remarks>
		/// Null only when there are no sections at all, which is missing
		/// content rather than a state to handle.
		/// </remarks>
		public MusicTimingTable.Entry Advance()
		{
			if (sections.Count == 0)
			{
				return null;
			}

			current = Draw();

			return current;
		}

		/// <summary>Takes the next entry out of the bag, reshuffling if empty.</summary>
		private MusicTimingTable.Entry Draw()
		{
			if (cursor >= sections.Count)
			{
				Shuffle();
			}

			MusicTimingTable.Entry entry = sections[cursor];

			cursor++;
			playedThisPass++;
			lastDrawn = entry;

			return entry;
		}

		/// <summary>
		/// Fisher-Yates, with the first draw nudged off the last one played.
		/// </summary>
		/// <remarks>
		/// The nudge matters at the seam between passes. Without it there is a
		/// one-in-six chance every round that a fresh shuffle begins on the
		/// section that just finished - precisely the sound the bag exists to
		/// prevent, and the most likely place for a repeat.
		/// </remarks>
		private void Shuffle()
		{
			for (int i = sections.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				MusicTimingTable.Entry swap = sections[i];
				sections[i] = sections[j];
				sections[j] = swap;
			}

			cursor = 0;
			playedThisPass = 0;

			if (sections.Count > 1 && sections[0] == lastDrawn)
			{
				MusicTimingTable.Entry swap = sections[0];
				sections[0] = sections[1];
				sections[1] = swap;
			}
		}
	}
}