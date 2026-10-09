using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>What a probe found out about a host.</summary>
	public enum HostReachability
	{
		/// <summary>No probe has completed yet.</summary>
		Unknown = -1,

		/// <summary>Nothing answered in time.</summary>
		Unreachable = 0,

		/// <summary>A host answered.</summary>
		Online = 1,

		/// <summary>A host answered, but is running a different build.</summary>
		VersionMismatch = 2,

		/// <summary>A host answered but has stopped accepting players.</summary>
		NotAccepting = 3
	}

	/// <summary>The result of probing one host.</summary>
	public struct HostProbeResult
	{
		public HostReachability Reachability;

		/// <summary>The host's game version, when one was reported.</summary>
		public System.Version HostVersion;

		/// <summary>Whether the host will admit a player right now.</summary>
		public bool AcceptingPlayers;

		/// <summary>Players already connected, when reported.</summary>
		public int PlayerCount;

		/// <summary>
		/// Whether the host could be joined, which is what the list cares about.
		/// </summary>
		public bool CanJoin => Reachability == HostReachability.Online && AcceptingPlayers;

		/// <summary>
		/// Whether a probe has actually completed for this host yet.
		/// </summary>
		/// <remarks>
		/// Distinct from "Offline". Before the first probe returns, the honest
		/// answer is "we don't know", and showing "Offline" until then would
		/// flash a lie every time the list opened.
		/// </remarks>
		public bool IsKnown => Reachability != HostReachability.Unknown;

		/// <summary>A line fit to show next to the host's name.</summary>
		public string Describe()
		{
			switch (Reachability)
			{
				case HostReachability.Unknown:
					return "Checking...";

				case HostReachability.Online:
					return AcceptingPlayers
						? "Online" + (PlayerCount > 0 ? " - " + PlayerCount + " connected" : string.Empty)
						: "Online - not accepting players";

				case HostReachability.VersionMismatch:
					return "Different version" + (HostVersion != null ? " (" + HostVersion + ")" : string.Empty);

				case HostReachability.NotAccepting:
					return "Online - not accepting players";

				default:
					// No reply. Either nothing is listening, or the host is
					// offline, or a network in between blocked it - all
					// indistinguishable from out here, so no more specific
					// claim is made.
					return "Offline";
			}
		}
	}

	/// <summary>
	/// Probes whether a host is reachable, using the same port as a real join.
	///
	/// There is no server to ask, so this opens a real connection to the host
	/// and exchanges one tiny message. A reply proves the host process is
	/// running, is in a game, and has multiplayer listening right now - which is
	/// precisely the question worth answering before a player tries to join.
	///
	/// The probe never creates a session on the host: the host answers it
	/// without a handshake and hangs up, so refreshing a list of hosts does not
	/// fill a session with phantom players.
	/// </summary>
	public static class HostProbe
	{
		/// <summary>
		/// How long to wait for a reply before calling a host unreachable.
		/// </summary>
		/// <remarks>
		/// Kept short because this runs per host in a list. Long enough to cover
		/// a normal round trip including a phone waking up, short enough that a
		/// list of a dozen dead hosts does not sit there for a minute.
		/// </remarks>
		public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

		/// <summary>
		/// Probes a host. <paramref name="onResult"/> is invoked on the main
		/// thread, always exactly once.
		/// </summary>
		public static void Probe(SessionCodeData host, Action<HostProbeResult> onResult)
		{
			if (host == null)
			{
				MainThreadDispatcher.Post(() => onResult?.Invoke(
					new HostProbeResult { Reachability = HostReachability.Unreachable }));

				return;
			}

			Thread worker = new Thread(() => ProbeBlocking(host, onResult))
			{
				IsBackground = true,
				Name = "OpenFrontier.Multiplayer.Probe"
			};

			worker.Start();
		}

		/// <summary>
		/// Probes several hosts concurrently.
		/// </summary>
		///
		/// Sequential probing would take the timeout per dead host, so a list of
		/// ten unreachable hosts would take half a minute. Probing in parallel
		/// bounds the whole refresh to one timeout.
		/// <param name="hosts">Hosts to probe, in display order.</param>
		/// <param name="onResult">
		/// Called on the main thread once per host, with the host's own result.
		/// </param>
		public static void ProbeAll(
			System.Collections.Generic.IReadOnlyList<SessionCodeData> hosts,
			Action<SessionCodeData, HostProbeResult> onResult)
		{
			if (hosts == null || hosts.Count == 0)
			{
				return;
			}

			foreach (SessionCodeData host in hosts)
			{
				Probe(host, result =>
				{
					if (onResult != null)
					{
						onResult(host, result);
					}
				});
			}
		}

		private static void ProbeBlocking(SessionCodeData host, Action<HostProbeResult> onResult)
		{
			HostProbeResult result;

			try
			{
				result = ProbeOnce(host);
			}
			catch (Exception)
			{
				// A probe failing is the normal case for a host that is not there.
				// Never let it surface as an error to the player.
				result = new HostProbeResult { Reachability = HostReachability.Unreachable };
			}

			HostProbeResult captured = result;
			MainThreadDispatcher.Post(() => onResult?.Invoke(captured));
		}

		private static HostProbeResult ProbeOnce(SessionCodeData host)
		{
			using (TcpClient client = new TcpClient())
			{
				client.NoDelay = true;

				IAsyncResult pending = client.BeginConnect(host.Address, host.Port, null, null);

				if (!pending.AsyncWaitHandle.WaitOne(Timeout))
				{
					return new HostProbeResult { Reachability = HostReachability.Unreachable };
				}

				try
				{
					client.EndConnect(pending);
				}
				catch (SocketException)
				{
					return new HostProbeResult { Reachability = HostReachability.Unreachable };
				}
				finally
				{
					pending.AsyncWaitHandle.Close();
				}

				using (NetworkStream stream = client.GetStream())
				{
					PingMessage ping = new PingMessage { ProtocolVersion = ProtocolVersion.Current };
					byte[] framed = FramedMessage.Create(ProtocolVersion.PingMessageId, ping.ToBytes());

					byte[] header = new byte[4];
					header[0] = (byte)(framed.Length >> 24);
					header[1] = (byte)(framed.Length >> 16);
					header[2] = (byte)(framed.Length >> 8);
					header[3] = (byte)(framed.Length & 0xFF);

					stream.Write(header, 0, header.Length);
					stream.Write(framed, 0, framed.Length);
					stream.Flush();

					byte[] replyHeader = new byte[4];

					if (!ReadExactly(stream, replyHeader, 4))
					{
						return new HostProbeResult { Reachability = HostReachability.Unreachable };
					}

					int length = (replyHeader[0] << 24) | (replyHeader[1] << 16) | (replyHeader[2] << 8) | replyHeader[3];

					// Bounded so a wrong or hostile length cannot make this
					// allocate an arbitrary buffer.
					if (length <= 0 || length > 4096)
					{
						return new HostProbeResult { Reachability = HostReachability.Unreachable };
					}

					byte[] replyFramed = new byte[length];

					if (!ReadExactly(stream, replyFramed, length))
					{
						return new HostProbeResult { Reachability = HostReachability.Unreachable };
					}

					if (!FramedMessage.TrySplit(replyFramed, out int messageId, out byte[] body))
					{
						return new HostProbeResult { Reachability = HostReachability.Unreachable };
					}

					if (messageId != ProtocolVersion.PongMessageId)
					{
						return new HostProbeResult { Reachability = HostReachability.Unreachable };
					}

					if (!PongMessage.TryFromBytes(body, out PongMessage pong, out string error))
					{
						Debug.LogWarning("Multiplayer probe got an unreadable reply: " + error);
						return new HostProbeResult { Reachability = HostReachability.Unreachable };
					}

					return Classify(pong);
				}
			}
		}

		private static HostProbeResult Classify(PongMessage pong)
		{
			HostProbeResult result = new HostProbeResult
			{
				HostVersion = pong.GameVersion,
				AcceptingPlayers = pong.AcceptingPlayers,
				PlayerCount = pong.PlayerCount
			};

			if (pong.ProtocolVersion != ProtocolVersion.Current)
			{
				// Something answered, so the host is up - it just speaks a
				// different protocol. Worth saying so, because "offline" would
				// be wrong and would send the player looking for the wrong
				// problem.
				result.Reachability = HostReachability.VersionMismatch;
				return result;
			}

			if (pong.GameVersion != null && pong.GameVersion != Versioning.Version)
			{
				result.Reachability = HostReachability.VersionMismatch;
				return result;
			}

			result.Reachability = pong.AcceptingPlayers
				? HostReachability.Online
				: HostReachability.NotAccepting;

			return result;
		}

		private static bool ReadExactly(NetworkStream stream, byte[] buffer, int count)
		{
			int offset = 0;

			while (offset < count)
			{
				int read;

				try
				{
					read = stream.Read(buffer, offset, count - offset);
				}
				catch (IOException)
				{
					return false;
				}

				if (read <= 0)
				{
					return false;
				}

				offset += read;
			}

			return true;
		}
	}
}