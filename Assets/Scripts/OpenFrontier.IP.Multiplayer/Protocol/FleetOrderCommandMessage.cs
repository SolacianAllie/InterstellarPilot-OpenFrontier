using System;
using System.IO;
using OpenFrontier.IP.Engine.FleetOrders;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// A player asking the host to give one of their fleets an order.
	/// </summary>
	/// <remarks>
	/// A client's order is not executed on the client. It is sent here, and
	/// the host calls the very method a local click would have called, so
	/// there is one implementation of "what mine asteroids does" rather than
	/// two that have to be kept in step.
	///
	/// The order names a fleet by id rather than the units the player had
	/// selected. The host owns fleet composition - a client's idea of which
	/// ships are in which fleet is a copy, not the truth - and both copies
	/// already agree on ids, because the client was handed the host's world
	/// rather than generating one of its own. Naming the fleet is therefore
	/// both smaller and more correct than naming the selection.
	///
	/// Every field is a plain number on purpose. The rest of the protocol
	/// speaks only System types, which is what lets a command be tested
	/// without a game running; putting Unity's maths types in here would drag
	/// UnityEngine into the codec for the sake of one vector.
	/// </remarks>
	public struct FleetOrderCommandMessage
	{
		/// <summary>How many ids one message may name.</summary>
		public const int MaxFleets = 16;

		/// <summary>
		/// Bytes before the first fleet id.
		/// </summary>
		/// <remarks>
		/// Spelled out rather than measured, because the decoder has to check
		/// a payload against this number before it allocates anything from it,
		/// and that number arrives from the network. Thirteen ints: the kind,
		/// three target ids, a cargo class, a position, a parameter, three
		/// flags, and the count.
		/// </remarks>
		public const int HeaderBytes = 14 * 4;

		/// <summary>Which order.</summary>
		public FleetOrderKind Kind;

		/// <summary>The fleets to give it to, in the player's selection order.</summary>
		public int[] FleetIds;

		/// <summary>Selected ships not yet in a fleet, for the host to build one for.</summary>
		public int[] LooseUnitIds;

		/// <summary>For orders that name a unit: what to dock with, claim, collect.</summary>
		public int TargetUnitId;

		/// <summary>For orders that name a sector.</summary>
		public int TargetSectorId;

		/// <summary>For orders that name a fleet: the one to merge into or protect.</summary>
		public int TargetFleetId;

		/// <summary>For trade: what to carry.</summary>
		public int CargoClassId;

		/// <summary>Where in the sector, for a position rather than an object.</summary>
		public float TargetX;
		public float TargetY;
		public float TargetZ;

		/// <summary>For rearm orders: how much of the equipment to use, 0 to 1.</summary>
		public float Parameter01;

		/// <summary>Add to the queue rather than replacing it.</summary>
		public bool Stack;

		/// <summary>For patrol: return to the start.</summary>
		public bool Loop;

		/// <summary>For patrol: go round again.</summary>
		public bool Repeat;

		public int Count => FleetIds?.Length ?? 0;

		public byte[] ToBytes()
		{
			int count = Math.Min(Count, MaxFleets);
			int looseCount = LooseUnitIds == null
				? 0
				: Math.Min(LooseUnitIds.Length, MaxFleets);

			using (MemoryStream stream = new MemoryStream(
				HeaderBytes + ((count + looseCount) * 4)))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write((int)Kind);
				writer.Write(TargetUnitId);
				writer.Write(TargetSectorId);
				writer.Write(TargetFleetId);
				writer.Write(CargoClassId);
				writer.Write(TargetX);
				writer.Write(TargetY);
				writer.Write(TargetZ);
				writer.Write(Parameter01);
				writer.Write(Stack ? 1 : 0);
				writer.Write(Loop ? 1 : 0);
				writer.Write(Repeat ? 1 : 0);
				writer.Write(count);
				writer.Write(looseCount);

				for (int i = 0; i < count; i++)
				{
					writer.Write(FleetIds[i]);
				}

				for (int i = 0; i < looseCount; i++)
				{
					writer.Write(LooseUnitIds[i]);
				}

				writer.Flush();
				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out FleetOrderCommandMessage message, out string error)
		{
			message = default(FleetOrderCommandMessage);
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
					int kind = reader.ReadInt32();
					int targetUnit = reader.ReadInt32();
					int targetSector = reader.ReadInt32();
					int targetFleet = reader.ReadInt32();
					int cargoClass = reader.ReadInt32();
					float x = reader.ReadSingle();
					float y = reader.ReadSingle();
					float z = reader.ReadSingle();
					float parameter = reader.ReadSingle();
					bool stack = reader.ReadInt32() != 0;
					bool loop = reader.ReadInt32() != 0;
					bool repeat = reader.ReadInt32() != 0;
					int count = reader.ReadInt32();
					int looseCount = reader.ReadInt32();

					// Bounded before allocating, from a number that arrived over a
					// socket. See UnitStateMessage for why this is not optional.
					if (count < 0 || count > MaxFleets
						|| looseCount < 0 || looseCount > MaxFleets)
					{
						error = "Count " + count + " fleets and " + looseCount
							+ " loose ships is outside 0-" + MaxFleets + " each.";
						return false;
					}

					if (stream.Length < HeaderBytes + (((long)count + looseCount) * 4))
					{
						error = "Payload holds " + stream.Length
							+ " bytes, short of the "
							+ (HeaderBytes + (((long)count + looseCount) * 4))
							+ " that " + (count + looseCount) + " ids need.";
						return false;
					}

					int[] fleets = new int[count];

					for (int i = 0; i < count; i++)
					{
						fleets[i] = reader.ReadInt32();
					}

					int[] loose = new int[looseCount];

					for (int i = 0; i < looseCount; i++)
					{
						loose[i] = reader.ReadInt32();
					}

					// Refused rather than guessed at: two builds that
					// disagree about what 17 is would otherwise have one of
					// them run the wrong order on a player's fleet, which is
					// worse than doing nothing and much harder to notice.
					if (!Enum.IsDefined(typeof(FleetOrderKind), kind))
					{
						error = "Order kind " + kind + " is not one this build knows.";
						return false;
					}

					message = new FleetOrderCommandMessage
					{
						Kind = (FleetOrderKind)kind,
						TargetUnitId = targetUnit,
						TargetSectorId = targetSector,
						TargetFleetId = targetFleet,
						CargoClassId = cargoClass,
						TargetX = x,
						TargetY = y,
						TargetZ = z,
						Parameter01 = parameter,
						Stack = stack,
						Loop = loop,
						Repeat = repeat,
						FleetIds = fleets,
						LooseUnitIds = loose
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
