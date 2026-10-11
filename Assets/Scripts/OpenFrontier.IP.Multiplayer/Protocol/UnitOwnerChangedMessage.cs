using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The host saying a ship now belongs to somebody else, or to nobody.
	/// </summary>
	/// <remarks>
	/// A third thing, and the one that is neither a ship appearing nor a ship
	/// going away. Abandoning a ship sets its faction to null and leaves it
	/// there: it floats on, unowned, and either side may claim it. That is the
	/// opposite of the two endings UnitGoneMessage carries, and it was missing
	/// between them - so a client watching a ship get abandoned saw it still
	/// owned by the faction that no longer had it, and could not claim it even
	/// once the host said so.
	///
	/// Separate from NewUnitMessage for a reason that matters later: the same
	/// message covers a captured ship changing hands, and a ship whose cargo
	/// was taken off it, and anything else where the object survives and only
	/// its owner changes. Those are facts about ownership rather than about
	/// existence, and mixing them into the create message would mean every
	/// receiver had to decide which of the two it was looking at.
	///
	/// Faction id zero means nobody. It has to be expressible, because the
	/// whole point is the case where a ship has no owner - and a message that
	/// could only say "belongs to X" could not describe the moment a ship
	/// stops belonging to anyone, which is exactly the moment that makes it
	/// claimable.
	/// </remarks>
	public struct UnitOwnerChangedMessage
	{
		/// <summary>No faction. The ship is free for anybody to claim.</summary>
		public const int Unowned = 0;

		public const int HeaderBytes = 8;

		public int UnitId;
		public int FactionId;

		public bool IsUnowned => FactionId == Unowned;

		public static UnitOwnerChangedMessage OwnedBy(int unitId, int factionId)
		{
			return new UnitOwnerChangedMessage { UnitId = unitId, FactionId = factionId };
		}

		public static UnitOwnerChangedMessage Abandoned(int unitId)
		{
			return new UnitOwnerChangedMessage { UnitId = unitId, FactionId = Unowned };
		}

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream(HeaderBytes))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(UnitId);
				writer.Write(FactionId);
				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out UnitOwnerChangedMessage message, out string error)
		{
			message = default(UnitOwnerChangedMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "An owner-change message is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int unitId = reader.ReadInt32();
					int factionId = reader.ReadInt32();

					if (unitId <= 0)
					{
						error = "An owner-change message names no unit (" + unitId + ").";
						return false;
					}

					// Zero is the one legal way to have no faction, and it is
					// the interesting one. Anything else is a number that does
					// not name a faction, which is not something to guess at.
					if (factionId < 0)
					{
						error = "An owner-change message puts unit " + unitId
							+ " under faction " + factionId
							+ ", which is neither a faction nor unowned.";
						return false;
					}

					message = new UnitOwnerChangedMessage
					{
						UnitId = unitId,
						FactionId = factionId
					};

					return true;
				}
			}
			catch (System.Exception e)
			{
				error = "An owner-change message could not be read: " + e.Message;
				return false;
			}
		}
	}
}