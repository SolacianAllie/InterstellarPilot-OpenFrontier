using System;
using System.Text;
using OpenFrontier.IP;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Encodes/decodes the join code a host shares and a joiner types in.
	///
	/// Layout (exactly 20 bytes, so Base32 is exactly 32 characters, which
	/// groups cleanly into four blocks of eight):
	///
	///   [0]      magic 0x4F
	///   [1]      protocol version   (ProtocolVersion.Current)
	///   [2..5]   game version       (major, minor, build, revision; 255 = unspecified)
	///   [6..9]   IPv4 address       (big endian)
	///   [10..11] port               (big endian)
	///   [12..18] session secret     (7 bytes / 56 bits)
	///   [19]     checksum           (sum of [0..18] mod 256)
	///
	/// WHY THE ADDRESS IS IN HERE: a peer-to-peer connection requires knowing
	/// where the host is, so with no server in the middle the code has to
	/// carry it. What the encoding buys is that a player never sees, types, or
	/// has to understand an IP address. It does NOT hide the address - the
	/// bytes are recoverable by anyone who decodes the code, and the joining
	/// player's machine necessarily learns the host's address. Genuine
	/// concealment would need a relay, which a zero-backend design excludes.
	///
	/// The secret exists so that random or unrelated people cannot join a
	/// session by guessing; it is not protection against someone who already
	/// has the code, since the code necessarily contains it.
	/// </summary>
	public static class SessionCode
	{
		/// <summary>Total encoded payload size in bytes.</summary>
		public const int PayloadSize = 20;

		/// <summary>Number of Base32 characters produced (20 bytes = 160 bits).</summary>
		public const int EncodedLength = 32;

		/// <summary>Characters per display block.</summary>
		public const int BlockSize = 8;

		private const byte Magic = 0x4F;
		private const byte VersionUnspecified = 255;
		private const int ChecksumIndex = PayloadSize - 1;
		private const int SecretOffset = 12;
		private const int SecretLength = 7;
		private const int SecretBytes = SecretLength * 8 / 32;

		// RFC 4648 Base32: no I, L, O, 0 or 1, so it survives being read aloud
		// or copied off a screen without ambiguity.
		private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

		/// <summary>
		/// Builds a code for a listening endpoint.
		/// </summary>
		/// <param name="address">Host address. IPv4 only in this protocol version.</param>
		/// <param name="port">Host port.</param>
		/// <param name="secret">Session secret; 7 bytes are used.</param>
		/// <param name="gameVersion">
		/// Version advertised to joiners. Defaults to this build's version.
		/// </param>
		/// <exception cref="ArgumentException">
		/// Thrown for a non-IPv4 address, an out-of-range port, or a version
		/// component that does not fit in a byte.
		/// </exception>
		public static string Create(System.Net.IPAddress address, int port, byte[] secret, Version gameVersion = null)
		{
			if (address == null)
			{
				throw new ArgumentNullException(nameof(address));
			}

			if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
			{
				throw new ArgumentException(
					"This protocol version encodes IPv4 addresses only; got " + address.AddressFamily + ".", nameof(address));
			}

			if (port < 0 || port > ushort.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(port), port, "Port must fit in 16 bits.");
			}

			if (secret == null)
			{
				throw new ArgumentNullException(nameof(secret));
			}

			if (secret.Length < SecretLength)
			{
				throw new ArgumentException(
					"Secret must be at least " + SecretLength + " bytes; got " + secret.Length + ".", nameof(secret));
			}

			Version version = gameVersion ?? Versioning.Version;

			byte[] bytes = new byte[PayloadSize];
			bytes[0] = Magic;
			bytes[1] = (byte)ProtocolVersion.Current;

			WriteVersion(bytes, version);

			byte[] addressBytes = address.GetAddressBytes();
			Buffer.BlockCopy(addressBytes, 0, bytes, 6, addressBytes.Length);

			bytes[10] = (byte)(port >> 8);
			bytes[11] = (byte)(port & 0xFF);

			Buffer.BlockCopy(secret, 0, bytes, SecretOffset, SecretLength);

			bytes[ChecksumIndex] = ComputeChecksum(bytes);

			return Encode(bytes);
		}

		/// <summary>
		/// Parses a code typed or pasted by a joiner.
		/// </summary>
		/// <returns>
		/// A parsed code, or a result describing why the code is unusable.
		/// Never throws for malformed input - this is reached with whatever
		/// the player typed.
		/// </returns>
		public static SessionCodeParseResult TryParse(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return SessionCodeParseResult.Fail("Enter a join code.");
			}

			// Tolerate whitespace, dashes and lowercase: the code is read aloud
			// and retyped often enough that being forgiving is worth more than
			// being strict.
			byte[] bytes;
			if (!TryDecode(text, out bytes))
			{
				return SessionCodeParseResult.Fail("That doesn't look like a join code.");
			}

			if (bytes[0] != Magic)
			{
				return SessionCodeParseResult.Fail("That doesn't look like a join code.");
			}

			if (bytes[ChecksumIndex] != ComputeChecksum(bytes))
			{
				return SessionCodeParseResult.Fail("The code is damaged - check it and try again.");
			}

			int protocolVersion = bytes[1];
			Version gameVersion = ReadVersion(bytes);
			var address = new System.Net.IPAddress(new byte[] { bytes[6], bytes[7], bytes[8], bytes[9] });
			int port = (bytes[10] << 8) | bytes[11];

			byte[] secret = new byte[SecretLength];
			Buffer.BlockCopy(bytes, SecretOffset, secret, 0, SecretLength);

			return SessionCodeParseResult.Ok(new SessionCodeData(protocolVersion, gameVersion, address, port, secret));
		}

		/// <summary>
		/// Formats a code for display: uppercase, in four blocks of eight.
		/// </summary>
		public static string Format(string encoded)
		{
			if (string.IsNullOrEmpty(encoded))
			{
				return string.Empty;
			}

			StringBuilder builder = new StringBuilder(encoded.Length + 4);
			for (int i = 0; i < encoded.Length; i++)
			{
				if (i > 0 && i % BlockSize == 0)
				{
					builder.Append('-');
				}

				builder.Append(encoded[i]);
			}

			return builder.ToString();
		}

		private static byte ComputeChecksum(byte[] bytes)
		{
			// Sum over the payload only; the checksum byte itself is excluded by
			// being the last index.
			int sum = 0;
			for (int i = 0; i < ChecksumIndex; i++)
			{
				sum += bytes[i];
			}

			return (byte)(sum & 0xFF);
		}

		private static void WriteVersion(byte[] bytes, Version version)
		{
			bytes[2] = ToVersionByte(version.Major, "major");
			bytes[3] = ToVersionByte(version.Minor, "minor");
			bytes[4] = ToVersionByte(version.Build, "build");
			bytes[5] = ToVersionByte(version.Revision, "revision");
		}

		private static byte ToVersionByte(int value, string componentName)
		{
			if (value < 0)
			{
				return VersionUnspecified;
			}

			if (value > byte.MaxValue)
			{
				throw new ArgumentException(
					"Version " + componentName + " component " + value + " does not fit in a byte.", nameof(value));
			}

			return (byte)value;
		}

		private static Version ReadVersion(byte[] bytes)
		{
			int major = FromVersionByte(bytes[2]);
			int minor = FromVersionByte(bytes[3]);
			int build = FromVersionByte(bytes[4]);
			int revision = FromVersionByte(bytes[5]);

			// Version's multi-argument constructors reject -1 outright; only the
			// shorter forms can express an unspecified component. Build the
			// shortest form that represents what was encoded so a version like
			// "0.1.1" comes back with Revision == -1 and still compares equal to
			// the local build.
			if (revision < 0)
			{
				if (build < 0)
				{
					if (minor < 0)
					{
						return new Version(major, 0);
					}

					return new Version(major, minor);
				}

				return new Version(major, minor, build);
			}

			return new Version(major, minor, build, revision);
		}

		private static int FromVersionByte(byte value)
		{
			return value == VersionUnspecified ? -1 : value;
		}

		private static string Encode(byte[] bytes)
		{
			StringBuilder builder = new StringBuilder(EncodedLength);

			int buffer = 0;
			int bitsHeld = 0;

			for (int i = 0; i < bytes.Length; i++)
			{
				buffer = (buffer << 8) | bytes[i];
				bitsHeld += 8;

				while (bitsHeld >= 5)
				{
					bitsHeld -= 5;
					builder.Append(Alphabet[(buffer >> bitsHeld) & 0x1F]);
				}
			}

			if (bitsHeld > 0)
			{
				builder.Append(Alphabet[(buffer << (5 - bitsHeld)) & 0x1F]);
			}

			return builder.ToString();
		}

		private static bool TryDecode(string text, out byte[] bytes)
		{
			bytes = null;

			StringBuilder cleaned = new StringBuilder(text.Length);
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (c == '-' || c == ' ' || c == '_')
				{
					continue;
				}

				cleaned.Append(char.ToUpperInvariant(c));
			}

			string compact = cleaned.ToString();
			if (compact.Length != EncodedLength)
			{
				return false;
			}

			byte[] result = new byte[PayloadSize];

			int buffer = 0;
			int bitsHeld = 0;
			int written = 0;

			for (int i = 0; i < compact.Length; i++)
			{
				int value = Alphabet.IndexOf(compact[i]);
				if (value < 0)
				{
					return false;
				}

				buffer = (buffer << 5) | value;
				bitsHeld += 5;

				if (bitsHeld >= 8)
				{
					bitsHeld -= 8;
					if (written >= PayloadSize)
					{
						return false;
					}

					result[written++] = (byte)((buffer >> bitsHeld) & 0xFF);
				}
			}

			if (written != PayloadSize)
			{
				return false;
			}

			bytes = result;
			return true;
		}
	}

	/// <summary>
	/// Decoded contents of a join code.
	/// </summary>
	public class SessionCodeData
	{
		public SessionCodeData(int protocolVersion, Version gameVersion, System.Net.IPAddress address, int port, byte[] secret)
		{
			ProtocolVersion = protocolVersion;
			GameVersion = gameVersion;
			Address = address;
			Port = port;
			Secret = secret;
		}

		/// <summary>Wire-format version the host that made this code speaks.</summary>
		public int ProtocolVersion { get; }

		/// <summary>
		/// Whether the wire format matches what this build speaks. Checked
		/// separately from the game version so a protocol regression is
		/// distinguishable from an ordinary version mismatch.
		/// </summary>
		public bool ProtocolMatches => ProtocolVersion == OpenFrontier.IP.Multiplayer.ProtocolVersion.Current;

		/// <summary>Game version the host is running.</summary>
		public Version GameVersion { get; }

		public System.Net.IPAddress Address { get; }

		public int Port { get; }

		/// <summary>Shared secret proving the joiner was invited.</summary>
		public byte[] Secret { get; }

		/// <summary>
		/// Whether this build could actually talk to the host that made the
		/// code. Requires an exact match on both the wire format and the game
		/// version - the rule is that only identical builds may share a session.
		/// </summary>
		public bool IsCompatibleWithLocalBuild(out string reason)
		{
			if (!ProtocolMatches)
			{
				reason = "That code is from a different version of Open Frontier ("
					+ ProtocolVersion + " vs " + OpenFrontier.IP.Multiplayer.ProtocolVersion.Current + ").";
				return false;
			}

			Version localVersion = Versioning.Version;
			if (GameVersion != localVersion)
			{
				reason = "That code is for Open Frontier " + GameVersion
					+ ", and you're running " + localVersion + ". Players must match versions exactly.";
				return false;
			}

			reason = null;
			return true;
		}
	}

	/// <summary>
	/// Outcome of parsing a code: either data, or a message fit to show a player.
	/// </summary>
	public struct SessionCodeParseResult
	{
		private SessionCodeParseResult(SessionCodeData data, string error)
		{
			Data = data;
			Error = error;
		}

		public SessionCodeData Data { get; }

		/// <summary>Player-facing reason the code could not be used, or null on success.</summary>
		public string Error { get; }

		public bool Success => Data != null;

		public static SessionCodeParseResult Ok(SessionCodeData data)
		{
			return new SessionCodeParseResult(data, null);
		}

		public static SessionCodeParseResult Fail(string error)
		{
			return new SessionCodeParseResult(null, error);
		}
	}
}