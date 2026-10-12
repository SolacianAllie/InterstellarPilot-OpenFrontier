using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The host saying a ship has stopped existing.
	/// </summary>
	/// <remarks>
	/// The other half of NewUnitMessage, and mandatory rather than optional
	/// because of it: a client that builds everything the host names
	/// accumulates copies, so a copy that is never removed is a frozen corpse
	/// hanging in space for the rest of the session.
	///
	/// It carries WHICH kind of ending, because the two need opposite
	/// treatment and a client that guessed would be wrong in the direction that
	/// shows. Selling a ship, dismantling it and letting a cargo unit expire
	/// all remove the object with no animation; being shot down is animated,
	/// and so is being ditched, because a ditched unit is killed. So the kinds
	/// are killed and removed, and they are told apart here rather than left to
	/// the receiver - a ship you sold must not explode.
	///
	/// Killed is said at the moment of the kill, not at the moment the object
	/// is destroyed, because the animated explosion plays in between and the
	/// client would otherwise set it off a full animation-length late.
	///
	/// The receiving copy runs the game's own destruction - KillAnonymously
	/// or SafeDestroy - which is the same rule the fleet orders follow: the
	/// host does not reimplement what happened, the client calls the identical
	/// code. That is what makes the explosion, debris and sound play natively
	/// rather than being described.
	///
	/// No attacker, because the client has no use for one and inventing one
	/// would put a stranger into its own kill records.
	/// </remarks>
	public struct UnitGoneMessage
	{
		/// <summary>It was shot down, or ditched. Play the destruction.</summary>
		public const byte KindKilled = 1;

		/// <summary>Sold, dismantled, expired, or simply gone. Just remove it.</summary>
		public const byte KindRemoved = 0;

		public const int HeaderBytes = 5;

		public byte Kind;
		public int UnitId;

		/// <summary>Was it destroyed, as opposed to merely removed?</summary>
		public bool WasKilled => Kind == KindKilled;

		public static UnitGoneMessage Removed(int unitId)
		{
			return new UnitGoneMessage { Kind = KindRemoved, UnitId = unitId };
		}

		public static UnitGoneMessage Killed(int unitId)
		{
			return new UnitGoneMessage { Kind = KindKilled, UnitId = unitId };
		}

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream(HeaderBytes))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(Kind);
				writer.Write(UnitId);
				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out UnitGoneMessage message, out string error)
		{
			message = default(UnitGoneMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "A unit-gone message is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					byte kind = reader.ReadByte();
					int unitId = reader.ReadInt32();

					if (unitId <= 0)
					{
						error = "A unit-gone message names no unit (" + unitId + ").";
						return false;
					}

					if (kind != KindKilled && kind != KindRemoved)
					{
						error = "A unit-gone message says unit " + unitId + " ended in a"
							+ " way this build does not know (" + kind + "). Refused rather"
							+ " than guessed at, because guessing wrong makes a ship you"
							+ " sold explode.";
						return false;
					}

					message = new UnitGoneMessage { Kind = kind, UnitId = unitId };

					return true;
				}
			}
			catch (System.Exception e)
			{
				error = "A unit-gone message could not be read: " + e.Message;
				return false;
			}
		}
	}
}