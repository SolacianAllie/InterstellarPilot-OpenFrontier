using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using OpenFrontier.IP;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Host side of a session: owns the listener, validates every joiner, and
	/// keeps the set of connected players.
	///
	/// The host is the authority for the world. A joiner is admitted only after
	/// passing the version gate and proving it has the session secret, and the
	/// world's god mode and saving permissions are switched off here so a
	/// connected player cannot use them.
	///
	/// No player limit is imposed. Each additional player costs a faction, a
	/// GamePlayer and a bandwidth share; there is no artificial cap.
	/// </summary>
	public sealed class HostSession : IDisposable
	{
		private readonly byte[] sessionSecret;
		private readonly List<RemotePlayer> players = new List<RemotePlayer>();
		private NetListener listener;
		private bool accepting = true;
		private bool disposed;

		/// <summary>The code to share, set once listening starts.</summary>
		public string JoinCode { get; private set; }

		/// <summary>Raised when a player has been admitted and identified.</summary>
		public event Action<RemotePlayer> PlayerJoined;

		/// <summary>Raised when a player leaves or is dropped.</summary>
		public event Action<RemotePlayer, string> PlayerLeft;

		/// <summary>Everyone currently connected.</summary>
		public IReadOnlyList<RemotePlayer> Players => players;

		/// <summary>Whether the host is still accepting new players.</summary>
		public bool AcceptingPlayers => accepting && !disposed;

		private HostSession(byte[] sessionSecret, NetListener listener)
		{
			this.sessionSecret = sessionSecret;
			this.listener = listener;
		}

		/// <summary>
		/// Starts hosting on the given address and port. Port 0 asks the OS for
		/// any free port.
		/// </summary>
		/// <param name="address">Interface to listen on.</param>
		/// <param name="port">Port, or 0 for any free port.</param>
		/// <param name="onError">Receives a player-facing message if hosting fails.</param>
		/// <returns>A live session, or null if the listener could not start.</returns>
		public static HostSession Start(IPAddress address, int port, Action<string> onError)
		{
			if (!MultiplayerSettings.CanHost)
			{
				onError?.Invoke("Multiplayer is switched off, or set to join-only.");
				return null;
			}

			byte[] secret = PlayerIdentity.CreateSessionSecret();

			// The join code carries a fixed 7 bytes of secret. Compare against
			// exactly that prefix, not the whole generated value - comparing a
			// 7-byte code secret against a 20-byte generated one can never
			// match, and would refuse every joiner.
			var listener = new NetListener(port, address);

			if (!listener.Start())
			{
				// Start already reported the reason through the session's own
				// handler; it is wired below before Start is called so the message
				// reaches the UI rather than being lost.
				listener.Dispose();
				return null;
			}

			var session = new HostSession(secret, listener);

			string code = SessionCode.Create(address, listener.BoundPort, secret);
			session.JoinCode = SessionCode.Format(code);

			session.listener.PeerAccepted += session.OnPeerAccepted;
			session.listener.Failed += message => onError?.Invoke(message);

			return session;
		}

		private void OnPeerAccepted(Socket socket)
		{
			if (disposed)
			{
				socket.Close();
				return;
			}

			// Note: a host that has stopped accepting players still ACCEPTS the
			// socket. Refusing it here would make a probe report "offline" when
			// the host is right there with a full session - the opposite of the
			// truth, and the player would be told to go and check something that
			// is working. Refusal happens at the handshake instead, where the
			// peer is told why.
			var channel = new MessageChannel(socket);
			var pending = new RemotePlayer(channel, OnPlayerMessage, OnPlayerClosed);

			channel.MessageReceived += pending.OnMessageReceived;
			channel.Closed += reason => pending.CloseWithReason(reason);

			pending.ExpireAfter(HandshakeTimeout);
			channel.StartReading();
		}

		/// <summary>How long a peer has to send a valid Hello before being dropped.</summary>
		private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(20);

		/// <summary>
		/// Routes a message from a peer. Runs on the main thread.
		///
		/// The message id has already been split off by RemotePlayer; what
		/// arrives here is just the payload.
		/// </summary>
		private void OnPlayerMessage(RemotePlayer player, int messageId, byte[] body)
		{
			// A liveness probe, answered without admitting a player. This runs
			// before the handshake gate so a player browsing their host list does
			// not create a session or occupy a slot.
			if (messageId == ProtocolVersion.PingMessageId)
			{
				AnswerPing(player);
				return;
			}

			if (messageId == ProtocolVersion.HelloMessageId)
			{
				if (player.Admitted)
				{
					// Only one Hello per peer; a second is either a bug or a probe.
					player.CloseWithReason("Unexpected extra handshake.");
					return;
				}

				HelloMessage hello;

				if (!HelloMessage.TryFromBytes(body, out hello, out string error))
				{
					Reject(player, HandshakeResult.BadSecret);
					return;
				}

				player.CancelHandshakeTimeout();
				CompleteHandshake(player, hello);
				return;
			}

			if (!player.Admitted)
			{
				player.CloseWithReason("The other player didn't introduce itself properly.");
				return;
			}

			// Further messages (orders, flight updates, chat) are handled as the
			// phases that need them are added.
		}

		private void Reject(RemotePlayer player, HandshakeResult result)
		{
			Send(player, result, Versioning.Version, returning: false);
			player.CloseAfterSend();
		}

		/// <summary>
		/// Replies to a liveness probe and hangs up.
		///
		/// The reply carries this host's protocol and game version, which lets a
		/// player see *why* a host is unreachable (wrong build) rather than just
		/// that it is. The peer is never admitted, and the connection is closed
		/// immediately so a probe costs one short exchange and no session state.
		/// </summary>
		private void AnswerPing(RemotePlayer player)
		{
			PongMessage pong = new PongMessage
			{
				ProtocolVersion = ProtocolVersion.Current,
				GameVersion = Versioning.Version,

				// Whether this host will actually admit a player right now. A
				// probe succeeding while the host has stopped accepting players
				// would otherwise read as a healthy session.
				AcceptingPlayers = AcceptingPlayers,
				PlayerCount = players.Count
			};

			try
			{
				player.Channel.Send(FramedMessage.Create(ProtocolVersion.PongMessageId, pong.ToBytes()));
			}
			catch (Exception)
			{
				// The prober went away. Nothing to do; the close below still runs.
			}

			player.CloseAfterSend();
		}

		/// <summary>
		/// Decides whether a joining peer is allowed in. This is the version
		/// gate: only an exact match on both the wire format and the game
		/// version is admitted.
		/// </summary>
		private HandshakeResult Evaluate(HelloMessage hello)
		{
			if (hello.ProtocolVersion != ProtocolVersion.Current)
			{
				return HandshakeResult.ProtocolMismatch;
			}

			if (hello.GameVersion != Versioning.Version)
			{
				return HandshakeResult.VersionMismatch;
			}

			if (!IsSecretValid(hello.SessionSecret))
			{
				return HandshakeResult.BadSecret;
			}

			if (!accepting)
			{
				return HandshakeResult.SessionClosed;
			}

			return HandshakeResult.Accepted;
		}

		private bool IsSecretValid(byte[] candidate)
		{
			// Delegate the comparison so the code's embedded length and the
			// host's secret can never disagree.
			return SessionCode.SecretsMatch(sessionSecret, candidate);
		}

		/// <summary>
		/// Completes a handshake for a peer that has sent a valid Hello.
		/// Called by <see cref="RemotePlayer"/> once its Hello parses.
		/// </summary>
		internal void CompleteHandshake(RemotePlayer player, HelloMessage hello)
		{
			HandshakeResult result = Evaluate(hello);

			bool returning = false;

			if (result == HandshakeResult.Accepted)
			{
				// A player we have seen before keeps their faction and unit, so
				// rejoining resumes control of what they were last flying rather
				// than starting a new faction.
				player.Identity = hello.PlayerToken;
				player.Admitted = true;
				returning = knownIdentities.Contains(hello.PlayerToken);

				players.Add(player);
			}

			Send(player, result, Versioning.Version, returning);

			if (result != HandshakeResult.Accepted)
			{
				// Refused: the peer reads its answer, then the socket closes.
				player.CloseAfterSend();
				return;
			}

			PlayerJoined?.Invoke(player);
		}

		/// <summary>
		/// Tokens seen this session, so a reconnecting player is recognised.
		/// Not persisted yet - restoring a faction across a host restart is a
		/// later phase, and until then a returning player simply starts fresh.
		/// </summary>
		private readonly HashSet<string> knownIdentities = new HashSet<string>();

		/// <summary>Records a token as belonging to a live player.</summary>
		internal void RememberIdentity(string token)
		{
			if (!string.IsNullOrEmpty(token))
			{
				knownIdentities.Add(token);
			}
		}

		private void Send(RemotePlayer player, HandshakeResult result, Version hostVersion, bool returning)
		{
			HelloReplyMessage reply = new HelloReplyMessage
			{
				Result = result,
				HostProtocolVersion = ProtocolVersion.Current,
				HostGameVersion = hostVersion,
				ReturningPlayer = returning
			};

			try
			{
				player.Channel.Send(FramedMessage.Create(ProtocolVersion.HelloReplyMessageId, reply.ToBytes()));
			}
			catch (Exception)
			{
				// The channel's own Closed handler deals with a dead peer.
			}
		}

		private void OnPlayerClosed(RemotePlayer player, string reason)
		{
			bool wasJoined = players.Remove(player);

			// Disarm the handshake timer before disposing so a pending callback
			// cannot fire against a disposed timer.
			player.CancelHandshakeTimeout();
			player.Dispose();

			if (wasJoined)
			{
				RememberIdentity(player.Identity);
				PlayerLeft?.Invoke(player, reason);
			}
		}

		/// <summary>
		/// Stops accepting new players. Existing players stay connected; this is
		/// the in-game "stop hosting" control.
		/// </summary>
		public void StopAcceptingPlayers()
		{
			accepting = false;
		}

		/// <summary>
		/// Drops a connected player. This is the host's authority over who plays:
		/// a removed player cannot reconnect with their old identity because the
		/// token no longer maps to anything.
		/// </summary>
		public void Kick(RemotePlayer player)
		{
			if (player == null)
			{
				return;
			}

			players.Remove(player);
			player.CloseWithReason("The host removed you from the session.");
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;
			accepting = false;

			foreach (RemotePlayer player in new List<RemotePlayer>(players))
			{
				player.CloseWithReason("The host ended the session.");
			}

			players.Clear();

			listener?.Dispose();
			listener = null;
		}
	}
}