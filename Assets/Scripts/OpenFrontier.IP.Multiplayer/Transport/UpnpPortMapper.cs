using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>What happened when the gateway was asked to forward a port.</summary>
	public enum UpnpStatus
	{
		/// <summary>Nothing has been asked yet.</summary>
		NotAttempted = 0,

		/// <summary>Asked, and still waiting to hear back.</summary>
		Asking,

		/// <summary>The gateway forwarded the port.</summary>
		Mapped,

		/// <summary>Nothing on the network answered the discovery.</summary>
		NoGateway,

		/// <summary>A gateway answered but will not forward ports.</summary>
		Refused,

		/// <summary>The attempt failed in a way worth reporting.</summary>
		Failed,

		/// <summary>The forward was taken back down.</summary>
		Removed,

		/// <summary>There is no way out of this network, so nothing was asked.</summary>
		NotNeeded
	}

	/// <summary>One outcome of asking the gateway to forward a port.</summary>
	public struct UpnpResult
	{
		public UpnpStatus Status;

		/// <summary>The port the gateway was asked to forward, when there is one.</summary>
		public int Port;

		/// <summary>The gateway that answered, for the session panel.</summary>
		public string Gateway;

		/// <summary>A sentence fit to show a player, or null.</summary>
		public string Detail;
	}

	/// <summary>
	/// Asks the network's gateway to forward a port, over UPnP.
	///
	/// What this is for: a host behind a router is unreachable from the
	/// internet until the router is told to let a connection through to it.
	/// UPnP IGD is the mechanism most home routers expose for exactly this -
	/// the game asks, the router opens a hole, and the host closes it again
	/// when it stops.
	///
	/// What it is NOT for: reaching across the internet on a phone. Mobile
	/// networks sit behind carrier NAT, so there is no gateway of the player's
	/// own to ask, and no amount of asking will help.
	///
	/// Every step here is best-effort and none of it may hold up hosting. A
	/// gateway that is slow, absent, or actively unhelpful has to cost the
	/// player nothing but a line on a panel they can read. So discovery runs
	/// on its own thread, every request is bounded by a timeout, and no
	/// failure is ever thrown back at the caller.
	/// </summary>
	public static class UpnpPortMapper
	{
		/// <summary>
		/// How long to listen for a gateway answering discovery.
		/// </summary>
		/// <remarks>
		/// SSDP replies are supposed to arrive well inside a second, but the
		/// search is repeated a few times because the first datagram is
		/// routinely lost, and a busy network needs longer than a quiet one.
		/// Kept short in absolute terms: a player waiting to host should not
		/// wait more than this, and by then has usually given up on whatever
		/// they were doing anyway.
		/// </remarks>
		public static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(3);

		/// <summary>How long to allow for the description and the mapping itself.</summary>
		private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

		/// <summary>How long to allow for taking a mapping back down.</summary>
		private static readonly TimeSpan RemoveTimeout = TimeSpan.FromSeconds(3);

		private const string SsdpAddress = "239.255.255.250";
		private const int SsdpPort = 1900;

		/// <summary>
		/// The service types that can forward a port. Most routers expose the
		/// first; the second is for DSL modems that do the NAT themselves.
		/// </summary>
		private static readonly string[] MappingServices =
		{
			"urn:schemas-upnp-org:service:WANIPConnection:1",
			"urn:schemas-upnp-org:service:WANIPConnection:2",
			"urn:schemas-upnp-org:service:WANPPPConnection:1"
		};

		/// <summary>What the mapping is called in the router's own list.</summary>
		private const string MappingDescription = "Open Frontier multiplayer";

		// Kept so the mapping can be taken down again without rediscovering
		// the gateway, which may have gone quiet in the meantime.
		private static string controlUrl;
		private static int mappedPort;
		private static readonly object stateLock = new object();

		/// <summary>The result of the most recent attempt, for the session panel.</summary>
		public static UpnpResult LastResult { get; private set; }

		/// <summary>
		/// Asks the gateway to forward <paramref name="port"/> to this machine.
		/// <paramref name="onResult"/> is invoked on the main thread, once.
		/// </summary>
		public static void Map(
			int port,
			IPAddress internalAddress,
			Action<UpnpResult> onResult,
			Action<string> onLog = null)
		{
			if (port <= 0 || port > 65535)
			{
				Post(upnp =>
				{
					upnp.Status = UpnpStatus.Failed;
					upnp.Detail = "That port number cannot be used";
				}, onResult);
				return;
			}

			// Nothing to ask if this network has no way out. Cheaper than a
			// discovery that is guaranteed to time out, and it keeps airplane
			// mode from reporting a missing gateway as a problem.
			if (!HasDefaultRoute())
			{
				Post(upnp =>
				{
					upnp.Status = UpnpStatus.NotNeeded;
					upnp.Port = port;
					upnp.Detail = "No way out of this network";
				}, onResult);
				return;
			}

			Thread worker = new Thread(() =>
			{
				UpnpResult result = MapBlocking(port, internalAddress, onLog);
				Post(upnp => { }, onResult, result);
			})
			{
				IsBackground = true,
				Name = "OpenFrontier.Multiplayer.UPnP"
			};

			worker.Start();
		}

		/// <summary>
		/// Takes a mapping down again.
		/// </summary>
		/// <remarks>
		/// This is the part that matters most and is easiest to skip. A forward
		/// left behind is a hole in the player's firewall that outlives the
		/// game, and routers keep them until something removes them - which, for
		/// many, is nothing at all until a reboot.
		/// </remarks>
		public static void Remove(Action<UpnpResult> onResult = null)
		{
			string url;
			int port;

			lock (stateLock)
			{
				url = controlUrl;
				port = mappedPort;
				controlUrl = null;
				mappedPort = 0;
			}

			if (url == null || port <= 0)
			{
				Post(upnp => { }, onResult, new UpnpResult { Status = UpnpStatus.Removed });
				return;
			}

			Thread worker = new Thread(() =>
			{
				UpnpResult result = new UpnpResult { Status = UpnpStatus.Removed, Port = port };

				try
				{
					Send(url, "DeletePortMapping", BuildDelete(port), RemoveTimeout, out string ignored);
					// Deliberately not treated as a failure. 714 is "no such
					// entry", which is the outcome that was wanted anyway.
				}
				catch (Exception)
				{
					// Best effort. The router may have forgotten it, rebooted,
					// or the player may have closed the game mid-request. None
					// of that is worth interrupting anything for, but it is
					// worth saying so in the log.
					result.Detail = "The port forward may still be set";
				}

				Post(upnp => { }, onResult, result);
			})
			{
				IsBackground = true,
				Name = "OpenFrontier.Multiplayer.UPnP.Remove"
			};

			worker.Start();
		}

		// ---- the work, all of it blocking and all of it on one thread ----

		private static UpnpResult MapBlocking(int port, IPAddress internalAddress, Action<string> onLog)
		{
			UpnpResult result = new UpnpResult { Status = UpnpStatus.Asking, Port = port };

			string location = Discover(onLog);

			if (location == null)
			{
				result.Status = UpnpStatus.NoGateway;
				result.Detail = "No UPnP router on this network";
				return result;
			}

			result.Gateway = HostOf(location);

			string control;

			try
			{
				control = FindControlUrl(location);
			}
			catch (Exception e)
			{
				// Fetching the description is an ordinary HTTP request to
				// whatever address the gateway put in its own reply, so it
				// fails in every way a request can. None of that may reach
				// the caller: this runs on a background thread with a
				// hosting session already up, and an escaping exception
				// would take the thread with it and say nothing at all.
				result.Status = UpnpStatus.Failed;
				result.Detail = "Could not read the router's description";
				return result;
			}

			if (control == null)
			{
				result.Status = UpnpStatus.Refused;
				result.Detail = "This router will not forward ports";
				return result;
			}

			string body;

			try
			{
				Send(control, "AddPortMapping", BuildAdd(port, internalAddress), RequestTimeout, out body);
			}
			catch (Exception e)
			{
				result.Status = UpnpStatus.Failed;
				result.Detail = "Could not ask the router to forward";
				return result;
			}

			// A router is allowed to answer "fine" and mean it. The response
			// body is where it says otherwise, and an error in it means the
			// mapping was never created.
			string fault = FaultOf(body);

			if (fault != null)
			{
				result.Status = UpnpStatus.Refused;
				result.Detail = fault.StartsWith("another program", StringComparison.Ordinal)
					? "Another program already uses this port"
					: "This router will not forward ports";
				return result;
			}

			lock (stateLock)
			{
				controlUrl = control;
				mappedPort = port;
			}

			result.Status = UpnpStatus.Mapped;
			result.Detail = "Reachable from outside this network";
			onLog?.Invoke("UPnP: forwarding " + result.Gateway + ":" + port + " to " + internalAddress);
			return result;
		}

		/// <summary>
		/// Asks every gateway on the network to identify itself.
		/// </summary>
		private static string Discover(Action<string> onLog)
		{
			UdpClient client = null;

			try
			{
				client = new UdpClient();
				client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
				client.Client.ReceiveTimeout = 500;

				IPEndPoint group = new IPEndPoint(IPAddress.Parse(SsdpAddress), SsdpPort);

				byte[] search = Encoding.ASCII.GetBytes(BuildSearch());

				// Repeated, because the first datagram of an SSDP search is
				// reliably lost often enough to matter.
				for (int i = 0; i < 3; i++)
				{
					client.Send(search, search.Length, group);

					if (i < 2)
					{
						Thread.Sleep(500);
					}
				}

				DateTime deadline = DateTime.UtcNow + DiscoveryTimeout;

				while (DateTime.UtcNow < deadline)
				{
					try
					{
						IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
						byte[] reply = client.Receive(ref from);

						string location = HeaderOf(Encoding.ASCII.GetString(reply), "LOCATION");

						if (!string.IsNullOrEmpty(location))
						{
							onLog?.Invoke("UPnP: " + from.Address + " answered discovery");
							return location;
						}
					}
					catch (SocketException)
					{
						// A read timeout, which is the normal way round here.
					}
				}

				return null;
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				try
				{
					client?.Close();
				}
				catch (Exception)
				{
					// Teardown only.
				}
			}
		}

		/// <summary>
		/// Fetches the gateway's description and finds where to send port
		/// mapping requests.
		/// </summary>
		private static string FindControlUrl(string location)
		{
			string xml = Fetch(location, RequestTimeout);

			if (string.IsNullOrEmpty(xml))
			{
				return null;
			}

			XmlDocument document = new XmlDocument();

			try
			{
				document.LoadXml(xml);
			}
			catch (XmlException)
			{
				return null;
			}

			XmlNamespaceManager namespaces = new XmlNamespaceManager(document.NameTable);
			namespaces.AddNamespace("u", "urn:schemas-upnp-org:device-1-0");

			foreach (XmlElement service in document.SelectNodes("//u:service", namespaces))
			{
				string type = Text(service, "serviceType");

				if (type == null || !IsMappingService(type))
				{
					continue;
				}

				string control = Text(service, "controlURL");

				if (!string.IsNullOrEmpty(control))
				{
					return new Uri(new Uri(location), control).AbsoluteUri;
				}
			}

			return null;
		}

		private static bool IsMappingService(string serviceType)
		{
			foreach (string candidate in MappingServices)
			{
				if (string.Equals(serviceType, candidate, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		// ---- the two SOAP bodies ----

		/// <summary>
		/// The request that opens the hole.
		/// </summary>
		/// <remarks>
		/// Internal and external port are the same number deliberately: the
		/// host has no second port to hand out, so forwarding one to the other
		/// is what makes the join code work from outside.
		///
		/// A zero lease is the convention for "until told otherwise" - the
		/// router forgets nothing, so <see cref="Remove"/> is what actually
		/// ends it, which is why that matters.
		/// </remarks>
		public static string BuildAdd(int port, IPAddress internalAddress)
		{
			string address = internalAddress == null ? string.Empty : internalAddress.ToString();

			return Envelope(
				MappingServices[0],
				"AddPortMapping",
				"<NewRemoteHost></NewRemoteHost>"
				+ "<NewExternalPort>" + N(port) + "</NewExternalPort>"
				+ "<NewProtocol>TCP</NewProtocol>"
				+ "<NewInternalPort>" + N(port) + "</NewInternalPort>"
				+ "<NewInternalClient>" + address + "</NewInternalClient>"
				+ "<NewEnabled>1</NewEnabled>"
				+ "<NewPortMappingDescription>" + MappingDescription + "</NewPortMappingDescription>"
				+ "<NewLeaseDuration>0</NewLeaseDuration>");
		}

		/// <summary>The request that closes the hole again.</summary>
		public static string BuildDelete(int port)
		{
			return Envelope(
				MappingServices[0],
				"DeletePortMapping",
				"<NewRemoteHost></NewRemoteHost>"
				+ "<NewExternalPort>" + N(port) + "</NewExternalPort>"
				+ "<NewProtocol>TCP</NewProtocol>");
		}

		private static string Envelope(string service, string action, string arguments)
		{
			return "<?xml version=\"1.0\"?>"
				+ "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\""
				+ " s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
				+ "<s:Body>"
				+ "<u:" + action + " xmlns:u=\"" + service + "\">"
				+ arguments
				+ "</u:" + action + ">"
				+ "</s:Body>"
				+ "</s:Envelope>";
		}

		/// <summary>The SSDP search datagram.</summary>
		public static string BuildSearch()
		{
			// MX is how long a gateway may wait before replying, in seconds.
			// Kept under our own discovery budget so a slow router still
			// answers in time to be heard.
			return "M-SEARCH * HTTP/1.1\r\n"
				+ "HOST: " + SsdpAddress + ":" + SsdpPort + "\r\n"
				+ "MAN: \"ssdp:discover\"\r\n"
				+ "MX: 2\r\n"
				+ "ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n"
				+ "\r\n";
		}

		// ---- small parsing helpers ----

		/// <summary>Pulls a header value out of a raw HTTP message.</summary>
		public static string HeaderOf(string message, string header)
		{
			if (string.IsNullOrEmpty(message))
			{
				return null;
			}

			foreach (string line in message.Split('\n'))
			{
				string trimmed = line.Trim();

				if (trimmed.StartsWith(header + ":", StringComparison.OrdinalIgnoreCase))
				{
					return trimmed.Substring(header.Length + 1).Trim();
				}
			}

			return null;
		}

		/// <summary>
		/// Reads a SOAP fault out of a response body, if there is one.
		/// </summary>
		/// <remarks>
		/// A router that refuses a mapping usually still answers with HTTP 200
		/// and the refusal tucked inside the body. Treating that as success
		/// would leave the panel claiming the player is reachable when they
		/// are not - so the body is checked, and the common IGD error codes
		/// are translated into something a player can do something about.
		/// </remarks>
		public static string FaultOf(string body)
		{
			if (string.IsNullOrEmpty(body) || body.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return null;
			}

			int codeAt = body.IndexOf("<errorCode>", StringComparison.OrdinalIgnoreCase);

			string description = Between(body, "<errorDescription>", "</errorDescription>");

			if (codeAt < 0)
			{
				return description;
			}

			string code = Between(body, "<errorCode>", "</errorCode>");

			switch (code)
			{
				case "501":
					return "this router's UPnP does not do port forwarding (" + description + ")";

				case "606":
					return "the router wants a login before it will forward a port";

				case "713":
					return "the router only accepts permanent forwards, and refused the request";

				case "714":
					return null;

				case "718":
					return "another program on this machine already forwards that port";

				case "725":
					return "the router only forwards to a named machine, and could not match one";

				default:
					return description == null ? ("router error " + code) : description;
			}
		}

		private static string Between(string text, string open, string close)
		{
			int start = text.IndexOf(open, StringComparison.OrdinalIgnoreCase);

			if (start < 0)
			{
				return null;
			}

			start += open.Length;
			int end = text.IndexOf(close, start, StringComparison.OrdinalIgnoreCase);

			if (end < 0)
			{
				return null;
			}

			string value = text.Substring(start, end - start).Trim();
			return value.Length == 0 ? null : value;
		}

		private static string Text(XmlNode node, string localName)
		{
			if (node == null)
			{
				return null;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.LocalName == localName)
				{
					return child.InnerText;
				}
			}

			return null;
		}

		private static string HostOf(string url)
		{
			try
			{
				return new Uri(url).Host;
			}
			catch (UriFormatException)
			{
				return null;
			}
		}

		private static string N(int value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		// ---- transport ----

		private static string Fetch(string url, TimeSpan timeout)
		{
#pragma warning disable SYSLIB0014
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014
			request.Method = "GET";
			request.Timeout = (int)timeout.TotalMilliseconds;
			request.ReadWriteTimeout = (int)timeout.TotalMilliseconds;

			using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
			{
				return reader.ReadToEnd();
			}
		}

		private static void Send(string url, string action, string body, TimeSpan timeout, out string response)
		{
#pragma warning disable SYSLIB0014
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014
			request.Method = "POST";
			request.Timeout = (int)timeout.TotalMilliseconds;
			request.ReadWriteTimeout = (int)timeout.TotalMilliseconds;
			request.ContentType = "text/xml; charset=\"utf-8\"";
			request.Headers.Add("SOAPACTION", "\"" + MappingServices[0] + "#" + action + "\"");

			byte[] payload = Encoding.UTF8.GetBytes(body);
			request.ContentLength = payload.Length;

			using (Stream stream = request.GetRequestStream())
			{
				stream.Write(payload, 0, payload.Length);
			}

			using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
			{
				response = reader.ReadToEnd();
			}
		}

		private static string Describe(Exception e)
		{
			WebException web = e as WebException;

			if (web != null && web.Response is HttpWebResponse response)
			{
				return "the router answered " + (int)response.StatusCode;
			}

			return e.Message;
		}

		private static bool HasDefaultRoute()
		{
			try
			{
				foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (adapter.OperationalStatus != OperationalStatus.Up
						|| adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
					{
						continue;
					}

					// Through IPInterfaceProperties, and with var rather than the
					// element type named: NetworkInterface.GatewayAddresses is
					// newer than the profile Unity compiles against, and the
					// GatewayAddress type is not in it at all. Naming either
					// fails to compile in the editor while looking perfectly
					// reasonable.
					foreach (var gateway in adapter.GetIPProperties().GatewayAddresses)
					{
						if (gateway.Address != null && !gateway.Address.Equals(IPAddress.Any))
						{
							return true;
						}
					}
				}
			}
			catch (Exception)
			{
				// Cannot tell, so assume there is a route and let discovery
				// decide. A false positive costs three seconds in the
				// background; a false negative would leave a reachable host
				// looking unreachable.
			}

			return false;
		}

		private static void Post(Action<UpnpResult> action, Action<UpnpResult> onResult, UpnpResult result = default(UpnpResult))
		{
			LastResult = result;

			MainThreadDispatcher.Post(() =>
			{
				action?.Invoke(result);
				onResult?.Invoke(result);
			});
		}
	}
}
