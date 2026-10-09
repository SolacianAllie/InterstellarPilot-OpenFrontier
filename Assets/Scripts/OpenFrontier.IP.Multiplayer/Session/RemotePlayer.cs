using System;
using System.Threading;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Host-side state for one peer that has connected but is not yet a
	/// confirmed player.
	///
	/// A socket arriving is not a player: the peer must send a valid Hello and
	/// pass the version gate first. Until then it is "pending" and is dropped if
	/// it goes quiet, so a port scan or a stray connection cannot sit around
	/// holding resources.
	/// </summary>
	public sealed class RemotePlayer : IDisposable
	{
		private readonly Action<RemotePlayer, int, byte[]> messageHandler;
		private readonly Action<RemotePlayer, string> closeHandler;

		private bool disposed;

		public RemotePlayer(MessageChannel channel, Action<RemotePlayer, int, byte[]> messageHandler, Action<RemotePlayer, string> closeHandler)
		{
			Channel = channel;
			this.messageHandler = messageHandler;
			this.closeHandler = closeHandler;

			HandshakeTimer = new Timer(OnHandshakeTimeout, null, Timeout.Infinite, Timeout.Infinite);
		}

		/// <summary>The channel carrying this peer's messages.</summary>
		public MessageChannel Channel { get; }

		/// <summary>Drops the peer if it does not complete a handshake in time.</summary>
		public Timer HandshakeTimer { get; }

		/// <summary>
		/// The player's persistent token, set once the handshake is accepted.
		/// Null until then.
		/// </summary>
		public string Identity { get; internal set; }

		/// <summary>Whether this peer has been admitted to the session.</summary>
		public bool Admitted { get; internal set; }

		/// <summary>
		/// Player-facing name, once known. Used for display only.
		/// </summary>
		public string DisplayName { get; internal set; }

		/// <summary>Arms the handshake timeout.</summary>
		public void ExpireAfter(TimeSpan timeout)
		{
			if (!disposed)
			{
				HandshakeTimer.Change(timeout, Timeout.InfiniteTimeSpan);
			}
		}

		/// <summary>
		/// Disarms the handshake timeout once the peer has introduced itself.
		/// Safe to call more than once and after disposal.
		/// </summary>
		public void CancelHandshakeTimeout()
		{
			try
			{
				HandshakeTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
			catch (ObjectDisposedException)
			{
				// Already torn down; nothing to cancel.
			}
		}

		private void OnHandshakeTimeout(object state)
		{
			MainThreadDispatcher.Post(() =>
			{
				if (disposed || Admitted)
				{
					return;
				}

				try
				{
					HandshakeTimer.Change(Timeout.Infinite, Timeout.Infinite);
				}
				catch (ObjectDisposedException)
				{
					return;
				}

				closeHandler?.Invoke(this, "The connection timed out before it introduced itself.");
			});
		}

		/// <summary>
		/// Handles a message from this peer. The channel already guarantees
		/// delivery on the main thread.
		/// </summary>
		public void OnMessageReceived(byte[] framed)
		{
			if (disposed)
			{
				return;
			}

			if (!FramedMessage.TrySplit(framed, out int messageId, out byte[] payload))
			{
				closeHandler?.Invoke(this, "The other player sent a malformed message.");
				return;
			}

			// A ping is answered before any handshake and is not a join attempt, so it
			// is let through the gate below. Everything else needs a handshake
			// first: a peer gets exactly one chance to introduce itself.
			bool isProbe = messageId == ProtocolVersion.PingMessageId;

			if (!Admitted && !isProbe && messageId != ProtocolVersion.HelloMessageId)
			{
				closeHandler?.Invoke(this, "The other player didn't introduce itself properly.");
				return;
			}

			messageHandler?.Invoke(this, messageId, payload);
		}

		/// <summary>
		/// Closes once the pending reply has been written. Lets a refused
		/// player read the reason before the socket goes away.
		/// </summary>
		public void CloseAfterSend()
		{
			// TCP will normally have already pushed the reply into the send
			// buffer by the time this runs, and MessageChannel.Send flushes
			// synchronously, so closing now still delivers it. This is
			// immediate rather than deferred deliberately: a refused peer gets
			// its reason and the socket is released at once.
			closeHandler?.Invoke(this, null);
		}

		/// <summary>Closes with a reason, which the peer sees.</summary>
		public void CloseWithReason(string reason)
		{
			closeHandler?.Invoke(this, reason);
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;
			HandshakeTimer.Dispose();
			Channel.Dispose();
		}
	}
}