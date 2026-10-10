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
		NotAccepting = 3,

		/// <summary>
		/// A host answered, and is in the main menu rather than in a game.
		/// </summary>
		/// <remarks>
		/// Its own state because the player needs to know a join is worth
		/// retrying in a moment rather than being something they have to go and
		/// fix. Folding it into NotAccepting would read as "this host is not
		/// interested", which is not what is happening.
		/// </remarks>
		InMainMenu = 4
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

		/// <summary>Why the probe failed, for the log.</summary>
		/// <remarks>
		/// Never shown to the player. The host list deliberately says only
		/// "Offline", because from out here a closed game, a blocked router
		/// and an out-of-date join code cannot be told apart, and guessing
		/// between them would be worse than admitting it. They are perfectly
		/// distinguishable from a log though, which is the difference between
		/// a host being annoying and a host being debuggable.
		/// </remarks>
		public string Detail;

		/// <summary>
		/// Whether the host could be joined, which is what the list cares about.
		/// </summary>
		public bool CanJoin => Reachability == HostReachability.Online && AcceptingPlayers;

		/// <summary>
		/// Whether this host is up but in the main menu, so a join would be
		/// refused until they start a game.
		/// </summary>
		public bool IsInMainMenu => Reachability == HostReachability.InMainMenu;

		/// <summary>
		/// Whether a probe has actually completed for this host yet.
		/// </summary>
		/// <remarks>
		/// Distinct from "Offline". Before the first probe returns, the honest
		/// answer is "we don't know", and showing "Offline" until then would
		/// flash a lie every time the list opened.
		/// </remarks>
		public bool IsKnown => Reachability != HostReachability.Unknown;

		/// <summary>
		/// The connection state alone, as one word or short phrase.
		/// </summary>
		/// <remarks>
		/// Split out from <see cref="Describe"/> so the host list can give the
		/// player count its own label instead of running the two together into
		/// one string. The details panel, which has one label for the lot,
		/// still uses <see cref="Describe"/>.
		/// </remarks>
		public string DescribeState()
		{
			switch (Reachability)
			{
				case HostReachability.Unknown:
					return "Checking...";

				case HostReachability.Online:
					return AcceptingPlayers ? "Online" : "Not accepting players";

				case HostReachability.VersionMismatch:
					return "Different version";

				case HostReachability.NotAccepting:
					return "Not accepting players";

				case HostReachability.InMainMenu:
					// Says what to do about it, not just what it is: the host is
					// fine, they are just not in a game yet.
					return "In the main menu";

				default:
					// No reply. Either nothing is listening, or the host is
					// offline, or a network in between blocked it - all
					// indistinguishable from out here, so no more specific
					// claim is made.
					return "Offline";
			}
		}

		/// <summary>A line fit to show next to the host's name.</summary>
		public string Describe()
		{
			string state = DescribeState();

			if (Reachability == HostReachability.VersionMismatch && HostVersion != null)
			{
				// The one case where knowing the state is not enough - the
				// player has to be told which build to go and get.
				return state + " (" + HostVersion + ")";
			}

			if (Reachability == HostReachability.Online && AcceptingPlayers && PlayerCount > 0)
			{
				return state + " - " + PlayerCount + " connected";
			}

			return state;
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

		/// <summary>
		/// Last reason logged for each host, so a host that stays down does
		/// not repeat itself on every poll.
		/// </summary>
		private static readonly System.Collections.Generic.Dictionary<string, string> LastLogged
			= new System.Collections.Generic.Dictionary<string, string>();

		private static readonly object LastLoggedLock = new object();

		/// <summary>Reports a host unreachable, and says why in the log.</summary>
		private static HostProbeResult Unreachable(SessionCodeData host, string reason)
		{
			HostProbeResult result = new HostProbeResult
				{
					Reachability = HostReachability.Unreachable,
					Detail = reason
				};

			string key = host.Address + ":" + host.Port;

			lock (LastLoggedLock)
			{
				if (LastLogged.TryGetValue(key, out string previous) && previous == reason)
				{
					// The list polls on a timer and a host that is down
					// stays down. Once is enough; repeating it every few
					// seconds would bury everything else in the log.
					return result;
				}

				LastLogged[key] = reason;
			}

			// Queued: this runs on the probe's worker thread.
			MainThreadDispatcher.Post(() => Debug.LogWarning(
				"Multiplayer: host " + key + " is unreachable - " + reason));

			return result;
		}

		/// <summary>Turns a socket error into something a player can act on.</summary>
		private static string Describe(SocketException e)
		{
			switch (e.SocketErrorCode)
			{
				case SocketError.ConnectionRefused:
					// The one that matters most: the machine answered,
					// but nothing is listening on that port. Since a
					// join code carries its own port, that almost always
					// means the code is out of date, not the host off.
					return "connection refused - nothing is listening there (is the code out of date?)";

				case SocketError.HostUnreachable:
					return "no route to that address";

				case SocketError.NetworkUnreachable:
					return "this device has no route to that network";

				case SocketError.TimedOut:
					return "the connection timed out";

				default:
					return e.SocketErrorCode + " (" + e.Message + ")";
			}
		}

		private static HostProbeResult ProbeOnce(SessionCodeData host)
		{
			using (TcpClient client = new TcpClient())
			{
				client.NoDelay = true;

				IAsyncResult pending = client.BeginConnect(host.Address, host.Port, null, null);

				if (!pending.AsyncWaitHandle.WaitOne(Timeout))
				{
					// Nothing at all answered. A refused connection (below)
					// means a stale code; silence here means something in
					// between is dropping the packets - a different network,
					// or a firewall.
					return Unreachable(host, "no reply after "
						+ (int)Timeout.TotalSeconds
						+ "s - packets are being dropped, so the devices are probably on different networks");
				}

				try
				{
					client.EndConnect(pending);
				}
				catch (SocketException e)
				{
					return Unreachable(host, Describe(e));
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
						return Unreachable(host, "connected, but the host never sent a reply");
					}

					int length = (replyHeader[0] << 24) | (replyHeader[1] << 16) | (replyHeader[2] << 8) | replyHeader[3];

					// Bounded so a wrong or hostile length cannot make this
					// allocate an arbitrary buffer.
					if (length <= 0 || length > 4096)
					{
						return Unreachable(host, "the reply claimed an impossible length of " + length);
					}

					byte[] replyFramed = new byte[length];

					if (!ReadExactly(stream, replyFramed, length))
					{
						return Unreachable(host, "the reply was cut short");
					}

					if (!FramedMessage.TrySplit(replyFramed, out int messageId, out byte[] body))
					{
						return Unreachable(host, "the reply was not a message this build understands");
					}

					if (messageId != ProtocolVersion.PongMessageId)
					{
						return Unreachable(host, "answered with message " + messageId + " instead of a PONG");
					}

					if (!PongMessage.TryFromBytes(body, out PongMessage pong, out string error))
					{
						// A PONG this build cannot read means the host is running a
						// different build. That is a real answer rather than a
						// failure, but it is reported through here so it reaches
						// the log, which is where someone stuck on "Offline"
						// will end up looking.
						return Unreachable(host, "answered with a PONG this build cannot read: " + error);
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

			if (pong.AcceptingPlayers)
			{
				result.Reachability = HostReachability.Online;
				return result;
			}

			// Up but refusing. The main menu is called out separately because it
			// is temporary and expected, unlike a host that has chosen not to
			// take players.
			result.Reachability = pong.InMainMenu
				? HostReachability.InMainMenu
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