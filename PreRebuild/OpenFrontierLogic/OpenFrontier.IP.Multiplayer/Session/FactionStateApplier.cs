using System.Collections.Generic;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.Multiplayer.UI;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Puts a client into the state the host says its faction is in.
	///
	/// The other half of <see cref="FactionStatePusher"/>. Credits are simply
	/// overwritten. The hold is rebuilt from what arrived rather than
	/// adjusted, because a difference applied as a delta is a difference that
	/// compounds: a missed message leaves the client permanently out by one
	/// purchase, and no later message can tell it which one.
	///
	/// That is the whole argument for replacing rather than adding. The
	/// client is not keeping a running total it is correcting - it is
	/// holding a picture of the host's total, and the latest picture is
	/// simply true.
	/// </summary>
	public sealed class FactionStateApplier : MonoBehaviour
	{
		/// <summary>The name of the object this installs itself on.</summary>
		public const string ObjectName = "Multiplayer Faction State Applier";

		/// <summary>Installs the applier if it is not already there.</summary>
		public static void EnsureInstalled()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			DontDestroyOnLoad(host);
			host.AddComponent<FactionStateApplier>();
		}

		public static FactionStateApplier Instance { get; private set; }

		private MultiplayerClientService service;

		private MultiplayerClientService connectedTo;

		/// <summary>Messages that named a faction this copy does not have.</summary>
		/// <remarks>
		/// Counted, not warned about. Faction state can arrive before the
		/// world has finished arriving, and a client that shouted about every
		/// early message would be screaming through the entire snapshot
		/// transfer. The number is here so that "it stopped arriving" and "it
		/// arrived and was thrown away" can be told apart afterwards.
		/// </remarks>
		public int UnknownCount => unknownTotal;

		public int AppliedCount => appliedTotal;

		private int appliedTotal;
		private int unknownTotal;

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		private void OnEnable()
		{
			Attach();
			Connect();
		}

		/// <summary>
		/// Binds to the client service, which exists before this does.
		/// </summary>
		/// <remarks>
		/// Both are created together, MultiplayerClientService first, so this
		/// usually succeeds on the first try - but "usually" is not a thing to
		/// build on, so the service's Connected event retries it. That retry is
		/// what the old pump-based version was relying on and never got.
		/// </remarks>
		private void Connect()
		{
			MultiplayerClientService current = MultiplayerClientService.Instance;

			if (current == null || current == connectedTo)
			{
				return;
			}

			if (connectedTo != null)
			{
				connectedTo.Connected -= Attach;
			}

			connectedTo = current;
			connectedTo.Connected += Attach;
		}

		/// <summary>
		/// Subscribes to the service, which forwards from whichever session is
		/// current. Never to the session itself.
		/// </summary>
		/// <remarks>
		/// Subscribing to the session looks equivalent and is not: this
		/// component is added before any session exists, so it would wait for
		/// one to be woken later and, with nothing to wake it, would sit there
		/// holding a subscription to nothing. The host was broadcasting the
		/// answer the whole time.
		/// </remarks>
		private void Attach()
		{
			MultiplayerClientService current = MultiplayerClientService.Instance;

			if (current == null || current == service)
			{
				return;
			}

			if (service != null)
			{
				service.FactionStateReceived -= Apply;
			}

			service = current;
			service.FactionStateReceived += Apply;

			Debug.Log("Multiplayer: faction-state applier is listening to the host");
		}

		private void Apply(FactionStateMessage message)
		{
			Faction faction = EngineASX.Instance == null
				? null
				: EngineASX.Instance.GetFactionByid(message.FactionId);

			if (faction == null)
			{
				unknownTotal++;

				if (unknownTotal == 1 || unknownTotal % 50 == 0)
				{
					Debug.LogWarning("Multiplayer: " + unknownTotal
						+ " faction state message(s) so far have named a faction this copy"
						+ " does not have - the latest was " + message.FactionId
						+ ". Usually this means the world has not finished arriving.");
				}

				return;
			}

			faction.Credits = message.Credits;

			ApplyCargo(message);

			appliedTotal++;

			Debug.Log("Multiplayer: faction " + message.FactionId + " is at "
				+ message.Credits + " credits, ship " + message.ShipId
				+ " holding " + message.Count + " cargo stack(s)");
		}

		/// <summary>Rebuilds the named ship's hold to match what arrived.</summary>
		private static void ApplyCargo(FactionStateMessage message)
		{
			EngineASX engine = EngineASX.Instance;

			Unit ship = engine == null ? null : engine.GetUnitByid(message.ShipId);

			CargoBayComponent bay = ship == null ? null : ship.CargoBayComponent;

			if (bay == null)
			{
				return;
			}

			// Emptyed first, then refilled, rather than moved towards the
			// target. Adjusting would need the current contents to match what
			// the host thought they were, and when they do not - a snapshot
			// that arrived half-built, a ship that was rebuilt rather than
			// synced - the adjustment keeps the difference instead of
			// clearing it.
			List<KeyValuePair<CargoClass, int>> held = new List<KeyValuePair<CargoClass, int>>();

			foreach (KeyValuePair<CargoClass, int> entry in bay.Cargos)
			{
				if (entry.Key != null && entry.Value > 0)
				{
					held.Add(entry);
				}
			}

			for (int i = 0; i < held.Count; i++)
			{
				bay.AddToCargo(held[i].Key, -held[i].Value, ignoreCapacity: true);
			}

			for (int i = 0; i < message.Count; i++)
			{
				CargoStack stack = message.Cargo[i];

				if (stack.Quantity <= 0)
				{
					continue;
				}

				CargoClass cargoClass = engine.GetCargoClassById(stack.ClassId);

				if (cargoClass == null)
				{
					// Said once per arrival rather than per stack: a missing
					// class means this client does not have that content, and
					// saying so once is enough to know it.
					Debug.LogWarning("Multiplayer: this copy has no cargo class "
						+ stack.ClassId + ", so the host's hold cannot be fully shown.");
					continue;
				}

				bay.AddToCargo(cargoClass, stack.Quantity, ignoreCapacity: true);
			}
		}
	}
}