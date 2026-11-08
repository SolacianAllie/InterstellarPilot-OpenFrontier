using System;
using System.IO;
using System.Text;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The host saying goodbye on purpose.
	/// </summary>
	/// <remarks>
	/// Sent the moment the host leaves its world, before the socket closes.
	///
	/// Without it, quitting and losing signal are the same event. A client
	/// whose host quit and a client whose network dropped both see a closed
	/// socket, and both get "connection lost" - which is wrong often enough
	/// to be useless. One of those means "go and find them", the other
	/// means "try again", and a player cannot tell which they have been told
	/// from the message alone.
	///
	/// Best effort, and that is the point. A farewell that never arrives
	/// because the host's machine died is exactly the case it cannot cover,
	/// and the client falls back to saying the connection was lost. Better a
	/// message that is sometimes absent than one that is sometimes wrong.
	/// </remarks>
	public struct HostFarewellMessage
	{
		/// <summary>Why the host is going.</summary>
		public string Reason;

		public byte[] ToBytes()
		{
			string reason = Reason ?? string.Empty;
			byte[] bytes = Encoding.UTF8.GetBytes(reason);

			if (bytes.Length > MaxReasonBytes)
			{
				byte[] cut = new byte[MaxReasonBytes];
				Array.Copy(bytes, cut, MaxReasonBytes);
				bytes = cut;
			}

			using (MemoryStream stream = new MemoryStream(4 + bytes.Length))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(bytes.Length);
				writer.Write(bytes);
				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out HostFarewellMessage message, out string error)
		{
			message = default(HostFarewellMessage);
			error = null;

			if (payload == null || payload.Length < 4)
			{
				error = "Farewell was truncated.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int length = reader.ReadInt32();

					// Before anything is reserved for it: this decides how much
					// memory a message from another machine buys.
					if (length < 0 || length > MaxReasonBytes)
					{
						error = "Farewell reason length " + length + " is out of range.";
						return false;
					}

					if (length > (stream.Length - stream.Position))
					{
						error = "Farewell ends before its reason does.";
						return false;
					}

					message = new HostFarewellMessage
					{
						Reason = length == 0
							? string.Empty
							: Encoding.UTF8.GetString(reader.ReadBytes(length))
					};

					return true;
				}
			}
			catch (IOException e)
			{
				error = "Farewell could not be read: " + e.Message;
				return false;
			}
		}

		/// <summary>Longest reason accepted, in bytes.</summary>
		public const int MaxReasonBytes = 128;
	}
}