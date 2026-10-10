using System;
using System.Collections.Generic;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Puts a world back together from whatever order it arrives in.
	/// </summary>
	/// <remarks>
	/// The counterpart to the host's chunking, and deliberately fussier than
	/// it looks. TCP gives ordered delivery for one connection, so this looks
	/// like it could not go wrong - but two things make it have to: a session
	/// can have a transfer already half-finished when another begins, and a
	/// chunk that is out of place or duplicated must not be allowed to quietly
	/// corrupt the result. A world loaded with a hole in it looks like a game
	/// bug, not a network one, and would be very hard to trace back to here.
	///
	/// Every piece is checked against the total the host declared, and the
	/// transfer is only handed over once every byte of it has arrived.
	/// </remarks>
	public sealed class SnapshotReassembler
	{
		private readonly Dictionary<int, Transfer> transfers = new Dictionary<int, Transfer>();

		/// <summary>How many transfers are open. More than one is allowed.</summary>
		public int OpenTransfers => transfers.Count;

		private sealed class Transfer
		{
			public int TotalLength;
			public byte[] Buffer;

			/// <summary>
			/// Which bytes have actually arrived.
			/// </summary>
			/// <remarks>
			/// A bitmap rather than a count of bytes received, because the two
			/// are not the same: a chunk sent twice adds its length twice and
			/// would declare a transfer finished while a region in the middle
			/// was still a hole. A world loaded with a hole in it is
			/// indistinguishable from a game bug, so the coverage is tracked
			/// exactly rather than inferred.
			/// </remarks>
			public byte[] Covered;

			public int CoveredBytes;
		}

		/// <summary>
		/// Takes one chunk. Hands back the finished snapshot when the last one
		/// lands, and nothing at all otherwise.
		/// </summary>
		public bool Accept(byte[] payload, out byte[] completed, out string error)
		{
			completed = null;
			error = null;

			if (!WorldSnapshotChunkMessage.TryFromBytes(payload, out WorldSnapshotChunkMessage chunk, out error))
			{
				return false;
			}

			if (!transfers.TryGetValue(chunk.TransferId, out Transfer transfer))
			{
				// A new transfer. Allocated against the total the host
				// declared, which the decoder has already bounded.
				transfer = new Transfer
				{
					TotalLength = chunk.TotalLength,
					Buffer = new byte[chunk.TotalLength],
					Covered = new byte[(chunk.TotalLength + 7) / 8]
				};

				transfers[chunk.TransferId] = transfer;
			}
			else if (transfer.TotalLength != chunk.TotalLength)
			{
				// The host changed its mind mid-transfer. Refused rather than
				// resized: resizing would mean discarding everything received
				// so far, and quietly continuing would produce a world that is
				// half one thing and half another.
				transfers.Remove(chunk.TransferId);
				error = "The snapshot changed size mid-transfer (was " + transfer.TotalLength
					+ ", now " + chunk.TotalLength + ").";
				return false;
			}

			if (chunk.Data != null && chunk.Data.Length > 0)
			{
				Buffer.BlockCopy(chunk.Data, 0, transfer.Buffer, chunk.Offset, chunk.Data.Length);

				for (int i = 0; i < chunk.Data.Length; i++)
				{
					int at = chunk.Offset + i;

					if (((transfer.Covered[at / 8] >> (at % 8)) & 1) == 0)
					{
						transfer.Covered[at / 8] |= (byte)(1 << (at % 8));
						transfer.CoveredBytes++;
					}
				}
			}

			if (transfer.CoveredBytes < transfer.TotalLength)
			{
				// Not finished. Chunks may arrive out of order, be duplicated,
				// or be the same bytes twice, and none of those may be allowed
				// to look like completion.
				return true;
			}

			transfers.Remove(chunk.TransferId);
			completed = transfer.Buffer;
			return true;
		}

		/// <summary>Forgets a transfer, e.g. when the peer went away mid-way.</summary>
		public void Abandon(int transferId)
		{
			transfers.Remove(transferId);
		}

		/// <summary>Forgets everything, e.g. when a join is abandoned.</summary>
		public void Clear()
		{
			transfers.Clear();
		}
	}
}
