using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The host saying a ship now exists.
	/// </summary>
	/// <remarks>
	/// The first message in the protocol that can bring something into
	/// existence. Everything else describes a thing both copies already have,
	/// which is why a client could not buy a ship: there was no way to say so.
	///
	/// It carries a unit class rather than a description, because both copies
	/// run the same build and hold the same unit class tables, so the host
	/// instantiating UnitClass.UnitPrefab and a client instantiating the same
	/// prefab produce the same ship - the same reasoning that lets both copies
	/// agree a fleet order means one thing, and that let the dock relay name a
	/// bay rather than pick one.
	///
	/// What this does NOT reproduce is anything that happened to a unit after
	/// it was created: damage, refits, cargo in its hold. None of those are
	/// replicated yet, so none of them are in the message. The boundary is
	/// known rather than guessed - the host creates units from scratch, so a
	/// class id reproduces it exactly, and the day that stops being true the
	/// message needs a component list and this needs rewriting.
	///
	/// A transform is included rather than left to the next state batch,
	/// because a ship that appeared at the world origin and then slid into
	/// place over two seconds would be visible, and "it is here now" is the
	/// whole message.
	/// </remarks>
	public struct NewUnitMessage
	{
		/// <summary>4 ids (16) + 3 position floats (12) + 4 rotation floats (16).</summary>
		public const int HeaderBytes = 16 + 12 + 16;

		public int UnitId;

		/// <summary>Which ship or station this is.</summary>
		public int UnitClassId;

		/// <summary>Who owns it.</summary>
		public int FactionId;

		/// <summary>Which sector it is in.</summary>
		public int SectorId;

		public float PositionX;
		public float PositionY;
		public float PositionZ;

		public float RotationX;
		public float RotationY;
		public float RotationZ;
		public float RotationW;

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream(HeaderBytes))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(UnitId);
				writer.Write(UnitClassId);
				writer.Write(FactionId);
				writer.Write(SectorId);
				writer.Write(PositionX);
				writer.Write(PositionY);
				writer.Write(PositionZ);
				writer.Write(RotationX);
				writer.Write(RotationY);
				writer.Write(RotationZ);
				writer.Write(RotationW);
				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out NewUnitMessage message, out string error)
		{
			message = default(NewUnitMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "A new-unit message is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					NewUnitMessage read = new NewUnitMessage
					{
						UnitId = reader.ReadInt32(),
						UnitClassId = reader.ReadInt32(),
						FactionId = reader.ReadInt32(),
						SectorId = reader.ReadInt32(),
						PositionX = reader.ReadSingle(),
						PositionY = reader.ReadSingle(),
						PositionZ = reader.ReadSingle(),
						RotationX = reader.ReadSingle(),
						RotationY = reader.ReadSingle(),
						RotationZ = reader.ReadSingle(),
						RotationW = reader.ReadSingle()
					};

					// Every id is checked here rather than at the point of use,
					// because the point of use is a dictionary lookup that
					// returns null and then an instantiation that throws, and a
					// message that says "there is a ship with no class" is not a
					// thing the receiver can do anything sensible with.
					if (read.UnitId <= 0)
					{
						error = "A new-unit message names no unit (" + read.UnitId + ").";
						return false;
					}

					if (read.UnitClassId <= 0)
					{
						error = "New unit " + read.UnitId + " has no unit class ("
							+ read.UnitClassId + "), so it cannot be built.";
						return false;
					}

					if (read.FactionId <= 0)
					{
						error = "New unit " + read.UnitId + " has no faction ("
							+ read.FactionId + ").";
						return false;
					}

					if (read.SectorId <= 0)
					{
						error = "New unit " + read.UnitId + " is in no sector ("
							+ read.SectorId + ").";
						return false;
					}

					message = read;

					return true;
				}
			}
			catch (System.Exception e)
			{
				error = "A new-unit message could not be read: " + e.Message;
				return false;
			}
		}
	}
}