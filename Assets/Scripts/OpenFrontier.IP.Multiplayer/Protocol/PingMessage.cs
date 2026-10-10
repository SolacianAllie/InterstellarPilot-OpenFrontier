using System;
using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// A liveness probe sent to a host before any handshake.
	/// </summary>
	///
	/// Deliberately carries no session secret: a probe must not let anyone
	/// discover whether a code is valid, and it creates no session, so there is
	/// nothing to authorise. The cost is that anyone who knows the address and
	/// port can tell whether a game is listening there - which is the same thing
	/// a failed join already reveals, and is acceptable for a game played
	/// between friends rather than hosted in the open.
	public struct PingMessage
	{
		/// <summary>Probe format the prober speaks.</summary>
		public int ProtocolVersion;

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream(4))
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(ProtocolVersion);
				}

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] bytes, out PingMessage message, out string error)
		{
			message = default;
			error = null;

			if (bytes == null || bytes.Length < 4)
			{
				error = "Ping was truncated.";
				return false;
			}

			try
			{
				// Read with BinaryReader so the byte order matches the
				// BinaryWriter in ToBytes. Decoding the int by hand here once
				// read 0x01000000 instead of 1.
				using (MemoryStream stream = new MemoryStream(bytes, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					message = new PingMessage
					{
						ProtocolVersion = reader.ReadInt32()
					};
				}
			}
			catch (EndOfStreamException)
			{
				error = "Ping was truncated.";
				return false;
			}

			return true;
		}
	}

	/// <summary>
	/// The host's answer to a probe.
	/// </summary>
	///
	/// Carries the host's versions so a prober can tell "this host is on a
	/// different build" from "nothing is listening here" - two problems that
	/// look identical from the outside and need different responses from the
	/// player.
	public struct PongMessage
	{
		public int ProtocolVersion;

		/// <summary>Host's game version, or null if it could not be read.</summary>
		public Version GameVersion;

		/// <summary>Whether the host will admit a player at the moment.</summary>
		public bool AcceptingPlayers;

		/// <summary>
		/// Whether the host is sitting in the main menu rather than in a game.
		/// </summary>
		/// <remarks>
		/// Distinct from simply not accepting. A host in the menu is up, healthy
		/// and will take players shortly; one that has stopped accepting has
		/// declined to. Telling a joiner the difference is the whole point -
		/// otherwise both read as the same unhelpful "cannot join right now".
		/// </remarks>
		public bool InMainMenu;

		/// <summary>How many players are already connected.</summary>
		public int PlayerCount;

		/// <summary>Who is hosting, by the name they have chosen.</summary>
		public string HostName;

		/// <summary>What they are playing, so a slot says something useful.</summary>
		public string ScenarioName;

		/// <summary>Seconds this session has been up.</summary>
		public int SessionSeconds;

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					writer.Write(ProtocolVersion);

					// -1 for "unspecified", matching how a version is encoded
					// elsewhere. Every component is written, so the layout is
					// fixed and a reader can validate before allocating.
					writer.Write(GameVersion == null ? -1 : GameVersion.Major);
					writer.Write(GameVersion == null ? -1 : GameVersion.Minor);
					writer.Write(GameVersion == null ? -1 : GameVersion.Build);
					writer.Write(GameVersion == null ? -1 : GameVersion.Revision);

					writer.Write(AcceptingPlayers);
					writer.Write(InMainMenu);
					writer.Write(PlayerCount);
					writer.Write(SessionSeconds < 0 ? -1 : SessionSeconds);

					WriteName(writer, HostName);
					WriteName(writer, ScenarioName);
				}

				return stream.ToArray();
			}
		}

		/// <summary>Longest name a host may send, in bytes.</summary>
		public const int MaxNameBytes = 48;

		private static void WriteName(BinaryWriter writer, string value)
		{
			byte[] bytes = string.IsNullOrEmpty(value)
				? new byte[0]
				: System.Text.Encoding.UTF8.GetBytes(value);

			if (bytes.Length > MaxNameBytes)
			{
				byte[] cut = new byte[MaxNameBytes];
				Array.Copy(bytes, cut, MaxNameBytes);
				bytes = cut;
			}

			writer.Write(bytes.Length);
			writer.Write(bytes);
		}

		private static string ReadName(BinaryReader reader, out bool ok)
		{
			ok = true;

			int length = reader.ReadInt32();

			// Checked before anything is reserved: this decides how much memory
			// a message from another machine buys.
			if (length < 0 || length > MaxNameBytes)
			{
				ok = false;
				return string.Empty;
			}

			if (length > (reader.BaseStream.Length - reader.BaseStream.Position))
			{
				ok = false;
				return string.Empty;
			}

			return length == 0
				? string.Empty
				: System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));
		}

		public static bool TryFromBytes(byte[] bytes, out PongMessage message, out string error)
		{
			message = default;
			error = null;

			// 4 protocol + 16 version + 1 accepting + 1 in-main-menu + 4 count
			// = 26. BinaryWriter writes a bool as a single byte, not four.
			// 26 for the fixed part, plus two four-byte name lengths.
			if (bytes == null || bytes.Length < 34)
			{
				error = "Reply was truncated.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(bytes, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int protocolVersion = reader.ReadInt32();
					int major = reader.ReadInt32();
					int minor = reader.ReadInt32();
					int build = reader.ReadInt32();
					int revision = reader.ReadInt32();
					bool accepting = reader.ReadBoolean();
					bool inMainMenu = reader.ReadBoolean();
					int playerCount = reader.ReadInt32();
					int sessionSeconds = reader.ReadInt32();

					string hostName = ReadName(reader, out bool hostNameOk);
					string scenarioName = ReadName(reader, out bool scenarioNameOk);

					if (!hostNameOk || !scenarioNameOk)
					{
						error = "Reply contained an implausible name.";
						return false;
					}

					Version version = null;

					if (VersionValidation.TryBuild(major, minor, build, revision, out Version parsed))
					{
						version = parsed;
					}

					// A nonsensical player count means the bytes are not something
					// this build wrote, so the reply is not trusted.
					if (playerCount < 0 || playerCount > 100000)
					{
						error = "Reply contained an implausible player count.";
						return false;
					}

					message = new PongMessage
					{
						ProtocolVersion = protocolVersion,
						GameVersion = version,
						AcceptingPlayers = accepting,
						InMainMenu = inMainMenu,
						PlayerCount = playerCount,
						SessionSeconds = sessionSeconds,
						HostName = hostName,
						ScenarioName = scenarioName
					};

					return true;
				}
			}
			catch (EndOfStreamException)
			{
				error = "Reply was truncated.";
				return false;
			}
			catch (IOException)
			{
				error = "Reply could not be read.";
				return false;
			}
		}
	}
}