using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenFrontier.IP.Engine.SaveGame;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// A multiplayer host the player has saved: the name they gave it and the
	/// join code needed to reach it.
	///
	/// These are the "saved games" of the multiplayer screen. They are held in
	/// a small text file next to the game's other data rather than in
	/// PlayerPrefs, because a host list is a list of records and grows, and
	/// because keeping it as a file makes it inspectable and easy to back up.
	/// </summary>
	public class SavedHost
	{
		/// <summary>
		/// What the player called this host. Shown in the list; not used to
		/// connect.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The raw, unformatted join code, exactly as produced by a host.
		/// </summary>
		public string Code { get; set; }

		/// <summary>When this entry was added, for display and ordering.</summary>
		public DateTime AddedUtc { get; set; }

		public SavedHost()
		{
		}

		public SavedHost(string name, string code)
		{
			Name = name;
			Code = code;
			AddedUtc = DateTime.UtcNow;
		}

		/// <summary>
		/// Whether a session with this host is currently connected.
		/// </summary>
		/// <remarks>
		/// Separate from <see cref="ProbeResult"/>: a probe says whether the host
		/// could be reached, this says whether this player is in it. Both are
		/// shown, because "the host is up but you are not connected to it" and
		/// "the host is not up at all" call for different actions.
		/// </remarks>
		/// <summary>Whether this player is in a session hosted here right now.</summary>
		public bool IsConnected { get; set; }

		/// <summary>
		/// Whether a join to this host is on its way.
		/// </summary>
		/// <remarks>
		/// Separate from <see cref="IsConnected"/> because a join takes a
		/// moment and the button has to stop being pressable the instant it is
		/// pressed, not once the answer arrives. Without this, a second press
		/// while the first was still being answered opened a second connection
		/// - which is not a harmless double, because the host admits it and
		/// ends up holding two players wearing the same identity.
		///
		/// Session state, not saved: a host file records who and where, not what
		/// this particular copy of the game is doing right now.
		/// </remarks>
		public bool IsConnecting { get; set; }

		/// <summary>
		/// What the last liveness probe found.
		/// </summary>
		/// <remarks>
		/// Not persisted: reachability is a property of right now, not of the
		/// saved entry, and writing it out would leave the file claiming hosts
		/// are online long after they stopped.
		/// </remarks>
		public HostProbeResult ProbeResult { get; set; }

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? Code : Name;
		}
	}

	/// <summary>
	/// The player's saved multiplayer hosts, persisted between sessions.
	///
	/// Deliberately tolerant of a damaged or hand-edited file: a bad line is
	/// skipped rather than losing every other host. Losing a whole list
	/// because one entry was malformed would be a poor trade for a file the
	/// player can see and edit.
	/// </summary>
	public static class SavedHostStore
	{
		private const string FileName = "MultiplayerHosts.txt";

		// One host per line, fields separated by a character that cannot appear
		// in either field - tab is rejected by the rename screen's validation
		// and never typed into a name.
		private const char FieldSeparator = '\t';

		private static List<SavedHost> cached;

		/// <summary>Where the list is stored, for display and diagnostics.</summary>
		public static string FilePath => Path.Combine(SaveGameUtilities.GetSaveDirectory(), FileName);

		/// <summary>
		/// Every saved host, most recently added first.
		/// </summary>
		public static IReadOnlyList<SavedHost> Hosts
		{
			get
			{
				if (cached == null)
				{
					cached = Load();
				}

				return cached;
			}
		}

		/// <summary>Drops the in-memory copy so the next read comes from disk.</summary>
		public static void InvalidateCache()
		{
			cached = null;
		}

		/// <summary>
		/// Adds a host, or returns the existing entry if the code is already
		/// saved.
		/// </summary>
		/// <remarks>
		/// Re-adding an existing code updates its name rather than creating a
		/// duplicate, so entering a code the player has joined before does not
		/// fill the list with copies of the same host.
		/// </remarks>
		public static SavedHost AddOrUpdate(string name, string code)
		{
			string trimmedCode = (code ?? string.Empty).Trim();

			if (string.IsNullOrEmpty(trimmedCode))
			{
				return null;
			}

			List<SavedHost> hosts = new List<SavedHost>(cached ?? Load());

			SavedHost match = null;

			foreach (SavedHost host in hosts)
			{
				if (string.Equals(host.Code, trimmedCode, StringComparison.OrdinalIgnoreCase))
				{
					match = host;
					break;
				}
			}

			if (match != null)
			{
				// Keep the existing name when the caller did not supply one.
				if (!string.IsNullOrWhiteSpace(name))
				{
					match.Name = name.Trim();
				}

				SortAndSave(hosts);
				return match;
			}

			SavedHost added = new SavedHost(string.IsNullOrWhiteSpace(name) ? trimmedCode : name.Trim(), trimmedCode);
			hosts.Add(added);
			SortAndSave(hosts);
			return added;
		}

		/// <summary>Renames a host. Returns false if it is no longer in the list.</summary>
		public static bool Rename(SavedHost host, string newName)
		{
			if (host == null || string.IsNullOrWhiteSpace(newName))
			{
				return false;
			}

			List<SavedHost> hosts = new List<SavedHost>(Hosts);

			if (!hosts.Contains(host))
			{
				return false;
			}

			host.Name = newName.Trim();
			SortAndSave(hosts);
			return true;
		}

		/// <summary>Removes a host. Returns false if it was not in the list.</summary>
		public static bool Remove(SavedHost host)
		{
			if (host == null)
			{
				return false;
			}

			List<SavedHost> hosts = new List<SavedHost>(Hosts);

			if (!hosts.Remove(host))
			{
				return false;
			}

			SortAndSave(hosts);
			return true;
		}

		private static void SortAndSave(List<SavedHost> hosts)
		{
			hosts.Sort((a, b) => b.AddedUtc.CompareTo(a.AddedUtc));
			cached = hosts;
			Save(hosts);
		}

		private static List<SavedHost> Load()
		{
			List<SavedHost> hosts = new List<SavedHost>();

			try
			{
				string path = FilePath;

				if (!File.Exists(path))
				{
					return hosts;
				}

				foreach (string line in File.ReadAllLines(path))
				{
					if (string.IsNullOrWhiteSpace(line))
					{
						continue;
					}

					string[] fields = line.Split(FieldSeparator);

					if (fields.Length < 2)
					{
						// Malformed line: skip it rather than failing the whole load.
						continue;
					}

					string name = fields[0];
					string code = fields[1];

					if (string.IsNullOrWhiteSpace(code))
					{
						continue;
					}

					DateTime added = DateTime.UtcNow;

					if (fields.Length >= 3 && DateTime.TryParse(fields[2], out DateTime parsed))
					{
						added = parsed;
					}

					hosts.Add(new SavedHost
					{
						Name = name,
						Code = code,
						AddedUtc = added
					});
				}
			}
			catch (Exception e)
			{
				// A corrupt or unreadable list must not stop the game starting.
				// Losing the host list is an inconvenience; failing to reach the
				// menu is worse.
				Debug.LogWarning("Could not read the saved multiplayer hosts: " + e.Message);
			}

			hosts.Sort((a, b) => b.AddedUtc.CompareTo(a.AddedUtc));
			return hosts;
		}

		private static void Save(List<SavedHost> hosts)
		{
			try
			{
				string directory = SaveGameUtilities.GetSaveDirectory();

				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				StringBuilder builder = new StringBuilder();

				foreach (SavedHost host in hosts)
				{
					// Strip the separator and newlines so a name can never break
					// the file's line structure.
					string name = (host.Name ?? string.Empty)
						.Replace(FieldSeparator, ' ')
						.Replace("\r", " ")
						.Replace("\n", " ");

					builder.Append(name)
						.Append(FieldSeparator)
						.Append(host.Code ?? string.Empty)
						.Append(FieldSeparator)
						.Append(host.AddedUtc.ToString("O"))
						.Append('\n');
				}

				File.WriteAllText(FilePath, builder.ToString());
			}
			catch (Exception e)
			{
				Debug.LogWarning("Could not save the multiplayer host list: " + e.Message);
			}
		}
	}
}