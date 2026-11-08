using System;
using UnityEngine;

namespace OpenFrontier.IP.Multiplayer
{
	/// <summary>
	/// Drives the multiplayer update loop from Unity's frame callback.
	///
	/// Sockets read and write on background threads, so anything they hand over
	/// is queued and must be moved onto the main thread before it touches the
	/// simulation - Unity objects are not thread-safe. This component exists
	/// solely to drain that queue once per frame; without it, no inbound message
	/// would ever be processed and a session would connect and then sit silent.
	///
	/// Deliberately a scene object added at startup rather than something the
	/// host and client have to remember to create: forgetting it would look
	/// exactly like a networking failure.
	/// </summary>
	[DefaultExecutionOrder(-10000)]
	public sealed class MultiplayerPump : MonoBehaviour
	{
		/// <summary>
		/// Raised once the pump exists, for anything installed later.
		/// </summary>
		/// <remarks>
		/// The pump is created before any scene loads, so most things
		/// are installed after it and can simply find it. Something
		/// that can be created first needs this: there is no way to
		/// subscribe to an event on a component that does not exist
		/// yet, and polling for it forever is worse than being told.
		/// </remarks>
		public static event Action AddedPumpTick;

		private const string ObjectName = "MultiplayerPump";

		/// <summary>
		/// Creates the pump if it is not already present.
		/// </summary>
		///
		/// Called before the first scene loads, so the pump exists before any
		/// session does. Uses HideFlags so it does not clutter the hierarchy, and
		/// survives scene loads because it is created before any scene exists.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void EnsureExists()
		{
			// Register the main thread while we are definitely on it, so
			// IsMainThread is correct even if something queues work before the
			// first Update.
			MainThreadDispatcher.RegisterMainThread();

			if (Instance != null)
			{
				return;
			}

			GameObject pump = new GameObject(ObjectName);
			pump.hideFlags = HideFlags.HideAndDontSave;

			// The one line everything else that must outlive a menu also has,
			// and its absence was the whole reason a joined game stopped
			// receiving anything.
			//
			// Joining a host means loading their world, which is a scene
			// change. Without this the pump is destroyed with the old scene,
			// MainThreadDispatcher.Drain stops being called, and every message
			// that arrives afterwards is queued and never delivered - so the
			// join succeeds, the world loads, and the two copies then drift
			// apart silently for the rest of the session.
			DontDestroyOnLoad(pump);

			pump.AddComponent<MultiplayerPump>();
		}

		private static MultiplayerPump Instance { get; set; }

		private void Awake()
		{
			// Defensive: with Enter Play Mode Options the domain may not reload,
			// so a stale reference could survive and skip creating the pump.
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			MainThreadDispatcher.RegisterMainThread();
		}

		private void Update()
		{
			MainThreadDispatcher.Drain();
		}

		private void Start()
		{
			// Once, on the first pump: a replacement pump would announce
			// again and a listener that had already unsubscribed from the old
			// one would simply be told a second time.
			AddedPumpTick?.Invoke();
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}
	}
}