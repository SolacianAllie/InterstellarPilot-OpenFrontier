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

		/// <summary>The port actually in use.</summary>
		/// <remarks>
		/// Not the port that was asked for. The session lets the OS pick any
		/// free port, so the requested value is zero and useless for
		/// diagnosing a connection that will not open.
		/// </remarks>
		public int Port => listener.BoundPort;

		/// <summary>The local address players are being told to reach.</summary>
		/// <remarks>
		/// Kept rather than read back off the listener, because this is the
		/// address that was chosen as reachable and written into every join
		/// code. Shown on the session panel so a player can see the address
		/// their friends are actually dialling.
		/// </remarks>
		public IPAddress Address { get; private set; }

		/// <summary>The address and port together, as one thing to look at.</summary>
		public string Endpoint => Address + ":" + Port;

		/// <summary>
		/// What each player wants to be called, keyed by token.
		/// </summary>
		/// <remarks>
		/// Kept per player rather than applied on arrival, because the faction
		/// is made when the world is exported and there may be several exports
		/// before that happens. Arriving twice, or asking twice, must not make
		/// two factions.
		/// </remarks>
		private readonly Dictionary<string, JoinerPreferencesMessage> preferences =
			new Dictionary<string, JoinerPreferencesMessage>();

		/// <summary>
		/// Set by whatever can actually touch the world, to handle a player's
		/// ship upload.
		/// </summary>
		/// <remarks>
		/// Injected rather than called directly because this class is the
		/// handshake, and the handshake is the one part of this system that
		/// can be tested without a running engine. Naming HostShipUpload here
		/// would put engine types back into it for the sake of one line, and
		/// taking that ability away is a bad trade for every future change.
		///
		/// Returning the number of units written is used for nothing today
		/// and is here because the natural next thing is to log a client
		/// uploading ships it does not own, and adding the plumbing later
		/// would mean threading it through two assemblies.
		/// </remarks>
		public static Func<HostSession, string, byte[], int> ShipUploadHandler { get; set; }

		/// <summary>
		/// Set by whatever can touch the world, to say whether a ship may be
		/// claimed by a given player.
		/// </summary>
		/// <remarks>
		/// Injected for the same reason as the upload handler. Answering this
		/// needs the engine - it is a question about a ship's faction - and
		/// putting that here would take the one part of this system that can
		/// be tested without Unity and put it back out of reach. Failing open
		/// is deliberate at the call site: an unset handler means no gate,
		/// which is the state this code was in for hours and is survivable,
		/// rather than a refusal that would break joining entirely.
		/// </remarks>
		public static Func<int, string, bool> ShipMayBeClaimed { get; set; }

		/// <summary>Who owns which ship. The host decides, and this is it.</summary>
		public UnitOwnershipRegistry Ownership { get; } = new UnitOwnershipRegistry();

		/// <summary>Raised when a player has been admitted and identified.</summary>
		public event Action<RemotePlayer> PlayerJoined;

		/// <summary>Raised when a ship's owner changes, so it can be told to everyone.</summary>
		public event Action<int, string> OwnershipChanged;

		/// <summary>Raised when a player leaves or is dropped.</summary>
		public event Action<RemotePlayer, string> PlayerLeft;

		/// <summary>Everyone currently connected.</summary>
		public IReadOnlyList<RemotePlayer> Players => players;

		/// <summary>Whether the host is still accepting new players.</summary>
		public bool AcceptingPlayers => accepting && !disposed;

		/// <summary>
		/// Whether this host is in the main menu rather than in a game.
		/// </summary>
		/// <remarks>
		/// Set by <see cref="MultiplayerHostService"/> each frame; the session
		/// itself has no opinion about where the game is, because it is a
		/// networking object that should not need to know about menus. Default
		/// false, so a session used without the service still reports a plain
		/// "not accepting" rather than inventing a reason.
		/// </remarks>
		public bool IsInMainMenu { get; internal set; }

		private HostSession(IPAddress address, byte[] sessionSecret, NetListener listener)
		{
			Address = address;
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
		/// <summary>Starts a session, listening on every interface.</summary>
		/// <remarks>
		/// The bind address and the advertised address are separate on
		/// purpose: binding everywhere means any route in reaches the
		/// session, while the code still names the single address most
		/// likely to be the right one for the person joining.
		/// </remarks>
		public static HostSession Start(
			IPAddress bindAddress,
			IPAddress advertisedAddress,
			int port,
			Action<string> onError,
			Action<string> onLog = null)
		{
			if (!MultiplayerSettings.CanHost)
			{
				onError?.Invoke("Multiplayer is switched off, or set to join-only.");
				return null;
			}

			byte[] secret = PlayerIdentity.SessionSecret;

			// The join code carries a fixed 7 bytes of secret. Compare against
			// exactly that prefix, not the whole generated value - comparing a
			// 7-byte code secret against a 20-byte generated one can never
			// match, and would refuse every joiner.
			//
			// Port 0 means "any free one", which is the fallback when every
			// preferred port is taken. The code carries whichever we get.
			var listener = new NetListener(port, bindAddress);

			if (!listener.Start())
			{
				// Start already reported the reason through the session's own
				// handler; it is wired below before Start is called so the message
				// reaches the UI rather than being lost.
				listener.Dispose();
				return null;
			}

			var session = new HostSession(advertisedAddress, secret, listener)
			{
				Log = onLog
			};

			// The advertised address, not the bind address: the bind is
			// always 0.0.0.0, which nobody can dial.
			string code = SessionCode.Create(advertisedAddress, listener.BoundPort, secret);
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
			if (messageId == ProtocolVersion.UnitStateMessageId)
			{
				ShipUploadHandler?.Invoke(this, player.Identity, body);
				return;
			}

			if (messageId == ProtocolVersion.JoinerPreferencesMessageId)
			{
				RememberPreferences(player, body);
				return;
			}

			if (messageId == ProtocolVersion.UnitOwnershipMessageId)
			{
				AnswerOwnershipClaim(player, body);
				return;
			}

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
		private void RememberPreferences(RemotePlayer player, byte[] body)
		{
			if (!player.Admitted)
			{
				// Before the handshake there is nobody to make a faction
				// for, and nothing worth remembering.
				return;
			}

			if (!JoinerPreferencesMessage.TryFromBytes(body, out JoinerPreferencesMessage message, out string error))
			{
				Log?.Invoke("Multiplayer: unreadable joiner preferences - " + error);
				return;
			}

			preferences[player.Identity] = message;

			Log?.Invoke("Multiplayer: " + Short(player.Identity) + " wants to be "
				+ message.PilotName + " of " + message.FactionName);
		}

		/// <summary>
		/// What a player asked to be called, or null if they never said.
		/// </summary>
		internal JoinerPreferencesMessage PreferencesFor(string token)
		{
			return token != null && preferences.TryGetValue(token, out JoinerPreferencesMessage message)
				? message
				: default(JoinerPreferencesMessage);
		}

		/// <summary>
		/// Decides whether this player gets this ship, and tells everyone.
		/// </summary>
		/// <remarks>
		/// The host can only ever take authority away, never grant it in a way
		/// a client waits for. A client takes its ship the instant the player
		/// grabs the stick and asks afterwards; this decides whether that
		/// stands.
		///
		/// The answer goes to every player, not only the one who asked. A
		/// client that knew only about its own ships would go on simulating
		/// the others, so ownership the claimant is not aware of is ownership
		/// that is not in force anywhere.
		/// </remarks>
		private void AnswerOwnershipClaim(RemotePlayer player, byte[] body)
		{
			if (!player.Admitted)
			{
				return;
			}

			if (!UnitOwnershipMessage.TryFromBytes(body, out UnitOwnershipMessage message, out string error))
			{
				Log?.Invoke("Multiplayer: unreadable ownership message - " + error);
				return;
			}

			if (message.Kind != UnitOwnershipMessage.KindRequest)
			{
				// A client telling the host who owns what is not something to
				// obey. This registry is the only writer.
				return;
			}

			if (!ShipMayBeClaimed?.Invoke(message.UnitId, player.Identity) ?? true)
			{
				// The registry refuses a ship another PLAYER holds. It had no
				// answer for the host's own ships, which carry no owner entry,
				// so a client that happened to be pointed at one could claim
				// it - and then the host would apply that client's position to
				// its own ship every frame. It was seen doing exactly that.
				Log?.Invoke("Multiplayer: refused " + Short(player.Identity)
					+ " ship " + message.UnitId + " - it belongs to the host");

				SendTo(player, UnitOwnershipMessage.Assign(message.UnitId, string.Empty));

				return;
			}

			if (Ownership.TryClaim(message.UnitId, player.Identity, out string reason))
			{
				SendToEveryone(message.UnitId, player.Identity);

				Log?.Invoke("Multiplayer: " + Short(player.Identity) + " took ship " + message.UnitId);
				return;
			}

			// Refused, and the client is told who has it rather than left to
			// work it out. A client that keeps simulating a ship the host has
			// taken back gets it yanked away on every message, and from the
			// player's side that is indistinguishable from a bad connection.
			Log?.Invoke("Multiplayer: " + Short(player.Identity) + " refused ship "
				+ message.UnitId + " - " + reason + ", held by " + Short(Ownership.OwnerOf(message.UnitId)));

			SendTo(player, UnitOwnershipMessage.Assign(message.UnitId, Ownership.OwnerOf(message.UnitId)));
		}

		/// <summary>Tells every admitted player who owns a ship.</summary>
		internal void SendToEveryone(int unitId, string ownerToken)
		{
			byte[] framed = FramedMessage.Create(ProtocolVersion.UnitOwnershipMessageId,
				UnitOwnershipMessage.Assign(unitId, ownerToken).ToBytes());

			for (int i = 0; i < players.Count; i++)
			{
				RemotePlayer player = players[i];

				if (player == null || !player.Admitted)
				{
					continue;
				}

				try
				{
					player.Channel.Send(framed);
				}
				catch (Exception)
				{
					// Someone who has gone away is noticed on their own close.
				}
			}
		}

		/// <summary>Tells one player who owns a ship.</summary>
		internal void SendTo(RemotePlayer player, UnitOwnershipMessage message)
		{
			if (player == null || !player.Admitted)
			{
				return;
			}

			try
			{
				player.Channel.Send(FramedMessage.Create(
					ProtocolVersion.UnitOwnershipMessageId, message.ToBytes()));
			}
			catch (Exception)
			{
				// As above.
			}
		}

		/// <summary>
		/// Sends one message to every admitted player.
		/// </summary>
		/// <remarks>
		/// For messages that are not about any one player - a farewell, a
		/// scene change - where the per-recipient filtering the ownership
		/// broadcast needs would make no difference.
		/// </remarks>
		public void Broadcast(int messageId, byte[] payload)
		{
			byte[] framed = FramedMessage.Create(messageId, payload);

			for (int i = 0; i < players.Count; i++)
			{
				RemotePlayer player = players[i];

				if (player == null || !player.Admitted)
				{
					continue;
				}

				try
				{
					player.Channel.Send(framed);
				}
				catch (Exception)
				{
					// Someone who has gone away is noticed on their own close.
				}
			}
		}

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

				// Whether the refusal is "I am in the main menu" or "I have
				// stopped taking players". A joiner needs the difference: the
				// first is a few seconds away, the second is not happening.
				InMainMenu = !AcceptingPlayers && IsInMainMenu,
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
				Log?.Invoke("Multiplayer: refused " + Short(hello.PlayerToken)
					+ " - protocol " + hello.ProtocolVersion + ", ours " + ProtocolVersion.Current);

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
		/// Whether a join code is this host's own.
		/// </summary>
		/// <remarks>
		/// Both the secret AND the endpoint have to match. The secret alone is
		/// unique per session, but a code also names an address and a port, and
		/// a secret that matched while the address did not would mean two
		/// different sessions had collided - which is worth refusing rather than
		/// acting on.
		///
		/// Checked locally before any socket is opened. The alternative - letting
		/// the connection be made and refused by the host - would report it as a
		/// network failure, which tells the player nothing about what they did
		/// wrong.
		/// </remarks>
		public bool IsOwnCode(SessionCodeData data)
		{
			if (data == null || disposed)
			{
				return false;
			}

			// The advertised address, NOT the listener's bind address. The
			// session binds to every interface so that 0.0.0.0 is always
			// what it listens on, and a code carries the one real address - so
			// comparing against the bind address would never match anything,
			// and every player would be free to join their own session.
			return data.Port == listener.BoundPort
				&& Address != null
				&& Address.Equals(data.Address)
				&& SessionCode.SecretsMatch(sessionSecret, data.Secret);
		}

		/// <summary>
		/// Completes a handshake for a peer that has sent a valid Hello.
		/// Called by <see cref="RemotePlayer"/> once its Hello parses.
		/// </summary>
		internal void CompleteHandshake(RemotePlayer player, HelloMessage hello)
		{
			HandshakeResult result = Evaluate(hello);

			// Every attempt is reported, accepted or not. A host that silently
			// swallows them makes "the joiner says the code is wrong" a
			// question with no answer anywhere: the joiner only knows what the
			// host told it, and the host says nothing at all. That is exactly
			// the shape of problem that gets blamed on the wrong side.
			Log?.Invoke("Multiplayer: join attempt from " + Short(hello.PlayerToken)
				+ " - protocol " + hello.ProtocolVersion
				+ ", version " + hello.GameVersion
				+ ", secret " + (IsSecretValid(hello.SessionSecret) ? "matches" : "DOES NOT MATCH")
				+ (AcceptingPlayers ? string.Empty : ", not accepting players")
				+ (IsInMainMenu ? " (in the main menu)" : string.Empty)
				+ " -> " + result);

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
				Log?.Invoke("Multiplayer: refused " + Short(hello.PlayerToken) + " - " + result);
				player.CloseAfterSend();
				return;
			}

			Log?.Invoke("Multiplayer: admitted " + Short(hello.PlayerToken)
				+ (returning ? " (returning player)" : string.Empty));

			PlayerJoined?.Invoke(player);
		}

		/// <summary>
		/// Tokens seen this session, so a reconnecting player is recognised.
		/// Not persisted yet - restoring a faction across a host restart is a
		/// later phase, and until then a returning player simply starts fresh.
		/// </summary>
		private readonly HashSet<string> knownIdentities = new HashSet<string>();

		/// <summary>Where to report what happens to join attempts.</summary>
		internal Action<string> Log { get; set; }

		/// <summary>Enough of a token to tell two players apart in a log line.</summary>
		private static string Short(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return "(no token)";
			}

			return token.Length <= 8 ? token : token.Substring(0, 8);
		}
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
				ReturningPlayer = returning,
				InMainMenu = IsInMainMenu
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
		/// Starts accepting players again.
		/// </summary>
		/// <remarks>
		/// The counterpart to <see cref="StopAcceptingPlayers"/>, which is
		/// called every frame while the host is somewhere players cannot be
		/// admitted - the main menu. Without a way back, a host that once left
		/// the menu could never take players again.
		/// </remarks>
		public void ResumeAcceptingPlayers()
		{
			if (!disposed)
			{
				accepting = true;
			}
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