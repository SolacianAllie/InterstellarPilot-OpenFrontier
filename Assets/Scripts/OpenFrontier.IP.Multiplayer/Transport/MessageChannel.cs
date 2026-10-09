using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Length-prefixed message framing over a stream socket.
	///
	/// Every message is a 4-byte big-endian length followed by that many bytes
	/// of payload. The length is capped: a peer must never be able to make us
	/// allocate an arbitrary buffer by claiming a huge message, so a bogus or
	/// malicious length shows up as a protocol error instead.
	///
	/// Sends are serialised with a lock so two threads cannot interleave
	/// halves of two messages on the wire. Reads happen on a single background
	/// thread and hand complete messages to the dispatcher.
	/// </summary>
	public sealed class MessageChannel : IDisposable
	{
		/// <summary>Largest single message we will accept or send, in bytes.</summary>
		/// <remarks>
		/// A full universe snapshot is roughly 2.5 MB compressed-free, so 16 MB
		/// leaves generous headroom for large worlds while still bounding what a
		/// bad length prefix can cost.
		/// </remarks>
		public const int MaxMessageSize = 16 * 1024 * 1024;

		private const int LengthPrefixBytes = 4;

		private readonly Socket socket;
		private readonly NetworkStream stream;
		private readonly object sendLock = new object();
		private readonly ManualResetEventSlim stopSignal = new ManualResetEventSlim(false);

		private Thread readThread;
		private volatile bool disposed;

		/// <summary>Raised on the read thread for each complete message.</summary>
		public event Action<byte[]> MessageReceived;

		/// <summary>Raised when the channel stops, whether by request or error.</summary>
		public event Action<string> Closed;

		public MessageChannel(Socket socket)
		{
			this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
			this.stream = new NetworkStream(socket, ownsSocket: false);

			this.socket.NoDelay = true;
		}

		/// <summary>Whether the underlying socket is still usable.</summary>
		public bool IsConnected => !disposed && socket.Connected;

		/// <summary>Starts the background read loop.</summary>
		public void StartReading()
		{
			readThread = new Thread(ReadLoop)
			{
				IsBackground = true,
				Name = "OpenFrontier.Multiplayer.Read"
			};

			readThread.Start();
		}

		/// <summary>
		/// Sends a message. Safe to call from any thread; the write is
		/// serialised against other sends.
		/// </summary>
		public void Send(byte[] payload)
		{
			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}

			if (payload.Length > MaxMessageSize)
			{
				throw new ArgumentException(
					"Message of " + payload.Length + " bytes exceeds the " + MaxMessageSize + " byte limit.", nameof(payload));
			}

			lock (sendLock)
			{
				if (disposed || !socket.Connected)
				{
					return;
				}

				try
				{
					byte[] header = new byte[LengthPrefixBytes];
					header[0] = (byte)(payload.Length >> 24);
					header[1] = (byte)(payload.Length >> 16);
					header[2] = (byte)(payload.Length >> 8);
					header[3] = (byte)(payload.Length & 0xFF);

					stream.Write(header, 0, header.Length);
					stream.Write(payload, 0, payload.Length);
					stream.Flush();
				}
				catch (Exception e) when (IsDisconnect(e))
				{
					// The peer went away mid-send. Nothing useful to do here; the
					// read loop will notice and raise Closed.
					MainThreadDispatcher.Post(() => Closed?.Invoke("The connection was lost."));
				}
			}
		}

		private void ReadLoop()
		{
			try
			{
				byte[] header = new byte[LengthPrefixBytes];

				while (!stopSignal.IsSet)
				{
					if (!ReadExactly(header, LengthPrefixBytes))
					{
						break;
					}

					int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];

					if (length < 0 || length > MaxMessageSize)
					{
						MainThreadDispatcher.Post(() => Closed?.Invoke("The other player sent a malformed message."));
						break;
					}

					byte[] payload = new byte[length];

					if (length > 0 && !ReadExactly(payload, length))
					{
						break;
					}

					Action<byte[]> handler = MessageReceived;

					if (handler != null)
					{
						// Hand off rather than invoking here: handlers touch engine
						// state, which is main-thread only.
						MainThreadDispatcher.Post(() => handler(payload));
					}
				}

				if (!stopSignal.IsSet)
				{
					MainThreadDispatcher.Post(() => Closed?.Invoke("The other player disconnected."));
				}
			}
			catch (Exception e)
			{
				if (!stopSignal.IsSet)
				{
					string message = IsDisconnect(e) ? "The other player disconnected." : "Network error: " + e.Message;
					MainThreadDispatcher.Post(() => Closed?.Invoke(message));
				}
			}
		}

		/// <summary>
		/// Reads exactly <paramref name="count"/> bytes, coping with partial
		/// reads. Returns false if the stream ended or failed first.
		/// </summary>
		private bool ReadExactly(byte[] buffer, int count)
		{
			int offset = 0;

			while (offset < count)
			{
				int read = stream.Read(buffer, offset, count - offset);

				if (read <= 0)
				{
					return false;
				}

				offset += read;
			}

			return true;
		}

		private static bool IsDisconnect(Exception e)
		{
			return e is IOException
				|| e is SocketException
				|| e is ObjectDisposedException;
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
				stream.Close();
			}
			catch (Exception)
			{
				// Closing a socket that is already gone is not an error worth
				// surfacing during teardown.
			}

			try
			{
				socket.Close();
			}
			catch (Exception)
			{
			}

			stopSignal.Dispose();

			// The read thread is a background thread and observes stopSignal, so
			// it will exit on its own; no join is needed and joining here would
			// risk blocking the main thread on a socket read.
		}
	}
}