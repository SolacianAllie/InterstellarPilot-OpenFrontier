using System;
using System.Net.Sockets;
using System.Threading;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Client side: connects to a host.
	///
	/// The connect attempt runs on a background thread because it blocks, and
	/// TCP will happily sit in a SYN retry for tens of seconds when the address
	/// is simply unreachable - long enough to freeze the game if done on the
	/// main thread. A timeout bounds it so a player is told what happened
	/// instead of watching a spinner.
	/// </summary>
	public static class NetDialer
	{
		/// <summary>How long to wait for a connection before giving up.</summary>
		public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);

		/// <summary>
		/// Attempts a connection. <paramref name="onResult"/> is invoked on the
		/// main thread with either a connected channel or a failure message fit
		/// to show a player.
		/// </summary>
		public static void Connect(System.Net.IPAddress address, int port, Action<MessageChannel> onConnected, Action<string> onFailed)
		{
			Thread worker = new Thread(() =>
			{
				Socket socket = null;
				MessageChannel channel = null;

				try
				{
					socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

					IAsyncResult pending = socket.BeginConnect(
						new System.Net.IPEndPoint(address, port), null, null);

					if (!pending.AsyncWaitHandle.WaitOne(ConnectTimeout))
					{
						socket.Close();
						socket = null;

						MainThreadDispatcher.Post(() => onFailed?.Invoke(
							"Couldn't reach the host. It may have stopped hosting, or a network is blocking the connection."));

						return;
					}

					// Completes the connect; throws if it failed.
					socket.EndConnect(pending);
					pending.AsyncWaitHandle.Close();

					channel = new MessageChannel(socket);
					channel.StartReading();

					MainThreadDispatcher.Post(() => onConnected?.Invoke(channel));
				}
				catch (SocketException e)
				{
					CloseQuietly(socket);
					CloseQuietly(channel);

					MainThreadDispatcher.Post(() => onFailed?.Invoke(DescribeFailure(e)));
				}
				catch (Exception e)
				{
					CloseQuietly(socket);
					CloseQuietly(channel);

					MainThreadDispatcher.Post(() => onFailed?.Invoke("Couldn't join: " + e.Message));
				}
			})
			{
				IsBackground = true,
				Name = "OpenFrontier.Multiplayer.Connect"
			};

			worker.Start();
		}

		private static string DescribeFailure(SocketException e)
		{
			switch (e.SocketErrorCode)
			{
				case SocketError.TimedOut:
					return "Couldn't reach the host - the connection timed out.";

				case SocketError.ConnectionRefused:
					return "Couldn't reach the host. The session may have ended, or the host may be behind a network that blocks incoming connections.";

				case SocketError.NetworkUnreachable:
				case SocketError.HostUnreachable:
					return "Couldn't reach the host. Check that you're on the same network, or that the host's network allows incoming connections.";

				case SocketError.AddressFamilyNotSupported:
					return "This session code uses an address format this device can't connect to.";

				default:
					return "Couldn't join: " + e.Message;
			}
		}

		private static void CloseQuietly(IDisposable disposable)
		{
			try
			{
				disposable?.Dispose();
			}
			catch (Exception)
			{
				// Teardown only; a failure here says nothing useful.
			}
		}
	}
}