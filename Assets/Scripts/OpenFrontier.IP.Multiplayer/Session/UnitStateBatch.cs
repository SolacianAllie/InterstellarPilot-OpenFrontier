using System.Collections.Generic;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Splits a frame's unit states into messages that fit the wire.
	/// </summary>
	/// <remarks>
	/// Separate from both ends on purpose. The host has units and a socket,
	/// the client has a socket and units, and neither of those can be built
	/// outside Unity - but the question in between, "how do these states
	/// become messages", is pure and is exactly where the mistakes live:
	/// dropping a unit silently, emitting an empty message, or writing a batch
	/// the other side will refuse for being oversized.
	/// </remarks>
	public static class UnitStateBatch
	{
		/// <summary>
		/// Packs states into as few messages as the ceiling allows, all
		/// stamped with the moment they were taken.
		/// </summary>
		/// <param name="states">The samples to send.</param>
		/// <param name="time">
		/// When they were taken, in seconds on the sender's clock. Passed in
		/// rather than assigned afterwards because a List hands back a copy
		/// from its indexer, so writing Time onto a packed message is a write
		/// into a temporary that is then thrown away - which compiles, and
		/// sends a time of zero to every client that then draws its ships
		/// from a clock it believes is stopped.
		/// </param>
		/// <param name="sectorId">
		/// The sector these states are in. Passed in and set in the
		/// constructor for the same reason as the time: a List hands back a
		/// copy from its indexer, so writing either field onto a packed
		/// message afterwards writes into a temporary and throws it away.
		/// </param>
		/// <returns>
		/// One message per <see cref="UnitStateMessage.MaxUnits"/> states,
		/// last one short. Empty input gives an empty list rather than one
		/// empty message - "nothing moved" and "I moved nothing" are the same
		/// thing to the client, and sending the second wastes a write.
		/// </returns>
		public static List<UnitStateMessage> Pack(
			IReadOnlyList<UnitState> states, float time = 0f, int sectorId = 0)
		{
			List<UnitStateMessage> messages = new List<UnitStateMessage>();

			if (states == null || states.Count == 0)
			{
				return messages;
			}

			for (int offset = 0; offset < states.Count; offset += UnitStateMessage.MaxUnits)
			{
				int count = System.Math.Min(UnitStateMessage.MaxUnits, states.Count - offset);
				UnitState[] batch = new UnitState[count];

				for (int i = 0; i < count; i++)
				{
					batch[i] = states[offset + i];
				}

				messages.Add(new UnitStateMessage
				{
					Units = batch,
					Time = time,
					SectorId = sectorId
				});
			}

			return messages;
		}

		/// <summary>
		/// How many messages a given number of units will take.
		/// </summary>
		public static int MessageCount(int unitCount)
		{
			if (unitCount <= 0)
			{
				return 0;
			}

			return (unitCount + UnitStateMessage.MaxUnits - 1) / UnitStateMessage.MaxUnits;
		}
	}
}
