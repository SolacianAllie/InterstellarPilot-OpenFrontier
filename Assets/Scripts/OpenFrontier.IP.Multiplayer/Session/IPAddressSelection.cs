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
		/// <summary>
		/// Picks an address to listen on and a port to request.
		/// </summary>
		/// <param name="address">Address to bind, or null to bind all interfaces.</param>
		/// <param name="port">Requested port, or 0 to let the OS choose.</param>
		/// <param name="problem">Why no usable address was found, or null.</param>
		public static void ListenAll(out IPAddress address, out int port, out string problem)
		{
			address = null;
			problem = null;

			// Port 0 asks the OS for any free port. A memorable port would be
			// nicer for players typing an address by hand, but an OS-assigned one
			// can never collide with another program or another copy of the game
			// - and the code carries the port anyway, so it never has to be typed.
			port = 0;

			if (TryGetReachableAddress(out IPAddress selected))
			{
				address = selected;
				return;
			}

			problem = "Couldn't identify a network address other players can reach. "
				+ "Connect to a network and try again.";
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