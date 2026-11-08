using System;
using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>One kind of goods, and how much of it.</summary>
	public struct CargoStack
	{
		/// <summary>The cargo class's id in the host's world.</summary>
		public int ClassId;

		public int Quantity;
	}

	/// <summary>
	/// What the host says a player's faction owns, as of right now.
	///
	/// Host-authoritative, like everything else here: the client is told its
	/// balance and its hold, and is never the one deciding either. This is
	/// what makes step two of the design tractable - once this exists, a
	/// client that sends "buy this" needs no answer, because the change to
	/// its credits and cargo arrives here whether the host agreed or refused.
	/// The push is the receipt.
	///
	/// Sent per player rather than broadcast, because it is about one
	/// person's money. Two people in a session have two balances, and a
	/// broadcast would tell each of them the other's.
	/// </summary>
	public struct FactionStateMessage
	{
		/// <summary>How many cargo stacks one message may carry.</summary>
		public const int MaxStacks = 64;

		/// <summary>Bytes one stack costs: two ints.</summary>
		public const int StackBytes = 8;

		/// <summary>The faction this state belongs to.</summary>
		public int FactionId;

		/// <summary>The faction's credits, in whole units.</summary>
		public int Credits;

		/// <summary>
		/// The player's current ship, whose hold this describes.
		/// </summary>
		/// <remarks>
		/// Sent alongside the cargo because cargo is not a faction property -
		/// it lives in a bay on a ship - so a client cannot work out which bay
		/// is meant without being told. It also means a client whose ship was
		/// just destroyed, or has docked somewhere else, is holding cargo that
		/// belongs to a hold it is no longer in, and can tell.
		/// </remarks>
		public int ShipId;

		public CargoStack[] Cargo;

		public int Count => Cargo?.Length ?? 0;

		public byte[] ToBytes()
		{
			int count = Math.Min(Count, MaxStacks);

			using (MemoryStream stream = new MemoryStream(12 + (count * StackBytes)))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(FactionId);
				writer.Write(Credits);
				writer.Write(ShipId);
				writer.Write(count);

				for (int i = 0; i < count; i++)
				{
					writer.Write(Cargo[i].ClassId);
					writer.Write(Cargo[i].Quantity);
				}

				writer.Flush();
				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out FactionStateMessage message, out string error)
		{
			message = default(FactionStateMessage);
			error = null;

			if (payload == null)
			{
				error = "No payload.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int factionId = reader.ReadInt32();
					int credits = reader.ReadInt32();
					int shipId = reader.ReadInt32();
					int count = reader.ReadInt32();

					// Bounded before allocating, from numbers that arrived over
					// a socket. See UnitStateMessage for why this matters.
					if (count < 0 || count > MaxStacks)
					{
						error = "Cargo count " + count + " is outside 0-" + MaxStacks + ".";
						return false;
					}

					if (stream.Length < 16L + ((long)count * StackBytes))
					{
						error = "Payload holds " + stream.Length
							+ " bytes, short of the " + (16L + ((long)count * StackBytes))
							+ " that " + count + " cargo stacks need.";
						return false;
					}

					CargoStack[] cargo = new CargoStack[count];

					for (int i = 0; i < count; i++)
					{
						cargo[i] = new CargoStack
						{
							ClassId = reader.ReadInt32(),
							Quantity = reader.ReadInt32()
						};
					}

					message = new FactionStateMessage
					{
						FactionId = factionId,
						Credits = credits,
						ShipId = shipId,
						Cargo = cargo
					};

					return true;
				}
			}
			catch (EndOfStreamException)
			{
				error = "Payload ended early.";
				return false;
			}
			catch (IOException)
			{
				error = "Payload could not be read.";
				return false;
			}
		}
	}
}