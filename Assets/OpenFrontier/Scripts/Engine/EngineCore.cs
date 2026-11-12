using UnityEngine;

namespace OpenFrontier.Engine
{
	/// <summary>
	/// The game's one heartbeat: a persistent MonoBehaviour singleton spawned
	/// AT BOOT through the LandingBoot seam ("grow this block as boot-time
	/// services arrive") and kept alive with DontDestroyOnLoad. The menu is
	/// intended to become an active engine world (pinned decision), so the
	/// engine exists from the first moment anything could need it instead of
	/// being spawned lazily on first launch. Boot() is idempotent, so a menu
	/// opened directly in the editor (no landing scene ran) can still start a
	/// gamemode — and the Main Menu scene stays standalone-playable regardless.
	///
	/// Deliberately different from the legacy engine: decompiled EngineASX was
	/// a per-world scene object with no DontDestroyOnLoad — every Single-mode
	/// scene load rebuilt it, and only its statics survived scene boundaries.
	/// EngineCore is ONE instance for the whole process; per-world state
	/// belongs in world payloads and registries, not in this object's
	/// lifetime.
	///
	/// EngineCore OWNS the engine's five services and exposes them for
	/// registration: ParallelismPolicy (the one parallelism knob, pushed into
	/// the job worker count at boot), EventBus (typed struct events — the
	/// WorldLoaded publish rides it), SimStateBuffer (the sim/render
	/// decoupling: the last two sim snapshots), RenderSync (the ONLY gameplay
	/// transform writer, driven per render frame from Update) and the
	/// WorldResolver seam (unloaded systems are RESOLVED, never ticked).
	/// World state lives in payloads and registries; these five are the
	/// process-lifetime machinery every world shares.
	///
	/// Its FixedUpdate drives every tier of the tick machine. Unity does not
	/// call FixedUpdate at timeScale 0, so the load/pause protocol (0 =
	/// loading/paused, 1 = ready) stops the machine natively. EngineCore is
	/// also the protocol's SINGLE writer: the legacy game had four unrelated
	/// timeScale writers (loader, loading screen, world init, ready
	/// notification) and relied on ordering luck; every M1 write goes through
	/// here.
	/// </summary>
	public class EngineCore : MonoBehaviour
	{
		private static EngineCore instance;

		/// <summary>The process-wide instance, or null before Boot().</summary>
		public static EngineCore Instance => instance;

		/// <summary>
		/// True once the current world has finished its boot chain (payload
		/// adopted, boot proven) and the machine is running at timeScale 1.
		/// </summary>
		public static bool LoadedAndReady { get; private set; }

		private TickScheduler scheduler;
		private EventBus events;
		private SimStateBuffer simState;
		private RenderSync render;
		private WorldResolver resolver;
		private ParallelismPolicy parallelism;

		/// <summary>The tick machine's driver.</summary>
		public TickScheduler Scheduler => scheduler;

		/// <summary>Typed engine events. Subscribe before a world's boot
		/// chain completes to catch its WorldLoadedEvent (published once per
		/// world, from NotifyWorldLoadedAndReady).</summary>
		public EventBus Events => events;

		/// <summary>The sim/render decoupling buffer: sim writes at the end
		/// of each tick, render reads between ticks. The last TWO states are
		/// kept so interpolation can be added (an M3 decision).</summary>
		public SimStateBuffer SimState => simState;

		/// <summary>The only gameplay transform writer. ISimRenderables
		/// register here; nothing else may write gameplay transforms.</summary>
		public RenderSync Render => render;

		/// <summary>The unloaded-world resolver seam: systems are RESOLVED,
		/// never ticked — there is NO SystemSlow tier.</summary>
		public WorldResolver Resolver => resolver;

		/// <summary>The one parallelism knob every parallel call site
		/// consults — no #if forks at call sites.</summary>
		public ParallelismPolicy Parallelism => parallelism;

		/// <summary>
		/// Spawn the engine heartbeat. Idempotent: safe from the landing
		/// boot, and defensively from any launch path that runs without it.
		/// </summary>
		public static EngineCore Boot()
		{
			if (instance != null)
			{
				return instance;
			}

			GameObject host = new GameObject("EngineCore");
			instance = host.AddComponent<EngineCore>();
			return instance;
		}

		/// <summary>
		/// The world boot chain finished: the payload was adopted and the
		/// machine is proven running. Lifts the load/pause protocol to 1 and
		/// publishes WorldLoadedEvent. Single authority for the write —
		/// nothing else sets timeScale for load/pause in M1.
		/// </summary>
		public static void NotifyWorldLoadedAndReady()
		{
			Time.timeScale = 1f;
			LoadedAndReady = true;

			if (instance == null)
			{
				// Unreachable through the launch chain (WorldBoot Boots
				// first); loud because a ready world with no engine is a
				// boot-order bug, not a warning-worthy oddity.
				Debug.LogError("[EngineCore] Ready signalled before Boot() — WorldLoadedEvent not published.");
				return;
			}

			instance.events.Publish(new WorldLoadedEvent());
		}

		/// <summary>
		/// A scene load begins (launch, quit-to-menu): the protocol drops to
		/// 0 so the machine stops natively behind the loading screen.
		/// </summary>
		public static void NotifyLoadStarted()
		{
			LoadedAndReady = false;
			Time.timeScale = 0f;
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(gameObject);
				return;
			}

			instance = this;
			DontDestroyOnLoad(gameObject);
			scheduler = new TickScheduler();
			events = new EventBus();
			simState = new SimStateBuffer();
			render = new RenderSync(simState);
			resolver = new WorldResolver();
			parallelism = new ParallelismPolicy();

			// Push the boot-time budget into Unity's job workers. Re-Apply
			// on change — under thermal pressure this is the ONE knob that
			// shrinks; no call site ever forks on platform.
			parallelism.Apply();
		}

		private void Update()
		{
			// RenderSync runs at RENDER cadence (per render frame), never in
			// FixedUpdate — the machine must not depend on frame rate and
			// render must not write sim state. At timeScale 0 this keeps
			// drawing the last snapshot while the sim is stopped natively
			// (no FixedUpdate) — exactly the loading/paused shape.
			render.RenderUpdate();
		}

		private void OnDestroy()
		{
			if (instance == this)
			{
				instance = null;
				LoadedAndReady = false;
			}
		}

		private void FixedUpdate()
		{
			// No pause gate wanted here: Unity does not call FixedUpdate at
			// timeScale 0, so the protocol stops the machine with no per-tick
			// check. (The legacy source kept a redundant gate whose comment
			// claimed code "runs while paused" — unreachable, and a trap this
			// file deliberately does not reproduce.)
			scheduler.Tick(Time.fixedDeltaTime);

			// End-of-tick sim write — the M1 form of the sim/render
			// decoupling: the pinned player's snapshot enters the buffer so
			// render has real state to read. The pinned player sits at the
			// origin forever, so M1's content is the tick stamping itself;
			// when entities arrive (M6) the writer moves into the world's
			// Housekeeping systems and the two-slot contract does not change.
			simState.Write(new SimSnapshot
			{
				TickIndex = scheduler.TickIndex,
				SimTime = scheduler.TickIndex * (double)Time.fixedDeltaTime,
				Player = SimBody.PinnedPlayer,
			});
		}
	}
}
