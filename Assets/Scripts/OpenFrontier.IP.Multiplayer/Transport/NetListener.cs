using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Host side: accepts inbound connections.
	///
	/// The accept loop runs on a background thread and hands each accepted
	/// socket to the caller on the main thread. Binding is deliberately
	/// explicit about which address family it uses, because on a machine with
	/// both WiFi and Ethernet the default choice is unpredictable and the
	/// resulting code often points at an interface nobody is using.
	/// </summary>
	public sealed class NetListener : IDisposable
	{
		private readonly TcpListener listener;
		private readonly ManualResetEventSlim stopSignal = new ManualResetEventSlim(false);
		private Thread acceptThread;
		private volatile bool disposed;

		/// <summary>Raised on the main thread for each accepted peer.</summary>
		public event Action<Socket> PeerAccepted;

		/// <summary>Raised when the listener itself fails.</summary>
		public event Action<string> Failed;

		public NetListener(int port, IPAddress address)
		{
			Port = port;
			Address = address;
			listener = new TcpListener(address, port);
		}

		public int Port { get; }

		public IPAddress Address { get; }

		/// <summary>
		/// Whether the listener actually came up. False means hosting could not
		/// start - usually another instance already holding the port, or the
		/// OS refusing the bind.
		/// </summary>
		public bool IsListening { get; private set; }

		/// <summary>
		/// Begins listening. Returns false rather than throwing so the caller can
		/// show a message about the port being unavailable.
		/// </summary>
		public bool Start()
		{
			try
			{
				listener.Start();
				IsListening = true;

				acceptThread = new Thread(AcceptLoop)
				{
					IsBackground = true,
					Name = "OpenFrontier.Multiplayer.Accept"
				};

				acceptThread.Start();
				return true;
			}
			catch (SocketException e)
			{
				Failed?.Invoke(DescribeBindFailure(e));
				return false;
			}
		}

		/// <summary>The port the OS actually assigned, valid after Start succeeds.</summary>
		public int BoundPort => ((IPEndPoint)listener.LocalEndpoint).Port;

		private void AcceptLoop()
		{
			while (!stopSignal.IsSet)
			{
				TcpClient client;

				try
				{
					client = listener.AcceptTcpClient();
				}
				catch (ObjectDisposedException)
				{
					return;
				}
				catch (InvalidOperationException)
				{
					// Listener stopped while blocked in Accept.
					return;
				}
				catch (SocketException e)
				{
					if (!stopSignal.IsSet)
					{
						string message = "The connection listener failed: " + e.Message;
						MainThreadDispatcher.Post(() => Failed?.Invoke(message));
					}

					return;
				}

				TcpClient captured = client;

				MainThreadDispatcher.Post(() =>
				{
					// Hand the raw socket to the session layer, which wraps it in
					// a MessageChannel. The TcpClient is kept referenced until
					// then so it is not collected before we use it.
					_ = captured;
					PeerAccepted?.Invoke(captured.Client);
				});
			}
		}

		private string DescribeBindFailure(SocketException e)
		{
			switch (e.SocketErrorCode)
			{
				case SocketError.AddressAlreadyInUse:
					return "Another program is already using the multiplayer port. Close it and try again.";

				case SocketError.AccessDenied:
					return "This system won't allow Open Frontier to open a network port, so hosting is unavailable.";

				case SocketError.AddressNotAvailable:
					return "That network address isn't available on this device, so hosting can't start.";

				default:
					return "Couldn't start hosting: " + e.Message;
			}
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;
			stopSignal.Set();

			try
			{
				listener.Stop();
			}
			catch (Exception)
			{
				// Stopping an already-stopped listener is harmless.
			}

			stopSignal.Dispose();
		}
	}
}