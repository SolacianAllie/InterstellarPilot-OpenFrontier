using System;
using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// One piece of the host's world, on its way to a client that has none.
	///
	/// A world is a couple of megabytes. Sending it as one message would work
	/// and would be the wrong thing: it is a single blocking write on the main
	/// thread, long enough to be felt as a freeze, and the far end allocates
	/// the whole thing in one go before a single byte is usable. Chunked, the
	/// host hands out a piece per frame, the client can say how far along it
	/// is, and neither side ever holds more than a chunk of the transfer.
	///
	/// Every chunk repeats the total length. That is wasteful and it is what
	/// makes the far end safe: the allocation happens on the first chunk,
	/// against a number the host supplied, and a hostile or broken host
	/// claiming a gigabyte is refused rather than obeyed.
	/// </summary>
	public struct WorldSnapshotChunkMessage
	{
		/// <summary>Largest world a host may offer, in bytes.</summary>
		/// <remarks>
		/// A ceiling rather than a plan. A saved world is a couple of
		/// megabytes; this is four times that, so a large scenario is not
		/// refused for being large, and anything beyond it is refused rather
		/// than allowed to reserve whatever it asked for.
		/// </remarks>
		public const int MaxSnapshotBytes = 32 * 1024 * 1024;

		/// <summary>Bytes of header before the payload proper.</summary>
		public const int HeaderBytes = 12;

		/// <summary>Which transfer this belongs to, so two can be in flight.</summary>
		public int TransferId;

		/// <summary>How long the whole thing is, repeated on every chunk.</summary>
		public int TotalLength;

		/// <summary>Where in the whole this piece starts.</summary>
		public int Offset;

		/// <summary>The piece itself.</summary>
		public byte[] Data;

		public byte[] ToBytes()
		{
			byte[] data = Data ?? new byte[0];

			using (MemoryStream stream = new MemoryStream(HeaderBytes + data.Length))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(TransferId);
				writer.Write(TotalLength);
				writer.Write(Offset);
				writer.Write(data);
				writer.Flush();
				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out WorldSnapshotChunkMessage message, out string error)
		{
			message = default(WorldSnapshotChunkMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "Chunk is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int transferId = reader.ReadInt32();
					int totalLength = reader.ReadInt32();
					int offset = reader.ReadInt32();

					if (totalLength < 0 || totalLength > MaxSnapshotBytes)
					{
						error = "Snapshot length " + totalLength + " is outside 0-"
							+ MaxSnapshotBytes + ".";
						return false;
					}

					if (offset < 0 || offset > totalLength)
					{
						error = "Chunk offset " + offset + " is outside the snapshot.";
						return false;
					}

					// Bytes still unread, not the length of the whole
					// payload: the three ints above have already been
					// consumed, so the stream's total length is the header
					// plus the data and using it here makes every chunk look
					// like it overruns by twelve bytes.
					int available = (int)(stream.Length - stream.Position);

					if (offset + available > totalLength)
					{
						error = "Chunk runs past the end of the snapshot.";
						return false;
					}

					byte[] data = reader.ReadBytes(available);

					message = new WorldSnapshotChunkMessage
					{
						TransferId = transferId,
						TotalLength = totalLength,
						Offset = offset,
						Data = data
					};

					return true;
				}
			}
			catch (EndOfStreamException)
			{
				error = "Chunk ended early.";
				return false;
			}
			catch (IOException)
			{
				error = "Chunk could not be read.";
				return false;
			}
		}
	}
}
