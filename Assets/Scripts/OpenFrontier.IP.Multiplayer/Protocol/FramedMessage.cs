using System;
using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// A message on the wire: a message id then its payload.
	///
	/// The id lets the receiver dispatch without knowing every payload type
	/// up front, and keeps the framing versionable - a new message can be added
	/// without shifting the meaning of anything already on the wire.
	/// </summary>
	public static class FramedMessage
	{
		public const int IdBytes = 4;

		public static byte[] Create(int messageId, byte[] payload)
		{
			payload = payload ?? Array.Empty<byte>();

			using (MemoryStream stream = new MemoryStream(IdBytes + payload.Length))
			{
				using (BinaryWriter writer = new BinaryWriter(stream))
				{
					// BinaryWriter is little-endian, so the id is written here and
					// read back with BinaryReader in TrySplit. Both ends must agree
					// on byte order - writing big-endian by hand and reading with
					// BinaryReader (or the reverse) silently produces a byte-shifted
					// id.
					writer.Write(messageId);
					writer.Write(payload);
				}

				return stream.ToArray();
			}
		}

		/// <summary>
		/// Splits a framed message. Returns false for anything too short or
		/// truncated rather than throwing.
		/// </summary>
		public static bool TrySplit(byte[] framed, out int messageId, out byte[] payload)
		{
			messageId = 0;
			payload = null;

			if (framed == null || framed.Length < IdBytes)
			{
				return false;
			}

			// Read with BinaryReader so the byte order matches BinaryWriter in Create.
			// Hand-decoding this big-endian is the classic way to get an id that
			// looks plausible shifted by three bytes.
			using (MemoryStream stream = new MemoryStream(framed, writable: false))
			using (BinaryReader reader = new BinaryReader(stream))
			{
				messageId = reader.ReadInt32();

				int payloadLength = framed.Length - IdBytes;
				payload = reader.ReadBytes(payloadLength);
			}

			return true;
		}
	}
}