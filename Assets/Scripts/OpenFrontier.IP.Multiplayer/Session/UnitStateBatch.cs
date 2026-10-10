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
		/// Packs states into as few messages as the ceiling allows.
		/// </summary>
		/// <returns>
		/// One message per <see cref="UnitStateMessage.MaxUnits"/> states,
		/// last one short. Empty input gives an empty list rather than one
		/// empty message - "nothing moved" and "I moved nothing" are the same
		/// thing to the client, and sending the second wastes a write.
		/// </returns>
		public static List<UnitStateMessage> Pack(IReadOnlyList<UnitState> states)
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

				messages.Add(new UnitStateMessage { Units = batch });
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
