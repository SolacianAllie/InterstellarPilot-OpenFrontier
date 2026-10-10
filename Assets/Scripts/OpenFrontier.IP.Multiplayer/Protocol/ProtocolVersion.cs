using System;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Wire-format constants for the multiplayer protocol.
	///
	/// The protocol number is DERIVED from the game version rather than kept
	/// as a counter of its own, because the counter was not doing its job. It
	/// existed to be noticed when it was forgotten, and it was being forgotten:
	/// bumping it by hand had to be remembered every time a message changed,
	/// and twice in one session it very nearly was not. One number to bump -
	/// the version everyone already bumps for a release - removes the whole
	/// class of mistake.
	///
	/// What that costs, said plainly: the two used to be separate on purpose,
	/// so that a wire change without a version bump would be caught at
	/// handshake. It no longer is, and it could not be - two builds reporting
	/// the same version derive the same protocol. So the honest description
	/// of this number is a DIAGNOSTIC rather than a second gate: it says which
	/// build a peer claimed, in a form a player can read back to us. The gate
	/// is and always was the exact version match in the handshake.
	///
	/// That also means a collision in the encoding would be harmless, because
	/// two versions that collided here would still be refused by their version
	/// check. So the encoding is chosen for being stable and readable rather
	/// than for being injective, and the cap is not a sharp edge.
	/// </summary>
	public static class ProtocolVersion
	{
		/// <summary>
		/// The protocol number this build speaks.
		/// </summary>
		/// <remarks>
		/// Read through a property rather than held in a const because it
		/// depends on <see cref="Versioning.Version"/>, which asks Unity for
		/// the application version. A property defers that to first use, where
		/// a static initializer would run it during type initialization -
		/// which for a Unity native call poisons the whole type.
		/// </remarks>
		public static int Current => For(Versioning.Version);

		/// <summary>The protocol number for a given game version.</summary>
		/// <remarks>
		/// 0.1.2 becomes 12, so the number a player is shown reads like the
		/// version they recognise. Revision is deliberately left out: it moves
		/// for rebuilds nobody else should be able to tell apart anyway, and
		/// including it would make a session break on a change that changed
		/// nothing.
		///
		/// Capped because the value travels in a single byte of a join code,
		/// where 255 is reserved for "version unspecified". A version far
		/// enough away to hit the cap is one nobody will reach, and if they
		/// somehow do, two builds colliding here are still separated by the
		/// version check.
		/// </remarks>
		public static int For(System.Version version)
		{
			if (version == null)
			{
				return 0;
			}

			int major = Math.Max(0, version.Major);
			int minor = Math.Max(0, version.Minor);
			int build = Math.Max(0, version.Build);

			long value = ((long)major * 100) + ((long)minor * 10) + build;

			return value > Unspecified ? Unspecified - 1 : (int)value;
		}

		/// <summary>Reserved in the code's byte for a version we did not record.</summary>
		public const int Unspecified = 255;

		/// <summary>A sentence for an error box, in terms a player recognises.</summary>
		public static string Describe()
		{
			return Versioning.Version + " (protocol " + Current + ")";
		}

		/// <summary>
		/// First message a client sends. Kept tiny and fixed-layout so the host
		/// can validate version and app version before allocating anything or
		/// reading further (a mismatched peer must never be able to make us
		/// allocate large buffers or deserialize hostile layouts).
		/// </summary>
		public const int HelloMessageId = 1;

		/// <summary>
		/// Host's reply to a Hello. Carries accept/reject plus a reason code the
		/// UI turns into a human-readable message.
		/// </summary>
		public const int HelloReplyMessageId = 2;

		/// <summary>
		/// A liveness probe. Sent before any handshake, and answered without one.
		/// </summary>
		///
		/// This is deliberately separate from the handshake: a player browsing
		/// their saved hosts should not create a session, consume a player slot
		/// or leave a pending-peer entry every time the list refreshes. A reply
		/// proves something specific and useful - the host process is running,
		/// is in a game, and has multiplayer listening on this port right now.
		public const int PingMessageId = 3;

		/// <summary>The host's answer to a <see cref="PingMessageId"/>.</summary>
		public const int PongMessageId = 4;

		/// <summary>Where a batch of units are. The first real game message.</summary>
		public const int UnitStateMessageId = 5;

		/// <summary>One piece of the host's world, on its way to a client.</summary>
		public const int WorldSnapshotChunkMessageId = 6;

		/// <summary>What the host is playing, sent before the world.</summary>
		public const int WorldDescriptorMessageId = 7;

		/// <summary>Claiming a ship, and the host saying who has it.</summary>
		public const int UnitOwnershipMessageId = 8;

		/// <summary>How a joining player wants their pilot and faction named.</summary>
		public const int JoinerPreferencesMessageId = 9;

		/// <summary>The host leaving on purpose.</summary>
		public const int HostFarewellMessageId = 10;

		/// <summary>What a player's faction owns, pushed host to client.</summary>
		/// <remarks>
		/// Added after the others rather than numbered alongside them, so no
		/// id already in flight is renumbered. Builds compare a version on
		/// connect and these are compared per message, and renumbering one
		/// would let two builds that both claim the same version silently
		/// disagree about what message 6 is.
		/// </remarks>
		public const int FactionStateMessageId = 11;

		/// <summary>A joiner saying it has arrived and can be sent to.</summary>
		public const int JoinerReadyMessageId = 12;

		/// <summary>A player asking the host to give a fleet an order.</summary>
		public const int FleetOrderCommandMessageId = 13;
	}
}