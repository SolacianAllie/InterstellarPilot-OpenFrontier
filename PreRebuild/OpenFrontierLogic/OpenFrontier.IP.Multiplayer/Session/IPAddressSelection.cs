using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Chooses which local network address to put in a join code.
	///
	/// A peer-to-peer session needs the host's address in the code, and "the
	/// address" is not obvious: a machine can have several interfaces (WiFi,
	/// Ethernet, VPN, virtual adapters), and only some of them are reachable
	/// from another device.
	///
	/// The preference order below is deliberately simple and explainable:
	///
	///   1. Loopback, when asked for - only reachable from this machine, so it
	///      is only used for testing or a same-device join.
	///   2. A private (LAN) address, preferring WiFi over Ethernet. Two players
	///      on the same network is the case that always works, and WiFi is the
	///      common way that happens.
	///   3. A public (internet) address, which requires the network to allow
	///      incoming connections.
	///
	/// When nothing suitable is found the host is told to listen on all
	/// interfaces, which still works - it just cannot be advertised in a code,
	/// so the player has to read the address off the screen.
	/// </summary>
	public static class IPAddressSelection
	{
		/// <summary>The port a session prefers to host on.</summary>
		/// <remarks>
		/// Fixed rather than left to the OS, which reverses the earlier choice - both
		/// of the reasons have since turned up. A join code carries its port, so an
		/// OS-assigned one meant every code stopped being valid the moment the host
		/// restarted, and a saved host list is mostly stale entries. And a port forward
		/// cannot be asked for in advance when nobody knows which port it will be.
		///
		/// Above 1024 so nothing needs privileges, and below the ephemeral range so
		/// the OS is not already using it. If it is taken - a second copy of the game,
		/// or another program - hosting falls back to any free port and goes without a
		/// forward, which beats refusing to host at all.
		/// </summary>
		/// <remarks>
		/// A small range rather than one number, tried in order. A single port is
		/// a single point of failure: anything else on the machine holding it
		/// stops the host, and the only fallback is an OS-assigned port whose
		/// code goes stale on the next restart. A range means a second copy of
		/// the game - or a leftover process - costs one number instead of the
		/// session. Factorio takes the same approach.
		///
		/// Where the range sits, and why:
		///
		/// Above 1024 so nothing needs privileges. Below 32768 because that is
		/// where Linux and Android hand out ports for outbound connections
		/// (32768-60999), and every one of those sits in TIME_WAIT for a minute
		/// after use - so a listening port up there spends its life being
		/// refused. Windows and macOS start at 49152, so this is clear of them
		/// too.
		///
		/// Unregistered in the IANA service registry. IANA asks that user ports
		/// be registered before use; only about 11% of 1024-49151 is taken, so
		/// finding free ones is easy, but free is not the same as unclaimed by
		/// convention - so this block was also checked against the ports games
		/// actually bind (7777, 25565, 27015, 3074, 3659, 2302-2404, 3478-3480,
		/// 10999, 19132, 34197 and the Steam ranges among them).
		///
		/// If every port in the range is taken, hosting falls back to any free
		/// port and simply goes without a forward, which beats refusing to host.
		/// </remarks>
		public const int FirstPreferredPort = 24890;

		/// <summary>How many ports the host will try before giving up on them.</summary>
		public const int PreferredPortCount = 8;

		/// <summary>
		/// The ports to try, in order, before falling back to any free one.
		/// </summary>
		/// <remarks>
		/// An array rather than a sequence so the result cannot be enumerated
		/// twice with the host making a different choice each time.
		///
		/// The order starts where the current slot points rather than always
		/// at the first port, and that is not tidiness - it is what makes two
		/// copies on one machine work at all. A join code names an address and
		/// a port, and two processes on one machine share both, so a code for
		/// the build and a code for the editor are indistinguishable from the
		/// other side: whichever happens to hold the port answers for both.
		/// Each slot taking a different port is the only thing that makes a
		/// code mean one player rather than "something on this machine".
		/// </remarks>
		public static int[] PreferredPorts()
		{
			int[] ports = new int[PreferredPortCount];
			int start = PlayerProfile.PortOffset(PreferredPortCount);

			for (int i = 0; i < ports.Length; i++)
			{
				ports[i] = FirstPreferredPort + ((start + i) % PreferredPortCount);
			}

			return ports;
		}

		/// <summary>
		/// Chooses where to listen, and which address goes into a join code.
		/// </summary>
		/// <remarks>
		/// These are two different questions, and until now they were answered
		/// as one - which is why a machine holding both a WiFi address and a
		/// public one could only ever be reached over WiFi.
		///
		/// The listener binds to every interface, so a session is reachable on
		/// WiFi, on Ethernet, and on any public address the machine actually
		/// holds. Binding to a single chosen address instead meant whichever
		/// interface the picker happened to land on, and every other route in
		/// was simply closed.
		///
		/// The advertised address stays singular, because a join code holds
		/// exactly one, and stays LAN-first, because two players on the same
		/// network is the case that always works and needs nothing set up.
		/// </remarks>
		/// <param name="bindAddress">Always <see cref="IPAddress.Any"/>.</param>
		/// <param name="advertisedAddress">The address to put in the join code.</param>
		/// <param name="problem">Why no usable address was found, or null.</param>
		public static void ChooseEndpoints(
			out IPAddress bindAddress,
			out IPAddress advertisedAddress,
			out string problem)
		{
			// Every interface, on a port the OS picks. A fixed port would
			// collide with another copy of the game or another program,
			// and the code carries the port anyway so nobody has to type it.
			bindAddress = IPAddress.Any;
			advertisedAddress = null;
			problem = null;

			if (TryGetReachableAddress(out IPAddress selected))
			{
				advertisedAddress = selected;
				return;
			}

			// Listening would still have worked - the bind above succeeds
			// regardless - but a code with no address in it is one nobody
			// can use, so a session is not worth starting.
			problem = "Couldn't identify a network address other players can reach. "
				+ "Connect to a network and try again.";
		}

		/// <summary>
		/// The private address another machine on this network would reach us at.
		/// </summary>
		/// <remarks>
		/// Separate from the advertised one because a port forward needs this and a
		/// join code does not: the router forwards to a private address, while
		/// players on the same network use whichever was advertised. They are usually
		/// the same, but not on a machine that holds a public address too.
		/// </remarks>
		public static bool TryGetPrivateAddress(out IPAddress address)
		{
			TryGetReachableAddress(out address);
		
			return address != null && IsPrivate(address);
		}

		/// <summary>
		/// Finds the most likely reachable local address, preferring a private
		/// WiFi address over anything else.
		/// </summary>
		public static bool TryGetReachableAddress(out IPAddress address)
		{
			address = null;

			IPAddress wifiPrivate = null;
			IPAddress anyPrivate = null;
			IPAddress anyPublic = null;

			foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (!IsUsable(adapter))
				{
					continue;
				}

				bool isWireless = adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;

				foreach (UnicastIPAddressInformation unicast in adapter.GetIPProperties().UnicastAddresses)
				{
					IPAddress candidate = unicast.Address;

					if (candidate.AddressFamily != AddressFamily.InterNetwork)
					{
						// This protocol version encodes IPv4 only. An IPv6-only
						// interface is skipped rather than silently producing a
						// code no one can join.
						continue;
					}

					if (IPAddress.IsLoopback(candidate))
					{
						continue;
					}

					if (IsPrivate(candidate))
					{
						if (isWireless && wifiPrivate == null)
						{
							wifiPrivate = candidate;
						}

						if (anyPrivate == null)
						{
							anyPrivate = candidate;
						}
					}
					else if (anyPublic == null)
					{
						anyPublic = candidate;
					}
				}
			}

			address = wifiPrivate ?? anyPrivate ?? anyPublic;
			return address != null;
		}

		private static bool IsUsable(NetworkInterface adapter)
		{
			if (adapter.OperationalStatus != OperationalStatus.Up)
			{
				return false;
			}

			// Virtual adapters (VPN, Hyper-V, container bridges) are up and carry
			// addresses, but an address on one of those is usually not reachable
			// from another device on the player's actual network.
			switch (adapter.NetworkInterfaceType)
			{
				case NetworkInterfaceType.Loopback:
				case NetworkInterfaceType.Tunnel:
				case NetworkInterfaceType.Unknown:
					return false;

				default:
					return true;
			}
		}

		private static bool IsPrivate(IPAddress address)
		{
			byte[] bytes = address.GetAddressBytes();

			if (bytes.Length != 4)
			{
				return false;
			}

			// 10.0.0.0/8
			if (bytes[0] == 10)
			{
				return true;
			}

			// 192.168.0.0/16
			if (bytes[0] == 192 && bytes[1] == 168)
			{
				return true;
			}

			// 172.16.0.0/12
			if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
			{
				return true;
			}

			// Carrier-grade NAT: 100.64.0.0/10. Treated as private because a
			// player on one of these is effectively behind carrier NAT, so the
			// address is no more reachable than a private one.
			if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
			{
				return true;
			}

			return false;
		}
	}
}