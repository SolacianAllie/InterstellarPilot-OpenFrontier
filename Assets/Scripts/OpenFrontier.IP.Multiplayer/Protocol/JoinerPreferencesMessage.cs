using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// How a joining player wants to be named.
	/// </summary>
	/// <remarks>
	/// Sent from client to host, before the host makes the faction, so that a
	/// joiner's pilot and faction carry the names that player actually uses
	/// rather than a generic one. Four short strings, because that is what a
	/// player's identity is in this game.
	///
	/// This has to be asked for rather than assumed. Those defaults live on
	/// GameController, which is per-profile, so two copies of the game on one
	/// machine have different ones - and in a shared session every player
	/// wants their own. A host reading its own defaults would name everyone
	/// after itself.
	///
	/// All four are optional. A joiner who has changed nothing sends what they
	/// have, and a missing string is filled in by the host's own defaults
	/// rather than refused, because a player with a blank faction name is
	/// entirely normal and should still be able to play.
	/// </remarks>
	public struct JoinerPreferencesMessage
	{
		/// <summary>Longest any one of the four strings may be, in bytes.</summary>
		public const int MaxStringBytes = 64;

		/// <summary>How many strings travel.</summary>
		public const int StringCount = 4;

		/// <summary>Bytes before the strings: four lengths.</summary>
		public const int HeaderBytes = StringCount * 4;

		/// <summary>The joining player's pilot name, e.g. "Ada".</summary>
		public string PilotName;

		/// <summary>Their rank or title, e.g. "Commander".</summary>
		public string PilotTitle;

		/// <summary>Their faction's name, e.g. "Frontier Corporation".</summary>
		public string FactionName;

		/// <summary>Their faction's short name, e.g. "Frontier".</summary>
		public string FactionShortName;

		public byte[] ToBytes()
		{
			string[] parts = { PilotName, PilotTitle, FactionName, FactionShortName };

			using (MemoryStream stream = new MemoryStream(HeaderBytes + (MaxStringBytes * StringCount)))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				for (int i = 0; i < StringCount; i++)
				{
					byte[] bytes = Encode(parts[i]);
					writer.Write(bytes.Length);
					writer.Write(bytes);
				}

				writer.Flush();
				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out JoinerPreferencesMessage message, out string error)
		{
			message = default(JoinerPreferencesMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "Preferences are shorter than their header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					var parts = new string[StringCount];

					for (int i = 0; i < StringCount; i++)
					{
						int length = reader.ReadInt32();

						// Before anything is reserved for it: this decides how
						// much memory a message from another machine buys.
						if (length < 0 || length > MaxStringBytes)
						{
							error = "Preference " + i + " length " + length + " is out of range.";
							return false;
						}

						if (length > (stream.Length - stream.Position))
						{
							error = "Preferences end before their strings do.";
							return false;
						}

						parts[i] = length == 0
							? string.Empty
							: Encoding.UTF8.GetString(reader.ReadBytes(length));
					}

					message = new JoinerPreferencesMessage
					{
						PilotName = parts[0],
						PilotTitle = parts[1],
						FactionName = parts[2],
						FactionShortName = parts[3]
					};

					return true;
				}
			}
			catch (IOException e)
			{
				error = "Preferences could not be read: " + e.Message;
				return false;
			}
		}

		/// <summary>
		/// Each string, with the host's own value standing in for a blank.
		/// </summary>
		/// <remarks>
		/// On the host side, because "the host's defaults" is the only set of
		/// defaults that exists there. A blank from a joiner is a player who
		/// has not chosen, not a player who wants nothing.
		/// </remarks>
		public string[] Resolved(string hostPilotName, string hostPilotTitle,
			string hostFactionName, string hostFactionShortName)
		{
			return new[]
			{
				Or(PilotName, hostPilotName),
				Or(PilotTitle, hostPilotTitle),
				Or(FactionName, hostFactionName),
				Or(FactionShortName, hostFactionShortName)
			};
		}

		private static byte[] Encode(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return new byte[0];
			}

			byte[] bytes = Encoding.UTF8.GetBytes(value);

			// Cut rather than refuse: a player with a very long name should
			// still be able to join, just as themselves.
			if (bytes.Length > MaxStringBytes)
			{
				byte[] cut = new byte[MaxStringBytes];
				Array.Copy(bytes, cut, MaxStringBytes);
				return cut;
			}

			return bytes;
		}

		private static string Or(string value, string fallback)
		{
			return string.IsNullOrWhiteSpace(value) ? fallback : value;
		}
	}
}