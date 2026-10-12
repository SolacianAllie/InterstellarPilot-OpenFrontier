using System;
using System.IO;
using System.Text;
using OpenFrontier.IP;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>Why the host accepted or refused a joiner.</summary>
	public enum HandshakeResult : byte
	{
		Accepted = 0,

		/// <summary>Wire format differs. Almost always a developer build mismatch.</summary>
		ProtocolMismatch = 1,

		/// <summary>Game versions differ. The rule is that only identical builds may share a session.</summary>
		VersionMismatch = 2,

		/// <summary>The joiner could not prove it was invited.</summary>
		BadSecret = 3,

		/// <summary>The session is full or otherwise closed to new players.</summary>
		SessionClosed = 4,

		/// <summary>The host is running a newer protocol than it will admit.</summary>
		HostFull = 5
	}

	/// <summary>
	/// The first thing a joiner sends, and the only message a host will act on
	/// from an unverified peer.
	///
	/// Layout is fixed and minimal on purpose. A host reads this and validates
	/// it BEFORE allocating anything sized by the request or deserialising
	/// anything, so a mismatched or bogus peer cannot make it reserve memory or
	/// interpret a layout it does not understand.
	/// </summary>
	public struct HelloMessage
	{
		/// <summary>Wire format the joiner speaks.</summary>
		public int ProtocolVersion;

		/// <summary>Game version the joiner runs.</summary>
		public Version GameVersion;

		/// <summary>The joiner's persistent identity, used to recognise a returning player.</summary>
		public string PlayerToken;

		/// <summary>Proves the joiner was given the code, not merely guessed an address.</summary>
		public byte[] SessionSecret;

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
				{
					writer.Write(ProtocolVersion);
					WriteVersion(writer, GameVersion);
					WriteString(writer, PlayerToken);
					writer.Write(SessionSecret?.Length ?? 0);
					if (SessionSecret != null && SessionSecret.Length > 0)
					{
						writer.Write(SessionSecret);
					}
				}

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] bytes, out HelloMessage message, out string error)
		{
			message = default;
			error = null;

			if (bytes == null)
			{
				error = "Empty handshake.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(bytes, writable: false))
				using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
				{
					int protocolVersion = reader.ReadInt32();
					Version gameVersion = ReadVersion(reader);
					string playerToken = ReadString(reader);

					int secretLength = reader.ReadInt32();

					// Bound the secret read: a hostile peer must not be able to
					// claim a huge length and make us allocate for it.
					if (secretLength < 0 || secretLength > 1024)
					{
						error = "Handshake had an invalid secret length.";
						return false;
					}

					if (secretLength > stream.Length - stream.Position)
					{
						error = "Handshake was truncated.";
						return false;
					}

					byte[] secret = reader.ReadBytes(secretLength);

					message = new HelloMessage
					{
						ProtocolVersion = protocolVersion,
						GameVersion = gameVersion,
						PlayerToken = playerToken,
						SessionSecret = secret
					};

					return true;
				}
			}
			catch (EndOfStreamException)
			{
				error = "Handshake was truncated.";
				return false;
			}
			catch (IOException)
			{
				error = "Handshake could not be read.";
				return false;
			}
		}

		private static void WriteVersion(BinaryWriter writer, Version version)
		{
			// -1 encodes "unspecified", matching Version's own convention for
			// components that were never set.
			writer.Write(version.Major);
			writer.Write(version.Minor);
			writer.Write(version.Build);
			writer.Write(version.Revision);
		}

		private static Version ReadVersion(BinaryReader reader)
		{
			int major = reader.ReadInt32();
			int minor = reader.ReadInt32();
			int build = reader.ReadInt32();
			int revision = reader.ReadInt32();

			Version version;

			if (!VersionValidation.TryBuild(major, minor, build, revision, out version))
			{
				// Throwing here is caught by TryFromBytes and converted into a
				// parse failure, so a corrupt version cannot crash the handshake.
				throw new IOException("Handshake contained an invalid version.");
			}

			return version;
		}

		private static void WriteString(BinaryWriter writer, string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				writer.Write(0);
				return;
			}

			byte[] bytes = Encoding.UTF8.GetBytes(value);
			writer.Write(bytes.Length);
			writer.Write(bytes);
		}

		private static string ReadString(BinaryReader reader)
		{
			int length = reader.ReadInt32();

			if (length <= 0)
			{
				return string.Empty;
			}

			// Cap at a generous but finite size; a token is short, and anything
			// huge here is malformed rather than legitimate.
			if (length > 4096 || length > reader.BaseStream.Length - reader.BaseStream.Position)
			{
				throw new IOException("Handshake string length out of range.");
			}

			return Encoding.UTF8.GetString(reader.ReadBytes(length));
		}
	}

	/// <summary>The host's answer to a joiner's handshake.</summary>
	public struct HelloReplyMessage
	{
		public HandshakeResult Result;

		/// <summary>Host's game version, so a rejected joiner can be told what it needed.</summary>
		public Version HostGameVersion;

		/// <summary>Host's wire format version.</summary>
		public int HostProtocolVersion;

		/// <summary>
		/// True when this player has joined before, so the host restores their
		/// faction and unit rather than creating a new one.
		/// </summary>
		public bool ReturningPlayer;

		/// <summary>
		/// The host is in the main menu, so there is no world to arrive in.
		/// </summary>
		/// <remarks>
		/// Separate from SessionClosed because the two are opposites from the
		/// joiner's point of view and the same from the host's. A host in the
		/// menu will be in a game in a minute; a host that has switched
		/// multiplayer off will not. Telling the first one "has stopped
		/// accepting players" is the sort of message that sends a player
		/// looking for a setting that was never touched.
		///
		/// The host already knows this - it refuses such joins on purpose -
		/// and already says so in its probe replies. It simply was not saying
		/// it here, where it was needed.
		/// </remarks>
		public bool InMainMenu;

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
				{
					writer.Write((byte)Result);
					writer.Write(HostProtocolVersion);
					writer.Write(HostGameVersion.Major);
					writer.Write(HostGameVersion.Minor);
					writer.Write(HostGameVersion.Build);
					writer.Write(HostGameVersion.Revision);
					writer.Write(ReturningPlayer);
					writer.Write(InMainMenu);
				}

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] bytes, out HelloReplyMessage message, out string error)
		{
			message = default;
			error = null;

			if (bytes == null || bytes.Length < 7)
			{
				error = "Host reply was truncated.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(bytes, writable: false))
				using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
				{
					var result = (HandshakeResult)reader.ReadByte();

					// The result byte comes off the wire, so it can be any of 256
					// values while only a handful are defined. Refuse an unknown
					// one rather than letting it propagate as a bogus "refused"
					// state that reads as a real answer.
					if (!System.Enum.IsDefined(typeof(HandshakeResult), result))
					{
						error = "Host sent an unrecognised reply.";
						return false;
					}

					int protocolVersion = reader.ReadInt32();
					int major = reader.ReadInt32();
					int minor = reader.ReadInt32();
					int build = reader.ReadInt32();
					int revision = reader.ReadInt32();
					bool returning = reader.ReadBoolean();
				bool inMainMenu = reader.ReadBoolean();

					// These come off the wire, so they can be arbitrary. Version
					// rejects negative components and absurd magnitudes, so refuse
					// anything that would not be a real version rather than letting
					// the constructor throw out of a parse method that promises
					// not to.
					Version version;
					if (!VersionValidation.TryBuild(major, minor, build, revision, out version))
					{
						error = "Host reply contained an invalid version.";
						return false;
					}

					message = new HelloReplyMessage
					{
						Result = result,
						HostProtocolVersion = protocolVersion,
						HostGameVersion = version,
						ReturningPlayer = returning,
						InMainMenu = inMainMenu
					};

					return true;
				}
			}
			catch (EndOfStreamException)
			{
				error = "Host reply was truncated.";
				return false;
			}
			catch (IOException)
			{
				error = "Host reply could not be read.";
				return false;
			}
		}

		/// <summary>Turns a refusal into something worth showing a player.</summary>
		public string Describe()
		{
			switch (Result)
			{
				case HandshakeResult.Accepted:
					return null;

				case HandshakeResult.ProtocolMismatch:
					// Nearly identical to the version case on purpose, because
					// both numbers derive from the version and matching them
					// is the fix either way. The protocol is named as well
					// because leaving it out produced two different failures
					// with one identical sentence, which is a message that
					// cannot be acted on and cannot be diagnosed.
					return "That host speaks a different wire format (protocol "
						+ HostProtocolVersion + ", you have " + ProtocolVersion.Current
						+ "). Both run Open Frontier " + Versioning.Version
						+ ", so they should agree - one of you is likely out of date.";

				case HandshakeResult.VersionMismatch:
					return "That host is running Open Frontier " + HostGameVersion
						+ ", and you're running " + Versioning.Version
						+ ". Players must match versions exactly.";

				case HandshakeResult.BadSecret:
					return "That join code isn't valid for this session.";

				case HandshakeResult.SessionClosed:
					// The one refusal with two opposite causes. A host in the
					// menu is about to be ready; a host that has switched
					// multiplayer off is not, and no amount of waiting will
					// change that.
					return InMainMenu
						? "The host is still in the main menu. They need to load "
							+ "a game before anyone can join them."
						: "The host has stopped accepting players.";

				case HandshakeResult.HostFull:
					return "The host can't accept another player right now.";

				default:
					// An undefined value means the bytes were corrupt or from a
					// build we do not understand. Never fall through to null:
					// callers use null to mean "accepted".
					return "The host refused the connection for an unknown reason.";
			}
		}
	}

	/// <summary>
	/// Builds a <see cref="Version"/> from wire values that may be arbitrary.
	///
	/// Returns false rather than throwing when the components could not be a
	/// real version. Used by both handshake messages, since both read version
	/// components straight off the network.
	/// </summary>
	internal static class VersionValidation
	{
		/// <summary>
		/// Highest plausible component. Real versions are tiny; this exists only
		/// so a corrupt value is rejected rather than constructing something
		/// nonsensical.
		/// </summary>
		private const int MaxComponent = 100000;

		public static bool TryBuild(int major, int minor, int build, int revision, out Version version)
		{
			version = null;

			if (!IsValid(major) || !IsValid(minor) || !IsValid(build) || !IsValid(revision))
			{
				return false;
			}

			// Build the most specific form the data supports, because Version's
			// constructors reject -1 components.
			if (revision >= 0)
			{
				version = new Version(major, minor, build, revision);
			}
			else if (build >= 0)
			{
				version = new Version(major, minor, build);
			}
			else
			{
				version = new Version(major, minor);
			}

			return true;
		}

		private static bool IsValid(int component)
		{
			return component == -1 || (component >= 0 && component <= MaxComponent);
		}
	}
}