using System;
using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Where one unit is, as the host says it is.
	///
	/// This is the message that makes a session visible. Everything else in
	/// the protocol is bookkeeping - is this person allowed in, is that host
	/// up, who are they - and none of it is a game. This is the game.
	///
	/// Host-authoritative: the host simulates every unit and says where they
	/// ended up. Clients send what they want done and never assert where
	/// something is, because two copies of the world guessing independently
	/// would drift apart within seconds and there would be no way to argue
	/// about it afterwards.
	///
	/// Units are named by the same integer in both copies of a world. The
	/// save format writes that integer verbatim and restores the allocator
	/// past it, which is why a joining client is given the host's world rather
	/// than being asked to generate one - ids are handed out in spawn order,
	/// so two copies that merely agreed on a seed could still disagree.
	/// </summary>
	public struct UnitState
	{
		/// <summary>The unit's id in the host's world.</summary>
		public int Id;

		// Plain floats rather than Vector3 and Quaternion, on purpose. The
		// rest of the protocol speaks only System types, which is what lets it
		// be tested without an engine; putting Unity's maths types in here
		// would drag UnityEngine into the codec for the sake of three structs
		// that are just nine floats on a wire. Converting is the sender's and
		// the receiver's job, and it happens once per unit per update.

		/// <summary>Position within the unit's sector.</summary>
		public float PositionX;
		public float PositionY;
		public float PositionZ;

		public float RotationX;
		public float RotationY;
		public float RotationZ;
		public float RotationW;

		/// <summary>Metres per second, so a client can extrapolate between updates.</summary>
		public float VelocityX;
		public float VelocityY;
		public float VelocityZ;

		/// <summary>Current turn rate, so a client can keep turning smoothly.</summary>
		public float CurrentTurn;

		/// <summary>
		/// How hard the engines are being pushed, 0 to 1.
		/// </summary>
		/// <remarks>
		/// Not motion - the position already says where the ship is. This says
		/// what the ship is *trying* to do, and it is what a client cannot
		/// work out from a position: a ship coasting and a ship at full
		/// throttle with the throttle shut are in the same place at the same
		/// time, and only this says which.
		///
		/// Without it every engine plume and every ion trail on a client's
		/// copy is driven by whatever the last thing the renderer saw was,
		/// which for a ship somebody else is flying is nothing at all. The
		/// ships look dead, and the world looks frozen, while the positions
		/// are perfectly correct.
		///
		/// Interpolated like everything else rather than applied as it arrives,
		/// because a plume that steps is the same artefact as a ship that
		/// steps - and interpolating a number is cheaper than interpolating a
		/// ship's position, so there was never a reason to treat it as less
		/// important.
		/// </remarks>
		public float Throttle;
	}

	/// <summary>A batch of unit states, sent as one write.</summary>
	/// <remarks>
	/// Batched rather than sent per unit because each one is a separate write
	/// on a separate socket, and at a hundred ships a frame that is a hundred
	/// syscalls to say something a phone could carry in a single packet.
	/// </remarks>
	public struct UnitStateMessage
	{
		/// <summary>How many units one message may carry.</summary>
		/// <remarks>
		/// A ceiling rather than a plan. One message is one write on one
		/// thread, and a session that has somehow accumulated thousands of
		/// units should spread them over more messages rather than hand the
		/// socket a buffer nobody reads before the next frame.
		/// </remarks>
		public const int MaxUnits = 64;

		/// <summary>
		/// Bytes one unit costs: an int id, three floats of position, a
		/// quaternion, three floats of velocity, a float of turn, and a float
		/// of throttle.
		/// </summary>
		/// <remarks>
		/// Spelled out rather than measured so the decoder can check the
		/// payload length before it allocates, which it must: the count comes
		/// off the network. Adding a field means changing this, and the test
		/// that checks the two agree is what catches it when it does not.
		/// </remarks>
		public const int UnitBytes = 4 + (3 * 4) + (4 * 4) + (3 * 4) + 4 + 4;

		/// <summary>
		/// Bytes before the first unit: an int count, a float time, an int
		/// sector.
		/// </summary>
		public const int HeaderBytes = 8 + 4;

		/// <summary>Units in this message.</summary>
		public UnitState[] Units;

		/// <summary>
		/// When the host took this sample, in seconds since it started.
		/// </summary>
		/// <remarks>
		/// One clock for the whole batch rather than one per unit, because
		/// they were all taken in the same frame and saying so sixteen times
		/// is sixteen chances to be subtly wrong.
		///
		/// Sent rather than stamped on arrival, because arrival time is not
		/// sampling time. Packets bunch up and spread out, and interpolating
		/// between arrival times makes ships change speed with the network
		/// instead of with their engines - which looks like the network
		/// having opinions about the game. The client keeps an offset
		/// between the two clocks and draws against the host's.
		/// </remarks>
		public float Time;

		/// <summary>
		/// The sector this batch is about - the one the recipient's own ship
		/// is in.
		/// </summary>
		/// <remarks>
		/// Sent because the host already knows it and was using it only to
		/// choose which ships to include. Without saying so, a client stays in
		/// whichever sector it arrived in and is sent the right ships for a
		/// place it is not: the host filters correctly and the result is
		/// invisibly wrong, which is the worst shape of bug this project has.
		///
		/// Part of the batch rather than its own message because it changes
		/// rarely and the batch already goes out several times a second, so a
		/// message for it would be almost entirely silence.
		///
		/// Zero means the host has not said, which is what a build from
		/// before this field sends. The client leaves the sector alone rather
		/// than guessing one.
		/// </remarks>
		public int SectorId;

		public int Count => Units?.Length ?? 0;

		public byte[] ToBytes()
		{
			int count = Math.Min(Count, MaxUnits);

			using (MemoryStream stream = new MemoryStream(
				HeaderBytes + (count * UnitBytes)))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(count);
				writer.Write(Time);
				writer.Write(SectorId);

				for (int i = 0; i < count; i++)
				{
					UnitState unit = Units[i];

					writer.Write(unit.Id);
					writer.Write(unit.PositionX);
					writer.Write(unit.PositionY);
					writer.Write(unit.PositionZ);
					writer.Write(unit.RotationX);
					writer.Write(unit.RotationY);
					writer.Write(unit.RotationZ);
					writer.Write(unit.RotationW);
					writer.Write(unit.VelocityX);
					writer.Write(unit.VelocityY);
					writer.Write(unit.VelocityZ);
					writer.Write(unit.CurrentTurn);
					writer.Write(unit.Throttle);
				}

				writer.Flush();
				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out UnitStateMessage message, out string error)
		{
			message = default(UnitStateMessage);
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
					int count = reader.ReadInt32();
					float time = reader.ReadSingle();
					int sectorId = reader.ReadInt32();

					// Bounded before allocating, from a number that arrived
					// over a socket. A peer claiming a million units gets a
					// refusal, not a multi-megabyte array.
					if (count < 0 || count > MaxUnits)
					{
						error = "Unit count " + count + " is outside 0-" + MaxUnits + ".";
						return false;
					}

					if (stream.Length < HeaderBytes + ((long)count * UnitBytes))
					{
						error = "Payload holds " + stream.Length
							+ " bytes, short of the "
							+ (HeaderBytes + ((long)count * UnitBytes))
							+ " that " + count + " units need.";
						return false;
					}

					UnitState[] units = new UnitState[count];

					for (int i = 0; i < count; i++)
					{
						units[i] = new UnitState
						{
							Id = reader.ReadInt32(),
							PositionX = reader.ReadSingle(),
							PositionY = reader.ReadSingle(),
							PositionZ = reader.ReadSingle(),
							RotationX = reader.ReadSingle(),
							RotationY = reader.ReadSingle(),
							RotationZ = reader.ReadSingle(),
							RotationW = reader.ReadSingle(),
							VelocityX = reader.ReadSingle(),
							VelocityY = reader.ReadSingle(),
							VelocityZ = reader.ReadSingle(),
							CurrentTurn = reader.ReadSingle(),
							Throttle = reader.ReadSingle()
						};
					}

					message = new UnitStateMessage
					{
						Units = units,
						Time = time,
						SectorId = sectorId
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
