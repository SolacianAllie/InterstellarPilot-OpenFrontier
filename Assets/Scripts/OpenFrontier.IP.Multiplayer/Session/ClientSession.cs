using System;
using UnityEngine;
using OpenFrontier.IP;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>What a join attempt is doing right now.</summary>
	public enum JoinState
	{
		Idle,

		/// <summary>Checking the code the player typed.</summary>
		CheckingCode,

		/// <summary>Opening a connection to the host.</summary>
		Connecting,

		/// <summary>Connected; exchanging handshakes.</summary>
		Handshaking,

		/// <summary>In the session.</summary>
		Connected,

		/// <summary>Stopped, with a reason.</summary>
		Failed
	}

	/// <summary>
	/// Client side of a session: parses a join code, connects, and completes the
	/// handshake.
	///
	/// The code is validated locally BEFORE any socket is opened, so a wrong
	/// version is reported immediately and no connection is attempted. This
	/// matters for the version rule: the common case - someone on a different
	/// version - fails without touching the network.
	/// </summary>
	public sealed class ClientSession : IDisposable
	{
		private MessageChannel channel;
		private bool disposed;

		/// <summary>Where the attempt has got to.</summary>
		public JoinState State { get; private set; } = JoinState.Idle;

		/// <summary>Player-facing message when the attempt failed.</summary>
		public string FailureReason { get; private set; }

		/// <summary>
		/// The host's reply, once one arrives. Includes the host's version so
		/// a mismatch can be explained precisely.
		/// </summary>
		public HelloReplyMessage HostReply { get; private set; }

		/// <summary>Raised when the session reaches <see cref="JoinState.Connected"/>.</summary>
		public event Action Connected;

		/// <summary>Raised when the attempt fails, with a message fit to show.</summary>
		public event Action<string> Failed;

		/// <summary>Raised when an established session ends unexpectedly.</summary>
		public event Action<string> Disconnected;

		/// <summary>
		/// A batch of unit states arrived from the host.
		/// </summary>
		/// <remarks>
		/// Separate from <see cref="Connected"/> because this keeps arriving
		/// for the whole time a session is joined, long after the menu screen
		/// that started it has been destroyed. Anything that only lives on that
		/// screen stops hearing about the world the moment a player enters a
		/// game, which is exactly when it matters.
		/// </remarks>
		public event Action<UnitStateMessage> UnitStateReceived;

		/// <summary>The host has said which world it is playing.</summary>
		public event Action<WorldDescriptorMessage> WorldDescriptorReceived;

		/// <summary>One piece of the host's world. May arrive many times.</summary>
		public event Action<byte[]> WorldSnapshotChunkReceived;

		/// <summary>
		/// Attempts to join using a code the player typed.
		/// </summary>
		public void Join(string code)
		{
			if (State != JoinState.Idle && State != JoinState.Failed)
			{
				return;
			}

			if (!MultiplayerSettings.CanJoin)
			{
				Abort("Multiplayer is switched off, or set to host-only.");
				return;
			}

			State = JoinState.CheckingCode;
			FailureReason = null;

			SessionCodeParseResult parsed = SessionCode.TryParse(code);

			if (!parsed.Success)
			{
				Abort(parsed.Error);
				return;
			}

			// The version rule, enforced before any network traffic: only an
			// exact match on both the wire format and the game version may join.
			if (!parsed.Data.IsCompatibleWithLocalBuild(out string incompatibility))
			{
				Abort(incompatibility);
				return;
			}

			State = JoinState.Connecting;

			NetDialer.Connect(
				parsed.Data.Address,
				parsed.Data.Port,
				OnConnected,
				message => Abort(message));

			// The secret travels in the handshake rather than as part of the
			// code's transport, so keep it for when the channel is ready.
			pendingSecret = parsed.Data.Secret;
		}

		private byte[] pendingSecret;

		private void OnConnected(MessageChannel established)
		{
			if (disposed)
			{
				established.Dispose();
				return;
			}

			channel = established;
			channel.MessageReceived += OnMessage;
			channel.Closed += OnChannelClosed;

			State = JoinState.Handshaking;

			HelloMessage hello = new HelloMessage
			{
				ProtocolVersion = ProtocolVersion.Current,
				GameVersion = Versioning.Version,
				PlayerToken = PlayerIdentity.Token,
				SessionSecret = pendingSecret
			};

			pendingSecret = null;
			channel.Send(FramedMessage.Create(ProtocolVersion.HelloMessageId, hello.ToBytes()));
		}

		private void OnMessage(byte[] framed)
		{
			if (disposed)
			{
				return;
			}

			if (!FramedMessage.TrySplit(framed, out int messageId, out byte[] payload))
			{
				Abort("The host sent a malformed reply.");
				return;
			}

			// State first: it is the only message that arrives after the
			// handshake, so handling it before the reply check keeps the
			// handshake logic from being the thing that has to know the full
			// set of message types.
			if (messageId == ProtocolVersion.UnitStateMessageId)
			{
				if (!UnitStateMessage.TryFromBytes(payload, out UnitStateMessage states, out string stateError))
				{
					Debug.LogWarning("Multiplayer: unreadable unit state - " + stateError);
					return;
				}

				UnitStateReceived?.Invoke(states);
				return;
			}

			if (messageId == ProtocolVersion.WorldSnapshotChunkMessageId)
			{
				// Handed over raw and reassembled further up. The split
				// already happened here and did its checking; doing it again
				// per chunk would only cost a second parse for nothing.
				WorldSnapshotChunkReceived?.Invoke(payload);
				return;
			}

			if (messageId == ProtocolVersion.WorldDescriptorMessageId)
			{
				if (!WorldDescriptorMessage.TryFromBytes(payload, out WorldDescriptorMessage descriptor, out string descriptorError))
				{
					Debug.LogWarning("Multiplayer: unreadable world descriptor - " + descriptorError);
					return;
				}

				WorldDescriptorReceived?.Invoke(descriptor);
				return;
			}

			if (messageId != ProtocolVersion.HelloReplyMessageId)
			{
				// Later phases add more message types here.
				return;
			}

			HelloReplyMessage reply;

			if (!HelloReplyMessage.TryFromBytes(payload, out reply, out string error))
			{
				Abort(error ?? "The host's reply could not be read.");
				return;
			}

			HostReply = reply;

			if (reply.Result != HandshakeResult.Accepted)
			{
				// The host knows exactly why, and says so in a form we can show
				// verbatim - including the version numbers behind a mismatch.
				Abort(reply.Describe());
				return;
			}

			State = JoinState.Connected;
			Connected?.Invoke();
		}

		private void OnChannelClosed(string reason)
		{
			if (disposed)
			{
				return;
			}

			// A close before the handshake completes is a failure; after it, the
			// session simply ended. Either way it happens once - a socket
			// closing can raise this more than once, and reporting a join as
			// ending two or three times would be its own confusion.
			if (State == JoinState.Connected)
			{
				State = JoinState.Failed;
				Disconnected?.Invoke(reason);
				return;
			}

			Abort(reason);
		}

		/// <summary>
		/// Fails the join, once.
		/// </summary>
		/// <remarks>
		/// Once only, because a rejection is always followed by the host
		/// hanging up - it sends the reason and then closes, and both arrive
		/// here in order. Without the guard the close overwrites the reason
		/// the host actually gave, and the player is told "the other player
		/// disconnected" a moment after being told "the host is in the main
		/// menu". The second message is the one they remember, and it is the
		/// one they can do nothing with.
		///
		/// It also fires a second message box over the first, which is its own
		/// problem: a box becomes the navigator's current screen, so anything
		/// that navigates afterwards is refused.
		/// </remarks>
		private void Abort(string reason)
		{
			if (disposed || State == JoinState.Failed || State == JoinState.Connected)
			{
				return;
			}

			FailureReason = reason;
			State = JoinState.Failed;

			Failed?.Invoke(reason);
		}

		/// <summary>Leaves the session and releases the socket.</summary>
		public void Disconnect()
		{
			if (channel != null)
			{
				channel.MessageReceived -= OnMessage;
				channel.Closed -= OnChannelClosed;
				channel.Dispose();
				channel = null;
			}

			State = JoinState.Idle;
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;
			Disconnect();
			MainThreadDispatcher.Clear();
		}
	}
}