using System;
using System.Collections.Generic;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.IO;
using OpenFrontier.Unity.Utils;
using UnityEngine;

namespace OpenFrontier.IP.Engine.StationNaming
{
	/// <summary>
	/// Names for every station that is not a bar.
	/// </summary>
	/// <remarks>
	/// Only bars were named. BuildStation special-cased FactionType.Bar and
	/// set unit.UnitName = faction.Name; every other station fell through to
	/// GetDesignationOrClassAndSeries, which yields "Outpost QQ3". In a world
	/// of 1024 sectors that is most of what the player sees.
	///
	/// Names are matched to StationPurpose where the pool has a preference, and
	/// fall back to any name when it does not, so every purpose always has
	/// candidates. Selection is by index rather than a random draw because the
	/// caller has no random source, and because a station's name should not
	/// change between the seeder running and the name being read back.
	///
	/// Old saves are untouched: UnitName is persisted, and an existing world
	/// has it empty on all these stations, so the designation fallback still
	/// applies to everything already placed.
	/// </remarks>
	public class StationNamer : MonoBehaviour
	{
		public TextAsset NamesFile;

		private struct Entry
		{
			public string Name;
			public string ShortName;
			public int PurposeMask;
		}

		private List<Entry> entries = new List<Entry>(200);
		private List<int> anyPurpose = new List<int>(200);

		private static StationNamer instance;
		private static bool warnedUnavailable;

		private bool loaded;

		public static StationNamer Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindAnyObjectByType<StationNamer>();
				}

				return instance;
			}
		}

		private void Awake()
		{
			instance = this;
			Load();
		}

		private void Load()
		{
			if (loaded)
			{
				return;
			}

			loaded = true;
			if (NamesFile == null)
			{
				// Previously this returned silently, and the symptom was every
				// station reading "Outpost QQ3" with no error anywhere. A missing
				// reference is exactly the failure AGENTS.md warns about, so say so.
				Debug.LogError("StationNamer.NamesFile is null, so no station names can be loaded. This component must be on the EngineASX prefab with NamesFile assigned.", this);
				return;
			}
			foreach (string line in IOHelper.ReadAndSplitTextLines(NamesFile))
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Name,"))
				{
					continue;
				}

				string[] parts = line.Split(',');
				if (parts.Length < 3)
				{
					continue;
				}

				if (!int.TryParse(parts[2], out int mask))
				{
					continue;
				}

				entries.Add(new Entry
				{
					Name = parts[0],
					ShortName = parts[1],
					PurposeMask = mask
				});

				if (mask == 0)
				{
					anyPurpose.Add(entries.Count - 1);
				}
			}

			LogWrapper.Log($"Loaded {entries.Count} station names", this, 1);
		}

		/// <summary>
		/// Assigns a name to a station that does not already have one.
		/// </summary>
		/// <param name="salt">
		/// Any value that differs between stations - the sector id is ideal.
		/// Used to spread picks without needing a random source.
		/// </param>
		public static void NameIfUnnamed(Unit unit, int salt)
		{
			if (unit == null || !string.IsNullOrEmpty(unit.UnitName))
			{
				return;
			}

			StationNamer namer = Instance;
			if (namer == null || namer.entries.Count == 0)
			{
				// Same reasoning as the null NamesFile case: this used to return in
				// silence. Warn once, not per station - world seeding calls this
				// once per station and would otherwise bury the console.
				if (!warnedUnavailable)
				{
					warnedUnavailable = true;
					Debug.LogError("StationNamer has no names available, so stations are being left unnamed and will read as a designation. Check that the component is on the EngineASX prefab and that Awake has run before world seeding.");
				}
				return;
			}

			int index = namer.PickIndex(unit.UnitClass != null
				? unit.UnitClass.StationPurpose
				: StationPurpose.None, salt);
			if (index < 0)
			{
				return;
			}

			Entry entry = namer.entries[index];
			unit.UnitName = entry.Name;
			unit.UnitShortName = entry.ShortName;
		}

		private int PickIndex(StationPurpose purpose, int salt)
		{
			List<int> matches = new List<int>(32);
			int wanted = (int)purpose;
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i].PurposeMask != 0 && (entries[i].PurposeMask & wanted) == wanted)
				{
					matches.Add(i);
				}
			}

			List<int> pool = matches.Count > 0 ? matches : anyPurpose;
			if (pool.Count == 0)
			{
				// No file wired and no matches: fall back to anything at all.
				pool = new List<int>(entries.Count);
				for (int i = 0; i < entries.Count; i++)
				{
					pool.Add(i);
				}
			}

			unchecked
			{
				uint h = (uint)(salt + 1) * 2654435761u;
				h ^= (uint)(wanted + 1) * 2246822519u;
				h ^= h >> 15;
				h *= 2654435761u;
				h ^= h >> 13;
				return pool[(int)(h % (uint)pool.Count)];
			}
		}
	}
}