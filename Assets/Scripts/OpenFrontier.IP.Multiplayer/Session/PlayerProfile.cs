using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Which player this copy of the game is playing as.
	///
	/// One installation is one player, which is what PlayerPrefs means and
	/// what almost every player wants. It stops being true the moment two
	/// copies run on one machine - an editor and a build, or a build and a
	/// build - and then they share a player token and a session secret, hand
	/// out identical join codes, and cannot tell each other apart. That is
	/// almost always exactly what you do not want when testing a network
	/// feature with two players, and nothing you want in the shipped game.
	///
	/// So a copy can be told which player it is, and that is the whole of it:
	/// the slot changes which prefs keys are used, and nothing else. A join
	/// code is still stable for as long as the slot is, so repeated
	/// connections from the same copy are unaffected - which is the whole
	/// point, since a slot that changed the code every launch would be worse
	/// than the problem it solves.
	///
	/// Only the multiplayer identity keys are scoped. Save games, settings
	/// and everything else are deliberately shared, because two copies on one
	/// machine are usually one person testing, not two people who happen to
	/// share a hard drive.
	/// </summary>
	public static class PlayerProfile
	{
		/// <summary>The slot a copy plays as when nothing says otherwise.</summary>
		public const string DefaultSlot = "default";

		/// <summary>The slot the editor plays as.</summary>
		public const string EditorSlot = "editor";

		private const string FileName = "openfrontier-profile.txt";
		private const string EnvironmentVariable = "OPENFRONTIER_PROFILE";
		private const string CommandLineSwitch = "--profile";

		// Enough to tell slots apart without letting a stray value grow into
		// something that breaks a prefs key or a file name.
		private const int MaxSlotLength = 24;

		private static string cachedSlot;

		/// <summary>
		/// The slot this copy plays as, resolved on first use.
		/// </summary>
		/// <remarks>
		/// Resolved lazily rather than in a static initializer: reading the
		/// command line and asking Unity for its data path are both things
		/// that must not happen during type initialization, which runs at
		/// points where such calls are not allowed and would poison the whole
		/// type.
		/// </remarks>
		public static string Slot
		{
			get
			{
				if (cachedSlot == null)
				{
					cachedSlot = Sanitize(Resolve());
				}

				return cachedSlot;
			}
		}

		/// <summary>Forgets the resolved slot, so the next read looks again.</summary>
		public static void InvalidateCache()
		{
			cachedSlot = null;
		}

		/// <summary>
		/// Whether this copy uses the plain player slot.
		/// </summary>
		/// <remarks>
		/// Deliberately not "is it the default slot", because the editor is a
		/// different thing rather than a fallback, and a prefs key must be
		/// scoped for it.
		/// </remarks>
		public static bool IsDefault => Slot == DefaultSlot;

		/// <summary>A line for the session panel, so a test shows which copy is which.</summary>
		public static string Describe()
		{
			return Slot;
		}

		/// <summary>
		/// Turns a prefs key into the key this slot actually uses.
		/// </summary>
		/// <remarks>
		/// The default slot is left completely untouched, so a player who
		/// never heard of this keeps exactly the keys they had. Only a slot
		/// that was asked for gets a suffix.
		/// </remarks>
		public static string Key(string baseKey)
		{
			return KeyFor(baseKey, Slot);
		}

		/// <summary>
		/// Turns a prefs key into the key a given slot uses.
		/// </summary>
		/// <remarks>
		/// Separate from <see cref="Key"/> so the rule can be checked on its
		/// own, which matters because the slot this process resolves to
		/// depends on where it is running - and outside a player, even Unity's
		/// own assemblies report it as the editor.
		/// </remarks>
		public static string KeyFor(string baseKey, string slot)
		{
			if (string.IsNullOrEmpty(baseKey))
			{
				return baseKey;
			}

			// The plain player slot is left completely untouched, so a player
			// who never heard of any of this keeps exactly the keys they had.
			// Only a slot that is actually distinct gets a suffix.
			if (string.IsNullOrEmpty(slot) || slot == DefaultSlot)
			{
				return baseKey;
			}

			return baseKey + "_" + Sanitize(slot);
		}

		/// <summary>
		/// Strips a slot down to something safe to put in a key or a file name.
		/// </summary>
		/// <remarks>
		/// A slot arrives from a command line, an environment variable and a
		/// text file, so it is as untrusted as any other string the game reads.
		/// Anything that is not a letter, a digit, a dash or an underscore is
		/// dropped, and an empty result falls back to the default rather than
		/// silently merging two copies into one identity.
		/// </remarks>
		public static string Sanitize(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return DefaultSlot;
			}

			StringBuilder cleaned = new StringBuilder(value.Length);

			foreach (char character in value.Trim())
			{
				bool allowed = (character >= 'a' && character <= 'z')
					|| (character >= 'A' && character <= 'Z')
					|| (character >= '0' && character <= '9')
					|| character == '-'
					|| character == '_';

				if (allowed)
				{
					cleaned.Append(character);
				}
			}

			if (cleaned.Length == 0)
			{
				return DefaultSlot;
			}

			if (cleaned.Length <= MaxSlotLength)
			{
				return cleaned.ToString();
			}

			// Shortening a long name would merge two slots that differ only at
			// the end, and two copies sharing an identity is exactly the thing
			// this exists to prevent - so the tail is kept as a digest rather
			// than thrown away. Eight hex characters of a non-cryptographic
			// hash: this only has to tell two names apart, and it is not
			// defending anything.
			string tail = Digest(cleaned.ToString());

			return cleaned.ToString(0, MaxSlotLength - tail.Length - 1) + "-" + tail;
		}

		/// <summary>Eight hex characters identifying a name uniquely enough.</summary>
		private static string Digest(string value)
		{
			// FNV-1a. Chosen for being short, dependency-free and stable
			// across platforms - a slot has to mean the same thing in a build
			// made yesterday and one made next year, or saved identities go
			// missing for no reason anyone could explain.
			const uint offset = 2166136261;
			const uint prime = 16777619;

			uint hash = offset;

			foreach (char character in value)
			{
				hash ^= character;
				hash *= prime;
			}

			return hash.ToString("x8", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// The slot to use when nothing has asked for one.
		/// </summary>
		/// <remarks>
		/// The editor gets its own, so the commonest way of running two
		/// players on one machine - the editor and a build of the same game,
		/// side by side - needs no configuration at all. Nobody else is
		/// affected: a player running the editor is a developer, and a
		/// developer is exactly the person who wants to be a distinct player
		/// from the build sitting next to them.
		///
		/// Separate rather than merged because they genuinely are different
		/// situations. A returning player who tested in the editor and then
		/// played the build should start fresh in the build, not inherit an
		/// identity from a copy of the game they were never really playing.
		/// </remarks>
		public static string DefaultSlotFor(bool isEditor)
		{
			return isEditor ? EditorSlot : DefaultSlot;
		}

		private static string Resolve()
		{
			string fromCommandLine = FromCommandLine();

			if (!string.IsNullOrEmpty(fromCommandLine))
			{
				return fromCommandLine;
			}

			string fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);

			if (!string.IsNullOrEmpty(fromEnvironment))
			{
				return fromEnvironment;
			}

			string fromFile = FromFile();

			if (!string.IsNullOrEmpty(fromFile))
			{
				return fromFile;
			}

			// Asked for nothing, so fall back to what this copy is rather than
			// to a fixed name. Read here and not in a field initializer,
			// because asking Unity anything during type initialization is not
			// allowed and would poison the whole type.
			try
			{
				return DefaultSlotFor(Application.isEditor);
			}
			catch (Exception)
			{
				return DefaultSlot;
			}
		}

		private static string FromCommandLine()
		{
			try
			{
				string[] arguments = Environment.GetCommandLineArgs();

				for (int i = 0; i < arguments.Length - 1; i++)
				{
					if (string.Equals(arguments[i], CommandLineSwitch, StringComparison.OrdinalIgnoreCase))
					{
						return arguments[i + 1];
					}
				}
			}
			catch (Exception)
			{
				// Some platforms refuse to hand over the command line. The file
				// and the environment variable still work.
			}

			return null;
		}

		/// <summary>
		/// Reads the slot from a file beside whatever is running.
		/// </summary>
		/// <remarks>
		/// One path for both cases, because the two land in different places
		/// on purpose: a build reads it from beside its own executable, and the
		/// editor from the project root. That is what lets a build and the
		/// editor sitting in that same project be two different players
		/// without either of them being configured.
		/// </remarks>
		private static string FromFile()
		{
			try
			{
				string folder = Path.GetDirectoryName(Application.dataPath);

				if (string.IsNullOrEmpty(folder))
				{
					return null;
				}

				string path = Path.Combine(folder, FileName);

				if (!File.Exists(path))
				{
					return null;
				}

				// The first non-blank line, so a trailing newline is harmless.
				foreach (string line in File.ReadAllLines(path))
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						return line.Trim();
					}
				}
			}
			catch (Exception)
			{
				// Unreadable, missing, or not allowed. A missing profile file
				// is the normal case and must not be a problem.
			}

			return null;
		}
	}
}
