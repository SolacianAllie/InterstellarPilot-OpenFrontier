namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Wire-format constants for the multiplayer protocol.
	///
	/// Deliberately SEPARATE from the game version (OpenFrontier.IP.Versioning).
	/// The game version gates who may join - players must match exactly. This
	/// value gates whether two builds can understand each other's bytes. They
	/// are almost always bumped together, but for different reasons:
	///
	///   - Versioning.Version changes when we cut a release.
	///   - ProtocolVersion changes whenever any message layout changes.
	///
	/// Keeping them separate means an accidental wire change during
	/// development is rejected at handshake instead of corrupting a session,
	/// even when both builds report the same game version.
	/// </summary>
	public static class ProtocolVersion
	{
		/// <summary>
		/// Bump on ANY change to message layout, framing, or serialization.
		/// Both peers must agree exactly; there is no compatibility range.
		/// </summary>
		/// <remarks>
		/// 2: PongMessage gained InMainMenu, so a host in the menu can say so
		/// rather than looking the same as one that has stopped taking players.
		/// </remarks>
		public const int Current = 2;

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
	}
}