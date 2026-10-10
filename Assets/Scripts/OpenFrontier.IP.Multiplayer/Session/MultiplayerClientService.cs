using System;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Owns this copy's place in someone else's session, for as long as it lasts.
	///
	/// It exists because the join used to be owned by the multiplayer menu
	/// screen. That works right up until the player enters a game: the screen
	/// is destroyed, its client goes with it, and the copy stops hearing
	/// anything - which is precisely when there is a world to hear about. The
	/// socket would still be open on the host's side while this player sat
	/// silently in a game, looking for all the world like a network fault.
	///
	/// Installed at startup alongside the host service, and for the same
	/// reason: it has to exist whether or not multiplayer is currently on, and
	/// asking every caller to remember to create it is how it would go missing.
	/// </summary>
	public sealed class MultiplayerClientService : MonoBehaviour
	{
		private const string ObjectName = "MultiplayerClient";

		public static MultiplayerClientService Instance { get; private set; }

		/// <summary>The session this copy is in, or null.</summary>
		public ClientSession Session { get; private set; }

		/// <summary>Raised once the host has admitted us.</summary>
		public event Action Connected;

		/// <summary>Raised with the reason when a join fails or a session ends.</summary>
		public event Action<string> Failed;

		/// <summary>Where incoming unit states go.</summary>
		public event Action<UnitStateMessage> UnitStateReceived;

		/// <summary>How many unit states have arrived since play began.</summary>
		public int StatesReceived { get; private set; }

		public bool IsConnected => Session != null && Session.State == JoinState.Connected;

		/// <summary>Installs the service before any scene loads.</summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Install()
		{
			if (Instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			DontDestroyOnLoad(host);
			host.AddComponent<MultiplayerClientService>();

			// The applier goes with it rather than into a scene, for the
			// reason this whole service exists: anything parented to a scene
			// stops existing the moment a player leaves the menu, and what it
			// exists to do only happens in a game.
			host.AddComponent<UnitStateApplier>();
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				// Domain reload left the previous instance alive. The outgoing
				// one has to go, or two copies of this would both believe they
				// own the session.
				Destroy(gameObject);
				return;
			}

			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance != this)
			{
				return;
			}

			// Disconnected before the reference goes, so a listener elsewhere
			// sees the end of the session rather than a service that merely
			// stopped existing.
			Disconnect();
			Instance = null;
		}

		/// <summary>Joins a session by its code, replacing any current one.</summary>
		public void Join(string code)
		{
			Disconnect();

			ClientSession client = new ClientSession();

			client.Connected += () =>
			{
				Connected?.Invoke();
			};

			client.Failed += reason => Failed?.Invoke(reason);

			client.Disconnected += reason => Failed?.Invoke(reason);

			client.UnitStateReceived += states =>
			{
				StatesReceived++;
				UnitStateReceived?.Invoke(states);
			};

			Session = client;
			client.Join(code);
		}

		/// <summary>Leaves the session, if there is one.</summary>
		public void Disconnect()
		{
			if (Session == null)
			{
				return;
			}

			ClientSession leaving = Session;
			Session = null;

			leaving.Dispose();
		}
	}
}
