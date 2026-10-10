using System;
using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// A joiner's "I have the world and I am in it", sent once.
	///
	/// Until this arrives the host treats the player as still arriving, and
	/// sends them nothing but the world itself. That is not politeness: a
	/// client with no world loaded cannot apply a unit state, so every one
	/// sent before this lands is thrown away. A join measured at over twelve
	/// thousand discarded updates while its snapshot was still being written
	/// to disk, which is a great deal of everyone's bandwidth spent on
	/// nothing at all - and it also fills the interpolator with samples for
	/// units that do not exist yet.
	///
	/// It carries the ids back as well, so the host can check that the player
	/// claiming to be ready is the player it made a faction for. A marker on
	/// its own would be accepted from anyone who had completed a handshake.
	/// </summary>
	public struct JoinerReadyMessage
	{
		/// <summary>The faction the host made for this joiner.</summary>
		public int FactionId;

		/// <summary>The shuttle it made for them.</summary>
		public int ShipId;

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream(8))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(FactionId);
				writer.Write(ShipId);
				writer.Flush();
				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out JoinerReadyMessage message, out string error)
		{
			message = default(JoinerReadyMessage);
			error = null;

			if (payload == null || payload.Length < 8)
			{
				error = "Payload was " + (payload == null ? "missing" : payload.Length + " bytes")
					+ ", short of the 8 a readiness takes.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					message = new JoinerReadyMessage
					{
						FactionId = reader.ReadInt32(),
						ShipId = reader.ReadInt32()
					};

					return true;
				}
			}
			catch (Exception e)
			{
				error = "Payload could not be read: " + e.Message;
				return false;
			}
		}
	}
}