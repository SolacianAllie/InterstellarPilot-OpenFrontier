using System;
using System.Collections.Generic;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The sending half of a world transfer.
	/// </summary>
	/// <remarks>
	/// Deliberately kept apart from WorldSnapshotBuilder, which produces the
	/// bytes and needs the engine. This half is arithmetic on an array, so it
	/// can be tested without one - and it should be, because the property
	/// worth having is that whatever this emits comes back out of the
	/// reassembler identical to what went in.
	///
	/// Yields lazily so the caller can send one piece per frame rather than
	/// building every chunk up front and holding a second copy of the world.
	/// </remarks>
	public static class SnapshotChunker
	{
		/// <summary>
		/// How much of the world goes out per message.
		/// </summary>
		/// <remarks>
		/// 64 KB. Large enough that a couple-of-megabyte world is a few dozen
		/// messages rather than thousands, small enough that writing one is a
		/// short job on the main thread rather than a visible hitch - which is
		/// the whole reason this is chunked instead of sent as one message.
		/// </remarks>
		public const int ChunkBytes = 64 * 1024;

		public static IEnumerable<byte[]> Chunk(byte[] snapshot, int transferId)
		{
			if (snapshot == null)
			{
				yield break;
			}

			int total = snapshot.Length;

			// A zero-length world is not a thing, but should one ever arrive
			// the far end would wait forever for a transfer that never
			// completes. One empty message closes it out properly.
			if (total == 0)
			{
				yield return new WorldSnapshotChunkMessage
				{
					TransferId = transferId,
					TotalLength = 0,
					Offset = 0,
					Data = new byte[0]
				}.ToBytes();

				yield break;
			}

			for (int offset = 0; offset < total; offset += ChunkBytes)
			{
				int length = Math.Min(ChunkBytes, total - offset);

				byte[] piece = new byte[length];
				Buffer.BlockCopy(snapshot, offset, piece, 0, length);

				yield return new WorldSnapshotChunkMessage
				{
					TransferId = transferId,
					TotalLength = total,
					Offset = offset,
					Data = piece
				}.ToBytes();
			}
		}
	}
}