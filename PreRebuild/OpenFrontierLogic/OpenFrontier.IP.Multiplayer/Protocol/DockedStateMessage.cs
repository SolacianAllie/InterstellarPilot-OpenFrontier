using System.IO;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// The host saying which hangar a ship is sitting in, if any.
	/// </summary>
	/// <remarks>
	/// Docked-ness is a fact about the world that the sector map filters on
	/// and the joiner factory reads when it takes a snapshot, and until now it
	/// was on neither the wire nor anything else. A client that docked put its
	/// ship inside a hangar the host had never heard of, and the two copies'
	/// UnitHangar.Items stayed different for the rest of the session with
	/// nothing to reconcile them.
	///
	/// Sent when it changes rather than carried in every UnitState. Only one
	/// ship changes at a time and the snapshot already covers anybody who
	/// joins later, so paying eight bytes a ship every two seconds to say
	/// "still not docked" to most of a galaxy would be paying for nothing.
	///
	/// The bay is named as well as the dock, because the two copies pick the
	/// bay themselves when a dock happens and there is no reason to believe
	/// they would pick the same one. The host decides and says which, and
	/// every copy puts the ship in that bay or the copy is wrong in a way that
	/// will never correct itself.
	/// </remarks>
	public struct DockedStateMessage
	{
		public const int HeaderBytes = 12;

		/// <summary>Means "not docked". No real hangar has id zero.</summary>
		public const int NotDocked = 0;

		public int ShipId;

		/// <summary>The station or carrier, or <see cref="NotDocked"/>.</summary>
		public int DockUnitId;

		/// <summary>Which bay within it.</summary>
		public int BayId;

		public bool IsDocked => DockUnitId != NotDocked;

		public static DockedStateMessage Undocked(int shipId)
		{
			return new DockedStateMessage
			{
				ShipId = shipId,
				DockUnitId = NotDocked,
				BayId = -1
			};
		}

		public static DockedStateMessage Docked(int shipId, int dockUnitId, int bayId)
		{
			return new DockedStateMessage
			{
				ShipId = shipId,
				DockUnitId = dockUnitId,
				BayId = bayId
			};
		}

		public byte[] ToBytes()
		{
			using (MemoryStream stream = new MemoryStream(HeaderBytes))
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(ShipId);
				writer.Write(DockUnitId);
				writer.Write(BayId);
				writer.Flush();

				return stream.ToArray();
			}
		}

		public static bool TryFromBytes(byte[] payload, out DockedStateMessage message, out string error)
		{
			message = default(DockedStateMessage);
			error = null;

			if (payload == null || payload.Length < HeaderBytes)
			{
				error = "A docked-state message is shorter than its header.";
				return false;
			}

			try
			{
				using (MemoryStream stream = new MemoryStream(payload, writable: false))
				using (BinaryReader reader = new BinaryReader(stream))
				{
					int shipId = reader.ReadInt32();
					int dockUnitId = reader.ReadInt32();
					int bayId = reader.ReadInt32();

					if (shipId <= 0)
					{
						error = "A docked-state message names no ship (" + shipId + ").";
						return false;
					}

					if (dockUnitId < 0)
					{
						error = "A docked-state message puts ship " + shipId
							+ " in dock " + dockUnitId + ", which is not a unit id."
							+ " Zero means undocked; anything else must be positive.";
						return false;
					}

					if (dockUnitId != NotDocked && bayId < 0)
					{
						error = "A docked-state message puts ship " + shipId + " in dock "
							+ dockUnitId + " at bay " + bayId + ", which is not a bay id.";
						return false;
					}

					message = new DockedStateMessage
					{
						ShipId = shipId,
						DockUnitId = dockUnitId,
						BayId = bayId
					};

					return true;
				}
			}
			catch (System.Exception e)
			{
				error = "A docked-state message could not be read: " + e.Message;
				return false;
			}
		}
	}
}