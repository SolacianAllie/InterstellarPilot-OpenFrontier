using System;
using System.IO;
using System.Text;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// What the host is playing, sent before the world itself.
	/// </summary>
	/// <remarks>
	/// Description, deliberately, and not configuration. It is tempting to
	/// send the host's sandbox settings along with this so the joiner's copy
	/// matches, and that would be a mistake twice over.
	///
	/// It is unnecessary: everything that decides what the world actually
	/// contains is already in the snapshot. The seeding settings are read
	/// only by WorldBase.SeedWorldOnNewGame, which is reached only when
	/// there is no save to load - a joining copy never gets there.
	///
	/// And it would be harmful: those settings live on the joiner's own
	/// GameController, so overwriting them would change how the player's
	/// own solo sandbox starts afterwards. A presentation setting like space
	/// fog is even clearer - a player on a weaker GPU should keep their own.
	///
	/// What this buys instead is a check worth having. The scenario id lets
	/// the joiner confirm it has that scenario installed BEFORE accepting the
	/// join, instead of failing later and less legibly once the snapshot has
	/// already been paid for.
	/// </remarks>
	public struct WorldDescriptorMessage
	{
		/// <summary>Bytes before the strings: five ints.</summary>
		public const int HeaderBytes = 20;

		/// <summary>
		/// Longest pilot name accepted, in characters.
		/// </summary>
		/// <remarks>
		/// A bound rather than a guess at what is reasonable. A name arrives
		/// from another machine, so this decides how much it can make us
		/// reserve before we have read a single character of it.
		/// </remarks>
		public const int MaxPilotNameChars = 64;

		/// <summary>The scenario the host is playing, or -1 if it does not know.</summary>
		public int ScenarioInfoId;

		/// <summary>
		/// The universe game type, as its enum value.
		/// </summary>
		/// <remarks>
		/// An int rather than the enum because this file stays free of engine
		/// types, the same rule the rest of the protocol follows. A value
		/// outside the enum is not rejected - it is simply not something this
		/// build has a name for, and a client that cannot label it should
		/// carry on rather than refuse to join.
		/// </remarks>
		public int GameTypeId;

		/// <summary>The host's pilot name. May be empty.</summary>
		public string PilotName;

		/// <summary>The faction made for the joiner, or -1.</summary>
		/// <remarks>
		/// Sent because the save models exactly one local player. The joiner
		/// loads the host's world and comes out as the host's pilot; these
		/// two say which of the world's factions is actually theirs, and are
		/// the only way it can be told.
		/// </remarks>
		public int MyFactionId;

		/// <summary>The shuttle made for the joiner, or -1.</summary>
		public int MyShipId;

		/// <summary>Whether a faction and ship were made for this client.</summary>
		public bool HasJoiner => MyFactionId >= 0;

		/// <summary>Whether the host could name the scenario it is playing.</summary>
		public bool HasScenario => ScenarioInfoId >= 0;

		public byte[] ToBytes()
		{
			string name = PilotName ?? string.Empty;

			if (name.Length > MaxPilotNameChars)
			{
				name = name.Substring(0, MaxPilotNameChars);
			}

			byte[] nameBytes = Encoding.UTF8.GetBytes(name);

			using (MemoryStream stream = new MemoryStream(HeaderBytes + 4 + nameBytes.Length))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(ScenarioInfoId);
				writer.Write(GameTypeId);
				writer.Write(MyFactionId);
				writer.Write(MyShipId);
				writer.Write(nameBytes.Length);
				writer.Write(nameBytes);
				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out WorldDescriptorMessage message, out string error)
		{
			message = default(WorldDescriptorMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "Descriptor is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int scenarioInfoId = reader.ReadInt32();
					int gameTypeId = reader.ReadInt32();
					int myFactionId = reader.ReadInt32();
					int myShipId = reader.ReadInt32();
					int nameLength = reader.ReadInt32();

					// Checked before anything is allocated for it: this is a
					// number from another machine deciding how much we
					// reserve.
					if (nameLength < 0 || nameLength > MaxPilotNameChars * 4)
					{
						error = "Pilot name length " + nameLength + " is out of range.";
						return false;
					}

					int available = (int)(stream.Length - stream.Position);

					if (nameLength > available)
					{
						error = "Descriptor ends before its pilot name does.";
						return false;
					}

					string pilotName = nameLength == 0 ? string.Empty : Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

					message = new WorldDescriptorMessage
					{
						ScenarioInfoId = scenarioInfoId,
						GameTypeId = gameTypeId,
						MyFactionId = myFactionId,
						MyShipId = myShipId,
						PilotName = pilotName
					};

					return true;
				}
			}
			catch (IOException e)
			{
				error = "Descriptor could not be read: " + e.Message;
				return false;
			}
		}
	}
}