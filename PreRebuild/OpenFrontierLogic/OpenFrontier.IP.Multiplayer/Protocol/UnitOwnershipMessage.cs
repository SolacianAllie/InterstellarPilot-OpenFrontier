using System;
using System.IO;
using System.Text;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Who is flying which ship.
	/// </summary>
	/// <remarks>
	/// One struct for both directions, because the two are the same fact
	/// argued about:
	///
	/// A Request is a client saying "I am taking this ship", and a host
	/// saying "this ship is the host's now". An Assignment is the host's
	/// answer - who owns it, or nobody.
	///
	/// The host decides and the client obeys, deliberately. Letting a client
	/// simply assert ownership would mean two players could claim one ship
	/// and neither would find out until their ships started moving through
	/// each other. Going through the host costs one round trip and makes the
	/// answer the same for everybody.
	///
	/// An empty owner token means the host has taken the ship back - the
	/// player put it on autopilot, or disconnected. It is a real value rather
	/// than an absent message so that "released" is something a client can be
	/// told, instead of something it has to infer from silence.
	/// </remarks>
	public struct UnitOwnershipMessage
	{
		/// <summary>A client asking for a ship.</summary>
		public const byte KindRequest = 0;

		/// <summary>The host's answer: this ship belongs to that player.</summary>
		public const byte KindAssignment = 1;

		/// <summary>Bytes before the length-prefixed token.</summary>
		public const int HeaderBytes = 5;

		/// <summary>Longest player token accepted, in bytes.</summary>
		public const int MaxTokenBytes = 32;

		/// <summary>Request, or Assignment. See the constants.</summary>
		public byte Kind;

		/// <summary>The ship being talked about.</summary>
		public int UnitId;

		/// <summary>Owner token, for an Assignment. Empty means the host has it.</summary>
		public string OwnerToken;

		/// <summary>Whether this message releases the ship to the host.</summary>
		public bool IsRelease => Kind == KindAssignment
			&& string.IsNullOrEmpty(OwnerToken);

		public static UnitOwnershipMessage Request(int unitId)
		{
			return new UnitOwnershipMessage
			{
				Kind = KindRequest,
				UnitId = unitId,
				OwnerToken = string.Empty
			};
		}

		public static UnitOwnershipMessage Assign(int unitId, string ownerToken)
		{
			return new UnitOwnershipMessage
			{
				Kind = KindAssignment,
				UnitId = unitId,
				OwnerToken = ownerToken ?? string.Empty
			};
		}

		public byte[] ToBytes()
		{
			byte[] token = Encoding.UTF8.GetBytes(OwnerToken ?? string.Empty);

			if (token.Length > MaxTokenBytes)
			{
				token = new byte[MaxTokenBytes];
			}

			using (MemoryStream stream = new MemoryStream(HeaderBytes + token.Length))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(Kind);
				writer.Write(UnitId);
				writer.Write(token.Length);
				writer.Write(token);
				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out UnitOwnershipMessage message, out string error)
		{
			message = default(UnitOwnershipMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "Ownership message is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					byte kind = reader.ReadByte();
					int unitId = reader.ReadInt32();
					int tokenLength = reader.ReadInt32();

					// The length decides the allocation, so it is checked
					// before anything is reserved for it.
					if (tokenLength < 0 || tokenLength > MaxTokenBytes)
					{
						error = "Token length " + tokenLength + " is out of range.";
						return false;
					}

					int available = (int)(stream.Length - stream.Position);

					if (tokenLength > available)
					{
						error = "Ownership message ends before its token does.";
						return false;
					}

					message = new UnitOwnershipMessage
					{
						Kind = kind,
						UnitId = unitId,
						OwnerToken = tokenLength == 0
							? string.Empty
							: Encoding.UTF8.GetString(reader.ReadBytes(tokenLength))
					};

					return true;
				}
			}
			catch (IOException e)
			{
				error = "Ownership message could not be read: " + e.Message;
				return false;
			}
		}
	}
}