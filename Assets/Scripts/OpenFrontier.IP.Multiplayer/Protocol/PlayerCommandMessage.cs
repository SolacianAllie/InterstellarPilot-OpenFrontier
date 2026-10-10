using System;
using System.IO;
using System.Text;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// A request from a client for the host to change something.
	/// </summary>
	/// <remarks>
	/// One envelope for every player action, rather than a message type each.
	/// There are around forty of them still to relay, and written
	/// individually that is forty message types, forty ids, forty codecs,
	/// forty host dispatch branches and forty rounds of codec tests - all of
	/// which have to be kept in step, and every one of which is an
	/// opportunity for two builds to disagree about the numbering.
	///
	/// Adding an action does not become a protocol change. A command the host
	/// has never heard of is refused loudly rather than ignored, which is the
	/// same honest-refusal rule as Authority.Refused.
	///
	/// Type safety is not lost, it is just moved to where it can be checked:
	/// each command has a named handler that reads IntArgs and TextArgs by
	/// position, so a handler that expects two ints and gets one fails in the
	/// handler rather than corrupting the parse.
	///
	/// Ship ids and sector ids travel here. Fleet ids never do - a client may
	/// build a fleet in its own world and the host has never issued that id, so
	/// an action that means something to a fleet names the ships in it.
	/// </remarks>
	public struct PlayerCommandMessage
	{
		public const int MaxIntArgs = 16;
		public const int MaxTextArgs = 8;
		public const int MaxTextBytes = 128;

		/// <summary>1 command, 2 int count, 2 text count.</summary>
		public const int HeaderBytes = 1 + 4 + 4;

		public byte CommandId;
		public int[] IntArgs;
		public string[] TextArgs;

		public int IntCount => IntArgs == null ? 0 : IntArgs.Length;

		public int TextCount => TextArgs == null ? 0 : TextArgs.Length;

		/// <summary>An int argument, or a fallback if it was not sent.</summary>
		public int IntAt(int index, int fallback = 0)
		{
			return IntArgs != null && index >= 0 && index < IntArgs.Length
				? IntArgs[index]
				: fallback;
		}

		/// <summary>A text argument, or null if it was not sent.</summary>
		public string TextAt(int index)
		{
			return TextArgs != null && index >= 0 && index < TextArgs.Length
				? TextArgs[index]
				: null;
		}

		public static PlayerCommandMessage Of(byte commandId, int[] intArgs, string[] textArgs)
		{
			return new PlayerCommandMessage
			{
				CommandId = commandId,
				IntArgs = intArgs ?? new int[0],
				TextArgs = textArgs ?? new string[0]
			};
		}

		public byte[] ToBytes()
		{
			int[] ints = IntArgs ?? new int[0];
			string[] texts = TextArgs ?? new string[0];

			if (ints.Length > MaxIntArgs)
			{
				ints = new int[MaxIntArgs];
				Array.Copy(IntArgs, ints, MaxIntArgs);
			}

			if (texts.Length > MaxTextArgs)
			{
				texts = new string[MaxTextArgs];
				Array.Copy(TextArgs, texts, MaxTextArgs);
			}

			byte[][] encoded = new byte[texts.Length][];
			int size = HeaderBytes + (ints.Length * 4);

			for (int i = 0; i < texts.Length; i++)
			{
				encoded[i] = Encoding.UTF8.GetBytes(texts[i] ?? string.Empty);

				if (encoded[i].Length > MaxTextBytes)
				{
					encoded[i] = new byte[MaxTextBytes];
				}

				size += 4 + encoded[i].Length;
			}

			using (MemoryStream stream = new MemoryStream(size))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(CommandId);
				writer.Write(ints.Length);
				writer.Write(texts.Length);

				for (int i = 0; i < ints.Length; i++)
				{
					writer.Write(ints[i]);
				}

				for (int i = 0; i < encoded.Length; i++)
				{
					writer.Write(encoded[i].Length);
					writer.Write(encoded[i]);
				}

				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out PlayerCommandMessage message, out string error)
		{
			message = default(PlayerCommandMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "A player command is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					byte commandId = reader.ReadByte();
					int intCount = reader.ReadInt32();
					int textCount = reader.ReadInt32();

					// Checked against what actually arrived before anything is
					// reserved for it. A length in a message is the one number
					// a sender fully controls, and this is the code that reads
					// every one of them.
					if (intCount < 0 || intCount > MaxIntArgs)
					{
						error = "A player command claims " + intCount
							+ " int arguments, which is outside 0-" + MaxIntArgs + ".";
						return false;
					}

					if (textCount < 0 || textCount > MaxTextArgs)
					{
						error = "A player command claims " + textCount
							+ " text arguments, which is outside 0-" + MaxTextArgs + ".";
						return false;
					}

					int intBytes = intCount * 4;

					if (stream.Length - stream.Position < intBytes)
					{
						error = "A player command promised " + intCount
							+ " int arguments and did not carry them.";
						return false;
					}

					int[] ints = new int[intCount];

					for (int i = 0; i < intCount; i++)
					{
						ints[i] = reader.ReadInt32();
					}

					string[] texts = new string[textCount];

					for (int i = 0; i < textCount; i++)
					{
						if (stream.Length - stream.Position < 4)
						{
							error = "A player command promised " + textCount
								+ " text arguments and ran out after " + i + ".";
							return false;
						}

						int length = reader.ReadInt32();

						if (length < 0 || length > MaxTextBytes)
						{
							error = "A player command text argument claims " + length
								+ " bytes, which is outside 0-" + MaxTextBytes + ".";
							return false;
						}

						if (stream.Length - stream.Position < length)
						{
							error = "A player command text argument claims " + length
								+ " bytes and does not carry them.";
							return false;
						}

						texts[i] = length == 0
							? string.Empty
							: Encoding.UTF8.GetString(reader.ReadBytes(length));
					}

					message = new PlayerCommandMessage
					{
						CommandId = commandId,
						IntArgs = ints,
						TextArgs = texts
					};

					return true;
				}
			}
			catch (System.Exception e)
			{
				error = "A player command could not be read: " + e.Message;
				return false;
			}
		}
	}
}