using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.Hypersleep;
using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// Decides what the music should be, and asks for it.
	/// </summary>
	/// <remarks>
	/// The state machine is the single source of truth. Every world change that
	/// ought to be audible goes through <see cref="Decide"/>, and the music
	/// follows whatever it says now - not whatever it said when the track
	/// started. So the music answers to the world rather than to its own
	/// history: faction control changes, a hostile appears, you get targeted,
	/// you dock, you jump, and each of those is a different answer.
	///
	/// Priority, highest first:
	///
	///   MainMenu    the scenario is the menu
	///   Combat       any hostile engaging you - overrides everything in game
	///   Docked       your ship is in a hangar - overrides space
	///   Hypersleep   the superluminal drive
	///   Space        whatever family owns this sector
	///
	/// Two ways a track changes, and the difference is the whole design:
	///
	///   The world changed under it. The outgoing track is interrupted - faded
	///   to silence over a set duration, then the new one starts. Nothing
	///   overlaps, because these pieces were composed to be heard alone.
	///
	///   The track ran out naturally. At its measured ending onset the next
	///   track is handed over and the outgoing one plays its own composed
	///   ending underneath it. Nothing is cut.
	///
	/// Combat is asked the question slightly differently, because a firefight
	/// lasts longer than a track: the sequencer owns which section is next,
	/// and a track ending mid-fight asks the sequencer rather than the random
	/// picker.
	/// </remarks>
	public sealed class MusicDirector : MonoBehaviour
	{
		public const string ObjectName = "Music Director";

		private static MusicDirector instance;

		public static MusicDirector Instance => instance;

		/// <summary>
		/// Installs the director and stands the old music controller down.
		/// </summary>
		/// <remarks>
		/// Created at startup rather than added to the EngineASX prefab, because
		/// a hand-edited prefab is the thing that fails silently and there is
		/// nothing here that needs an inspector field. Hidden and made not to
		/// be saved, so it survives a scene change without appearing in a
		/// hierarchy anyone looks at.
		///
		/// The old EngineMusicPlayerController is disabled rather than removed.
		/// It plays the eleven old tracks from Resources and would otherwise
		/// talk over the top of this one - two systems deciding what the music
		/// is, which is the exact failure this design exists to avoid.
		///
		/// Its SimpleMusicPlayer is left in place and still read for volume, so
		/// the options screen keeps working with no change to it and no second
		/// volume setting to disagree with the first.
		/// </remarks>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Install()
		{
			if (instance != null)
			{
				return;
			}

			GameObject host = new GameObject(ObjectName);
			host.hideFlags = HideFlags.HideAndDontSave;
			Object.DontDestroyOnLoad(host);
			host.AddComponent<MusicDirector>();

			// Scene events are subscribed in Awake, not here: Install is
			// static and cannot bind an instance handler (CS0120).
			//
			// The old controller is stood down from Update rather than from
			// here, because it lives on the EngineASX prefab and that does
			// not exist yet at BeforeSceneLoad. Looking now finds nothing,
			// says nothing, and leaves two systems playing music over each
			// other for the whole session.
		}

		/// <summary>
		/// Keeps at least one AudioListener alive, so the music is never
		/// silently inaudible.
		/// </summary>
		/// <remarks>
		/// THE CAUSE OF A SECOND-WORTH OF SILENCE ON EVERY LOAD. This project
		/// has exactly two AudioListeners in it, on MenuCamera and GameCamera,
		/// and NEITHER is referenced by LoadingScene.unity - so for the whole
		/// of the load there is no listener at all, and an AudioSource with no
		/// listener in the scene plays into nothing. The music was not being
		/// stopped or faded; it was playing perfectly into a scene with no
		/// ears in it, which is indistinguishable from a dropout by ear.
		///
		/// Unity has no "keep the last listener" behaviour, so this supplies
		/// one - but ONLY while there is nothing else. The moment any other
		/// live listener exists this one is switched off again, so there are
		/// never two. Two listeners is not a quiet version of one: Unity
		/// warns about it and the audio ends up somewhere arbitrary.
		///
		/// Re-checked on every scene load and unload, so the gap is covered
		/// from the frame the camera goes rather than up to a poll later, and
		/// on a slow poll after that as a backstop for listeners destroyed by
		/// something that is not a scene event.
		/// </remarks>
		private void EnsureMusicStaysAudible()
		{
			if (Time.realtimeSinceStartup < nextListenerCheck)
			{
				return;
			}

			nextListenerCheck = Time.realtimeSinceStartup + 0.5f;

			AudioListener[] all = Object.FindObjectsOfType<AudioListener>(true);

			bool anotherLive = false;

			for (int i = 0; i < all.Length; i++)
			{
				if (all[i] == null || all[i] == fallbackListener)
				{
					continue;
				}

				if (all[i].enabled && all[i].gameObject.activeInHierarchy)
				{
					anotherLive = true;
					break;
				}
			}

			if (anotherLive)
			{
				if (fallbackListener != null && fallbackListener.enabled)
				{
					fallbackListener.enabled = false;
				}

				return;
			}

			if (fallbackListener == null)
			{
				fallbackListener = gameObject.AddComponent<AudioListener>();
			}

			if (!fallbackListener.enabled)
			{
				fallbackListener.enabled = true;

				Debug.Log("Music: nothing in this scene can hear anything, so"
					+ " the music has supplied its own listener until one"
					+ " exists again.");
			}
		}

		// Two handlers rather than one shared, because the two events do NOT
		// have the same signature here: sceneLoaded passes (Scene, LoadSceneMode)
		// and sceneUnloaded passes (Scene). Writing one handler for both
		// produced two CS0123s that read like the event was mistyped.
		private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
			UnityEngine.SceneManagement.LoadSceneMode mode)
		{
			// Re-check on the next frame rather than now: at sceneLoaded the
			// camera that brings the listener may not have run Awake yet.
			nextListenerCheck = 0f;
		}

		private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
		{
			// This is the one that matters. The camera carrying the listener
			// has just been destroyed, so this is the frame the scene stopped
			// being able to hear anything.
			nextListenerCheck = 0f;
		}

		private AudioListener fallbackListener;
		private float nextListenerCheck;

		private float nextStandDownCheck;
		private bool warnedAboutOldMusic;

		/// <summary>
		/// Disables the previous music controller, every time a new one turns
		/// up, and stops whatever it left playing.
		/// </summary>
		/// <remarks>
		/// THIS WAS A ONE-SHOT AND IT WAS WRONG. It disabled the first
		/// EngineMusicPlayerController it found and set a flag so it would
		/// never look again - fine exactly once per session, and wrong from
		/// the first sector load onwards. That controller lives on the EngineASX
		/// prefab, and every world or sector load instantiates a fresh one. The
		/// fresh copy arrived enabled, started playing the old sector music,
		/// and was never disabled, because the flag said the job was done.
		///
		/// The visible result was a correct menu and two pieces of music in
		/// every sector: this system's track and the old scenario music, from
		/// two live controllers that both believed they owned the music.
		///
		/// Disabling a controller does NOT stop what it already started.
		/// SimpleMusicPlayer is a separate component that keeps playing and
		/// keeps its queue, and the only code that would ever have cleared
		/// that is inside the controller's own Update - the code just
		/// disabled. So the queue is cleared and the track stopped too.
		///
		/// Checked once a second rather than every frame: a new controller can
		/// only appear on a world load, and FindObjectsOfType every frame for
		/// something that happens every few minutes is waste.
		/// </remarks>
		private void StandDownTheOldController()
		{
			if (Time.realtimeSinceStartup < nextStandDownCheck)
			{
				return;
			}

			nextStandDownCheck = Time.realtimeSinceStartup + 1f;

			EngineMusicPlayerController[] found =
				Object.FindObjectsOfType<EngineMusicPlayerController>();

			bool disabledAny = false;

			for (int i = 0; i < found.Length; i++)
			{
				if (found[i] != null && found[i].enabled)
				{
					found[i].enabled = false;
					disabledAny = true;
				}
			}

			StopLeftoverOldMusic();

			if (disabledAny && !warnedAboutOldMusic)
			{
				warnedAboutOldMusic = true;

				Debug.Log("Music: the previous music controller has been stood"
					+ " down, and anything it had playing has been stopped."
					+ " It lives on the EngineASX prefab, so a new one appears on"
					+ " every world and sector load and has to be stood down"
					+ " again each time. Its SimpleMusicPlayer is still read for"
					+ " volume, so the options screen is unchanged.");
			}
		}

		/// <summary>
		/// Clears the old player's queue and stops its track.
		/// </summary>
		/// <remarks>
		/// Only touches the player when it actually has something on it, so
		/// this is a no-op in normal operation and cannot fight anything.
		/// </remarks>
		private void StopLeftoverOldMusic()
		{
			if (GameController.Instance == null || GameController.Instance.MusicPlayer == null)
			{
				return;
			}

			OpenFrontier.IP.MusicPlayer.SimpleMusicPlayer old =
				GameController.Instance.MusicPlayer;

			if (old.QueuedTracks != null && old.QueuedTracks.Count > 0)
			{
				old.QueuedTracks.Clear();
			}

			if (old.PlayingTrack != null)
			{
				old.StopPlayingTrack();
			}
		}

		private void Awake()
		{
			instance = this;

			// Scene events so the fallback listener goes in the frame the
			// camera leaves, rather than up to half a second later.
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
			UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
		}

		private void OnDestroy()
		{
			if (instance == this)
			{
				instance = null;
			}
		}

		private MusicCrossfadePlayer player;
		private readonly MusicCombatDetector detector = new MusicCombatDetector();
		private readonly MusicCombatSequencer sequencer = new MusicCombatSequencer();

		private string currentKey = "none";
		// The dock is held as a STATE, not as an edge.
		//
		// It used to be an edge - dockedChanged, true for exactly one frame,
		// consumed by the transition block - and that is wrong in two ways
		// that both showed up as "the station crossfade is broken".
		//
		// One frame is not long enough. If Crossfade is refused on that frame
		// because a handover is already pending, the next frame sees false,
		// and the dock is handled as a plain interrupt. The crossfade never
		// happens at all.
		//
		// More seriously, combat outranks docked in Decide. Dock while being
		// shot at on the way in - which is most arrivals - and Decide keeps
		// answering "Combat/...", so no transition is even attempted. Thirty
		// seconds later the shooting stops, the transition finally happens,
		// and the one-frame flag expired long ago. Arriving at a station
		// under fire was therefore never crossfaded.
		//
		// Holding the dock state and comparing it against the dock state at
		// the last transition that SUCCEEDED makes the request persistent: it
		// survives a refused crossfade, and it survives combat in between,
		// and it is answered the moment it can be.
		private bool dockedNow;
		private bool dockAtLastTransition;

		/// <summary>How long a wormhole transit may claim to be running.</summary>
		/// <remarks>
		/// Generous - a transit is seconds - and only a backstop for a flag
		/// owned by another class. See the backstop in Update.
		/// </remarks>
		private const float WormholeStaleSeconds = 45f;

		private float wormholeSince = -1f;
		private MusicTimingTable.Entry lastRequested;
		private MusicCombatState lastCombat;

		/// <summary>The crossfader this drives.</summary>
		public MusicCrossfadePlayer Player
		{
			get
			{
				if (player == null)
				{
					player = GetComponent<MusicCrossfadePlayer>();

					if (player == null)
					{
						player = gameObject.AddComponent<MusicCrossfadePlayer>();
					}
				}

				return player;
			}
		}

		/// <summary>
		/// Everything that could be making a sound, in one string.
		/// </summary>
		/// <remarks>
		/// Added because "two music systems are playing at once" has been
		/// wrong twice for different reasons, and both times the only way to
		/// tell them apart was to already know the answer. This names every
		/// voice in the game that can play music and says what each one is
		/// doing: the old controller's enabled count, the old player's track
		/// and queue, and BOTH of this system's audio sources - the second
		/// one matters because during a handover or a crossfade it is
		/// supposed to be playing, and that is indistinguishable from a bug
		/// by ear.
		///
		/// CACHED, AND THAT IS THE FIX RATHER THAN AN OPTIMISATION.
		///
		/// This used to run FindObjectsOfType, which walks every GameObject in
		/// the scene and allocates an array to return. The overlay calls it
		/// from OnGUI, and OnGUI runs at least twice per frame - once for the
		/// Layout event, once for Repaint - so this cost two full scene scans
		/// per frame, plus one more for the listener count. Four scans and
		/// four array allocations, every frame, on a project whose scenes are
		/// enormous.
		///
		/// It showed up as "moving the camera halves the FPS", which misleads
		/// in a specific way: these are CPU costs competing with rendering,
		/// so they only become visible once there IS rendering to compete
		/// with. Standing still looked fine. The allocations also mean a
		/// periodic garbage collection, which is the "sometimes" in
		/// "sometimes the FPS is cut in half".
		/// </remarks>
		public string VoiceReport() => cachedVoiceReport;

		private const float VoiceReportInterval = 0.25f;

		private string cachedVoiceReport = "not sampled yet";
		private float nextVoiceReportAt;

		/// <summary>AudioListeners enabled and in an active hierarchy.</summary>
		private static int LiveListenerCount()
		{
			AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>();
			int count = 0;

			for (int i = 0; i < listeners.Length; i++)
			{
				if (listeners[i] != null && listeners[i].enabled
					&& listeners[i].gameObject.activeInHierarchy)
				{
					count++;
				}
			}

			return count;
		}

		private void RefreshVoiceReport()
		{
			if (Time.realtimeSinceStartup < nextVoiceReportAt)
			{
				return;
			}

			nextVoiceReportAt = Time.realtimeSinceStartup + VoiceReportInterval;

			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			EngineMusicPlayerController[] controllers =
				Object.FindObjectsOfType<EngineMusicPlayerController>();
			int enabled = 0;

			for (int i = 0; i < controllers.Length; i++)
			{
				if (controllers[i] != null && controllers[i].enabled)
				{
					enabled++;
				}
			}

			sb.Append("old ctrls ").Append(controllers.Length)
				.Append(" (").Append(enabled).Append(" enabled)");

			if (GameController.Instance != null && GameController.Instance.MusicPlayer != null)
			{
				OpenFrontier.IP.MusicPlayer.SimpleMusicPlayer old =
					GameController.Instance.MusicPlayer;

				sb.Append(", old player ");

				if (old.PlayingTrack != null && old.PlayingTrack.AudioSource != null)
				{
					sb.Append("'").Append(old.PlayingTrack.Track.TrackName)
						.Append("' ").Append(old.PlayingTrack.AudioSource.isPlaying
							? "PLAYING" : "silent");
				}
				else
				{
					sb.Append("nothing");
				}

				sb.Append(", queued ").Append(old.QueuedTracks.Count);
			}
			else
			{
				sb.Append(", no old player");
			}

			sb.Append(" | mine ").Append(Player.SourceReport())
				.Append(", listeners ").Append(LiveListenerCount());

			cachedVoiceReport = sb.ToString();
		}

		/// <summary>The answer the state machine is currently giving.</summary>
		public string CurrentKey => currentKey;

		/// <summary>
		/// The last combat reading, for the diagnostic overlay.
		/// </summary>
		/// <remarks>
		/// Reads through the detector rather than a copy, so the overlay shows
		/// the same number the state machine acted on rather than a second
		/// measurement taken a frame later and possibly disagreeing with it.
		/// </remarks>
		public MusicCombatState CombatState => detector.Sample();

		/// <summary>Which family owns the sector you are in, for the overlay.</summary>
		public MusicFamily SectorFamily =>
			EngineASX.Instance == null || EngineASX.Instance.ActiveSector == null
				? MusicFamily.Neutral
				: MusicFactionClassifier.FamilyOf(
					EngineASX.Instance.ActiveSector.ControllingFaction,
					EngineASX.Instance.LocalFaction);

		/// <summary>
		/// The player has committed to loading a scenario.
		/// </summary>
		/// <remarks>
		/// Called from ScenarioLoaderUI.TryLoadScenario, at the moment the
		/// load is queued. That is the earliest point at which the music can
		/// know the menu is over - and getting it EARLIER than this is not
		/// possible, while getting it later is what went wrong twice.
		///
		/// The first attempt watched for EngineASX.LoadedAndReady going
		/// false. It never once fired: the log contains ZERO "Engine:
		/// OnDestroy" lines, because the engine is not destroyed when you
		/// leave the menu - the same instance carries the menu world and then
		/// the galaxy one. LoadedAndReady stays true throughout, so the hook
		/// was unreachable code that looked entirely plausible.
		///
		/// Watched for a scene change instead? No - the menu changes scene
		/// constantly while you navigate it, and that would have cut the
		/// music off every time you opened a submenu.
		///
		/// A static, called by the thing that knows, is the honest version of
		/// this hook. Same assembly, so there is no reference to add.
		/// </remarks>
		public static void ScenarioLoadCommitted()
		{
			if (instance != null && instance.currentKey == "Menu")
			{
				instance.Player.FadeOutOnly(instance.Player.MenuFadeSeconds);
			}
		}

		private void Update()
		{
			StandDownTheOldController();
			EnsureMusicStaysAudible();

			// DIAGNOSTIC ONLY, AND DIAGNOSTIC WORK DOES NOT SHIP.
			//
			// cachedVoiceReport is read by one thing: MusicDebugOverlay, which
			// is compiled out of release builds entirely. Two FindObjectsOfType
			// scans every quarter second to fill a cache nobody can read is
			// pure cost on the platform this game is designed for.
			//
			// Compiled out rather than guarded at runtime, so a release build
			// does not even contain it.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			RefreshVoiceReport();
#endif

			EngineASX engine = EngineASX.Instance;

			// INSIDE A WORMHOLE: let go of everything.
			//
			// The old music used to play straight through the transit and stop
			// dead on the far side, which is the one place a hard stop is
			// plainly wrong: the ship is somewhere that has no music, and
			// arriving somewhere should not begin with the previous place
			// being cut off.
			//
			// Deliberately ABOVE the engine check and above Decide. Deciding
			// requires a world, and during a transit there is no world to
			// decide about - this is the same lesson as the main menu, where
			// the fade could not start because it was waiting for the next
			// world to become observable.
			//
			// Deciding is skipped entirely rather than returning a Wormhole
			// state, because there is nothing to play here. currentKey is left
			// alone on purpose: when the transit ends, the new sector's key
			// differs from it, nothing is playing, and the new track starts.
			// If the far side is the same family the key matches instead and
			// the "same answer, nothing playing" branch starts it. Both paths
			// work.
			//
			// One property read per frame. No scene walk - this is a phone.
			if (WormholeAnimator.Active)
			{
				// BACKSTOP. The flag is cleared by the animator finishing and
				// by OnDisable, but those are two code paths owned by another
				// class, and the failure if both are ever wrong is the music
				// never coming back - a far worse bug than the missing fade
				// this exists to fix. A transit that somehow lasts
				// implausibly long is therefore treated as over.
				if (wormholeSince < 0f)
				{
					wormholeSince = Time.realtimeSinceStartup;
				}
				else if (Time.realtimeSinceStartup - wormholeSince > WormholeStaleSeconds)
				{
					Debug.LogWarning("Music: a wormhole has reported itself"
						+ " active for " + WormholeStaleSeconds + "s, which no"
						+ " transit takes. Clearing it so the music comes back."
						+ " The animation's own flag-clearing has missed"
						+ " something and is worth a look.");

					WormholeAnimator.ClearActive();
					wormholeSince = -1f;

					return;
				}

				Player.FadeOutOnly(Player.WormholeFadeSeconds);

				return;
			}

			wormholeSince = -1f;

			if (engine == null || !EngineASX.LoadedAndReady)
			{
				// No music decision to make without a world. The menu used to
				// fade out here, which was wrong twice over: the branch is
				// effectively unreachable because the engine persists across
				// the transition, and ScenarioLoadCommitted is the real seam.
				return;
			}

			string wantedKey;
			MusicTimingTable.Entry wanted;

			Decide(out wantedKey, out wanted);

			if (wanted == null)
			{
				return;
			}

			// Through the property, not the field. The crossfader is made on
			// demand by Player, and reading the field directly left it null
			// here - a NullReferenceException every single frame, which is
			// what the first run of this actually did.
			MusicCrossfadePlayer crossfader = Player;

			// The world changed.
			if (wantedKey != currentKey)
			{
				// THE STATE CHANGED BUT THE TRACK DID NOT NEED TO.
				//
				// A wormhole flaps the state - the sector changes, the local
				// ship goes away, combat ends - and the answer can come back
				// as the track already playing. Passing that to Interrupt
				// meant fading the music to silence and starting it again
				// from its first sample: a hard ending, which is exactly
				// what a wormhole sounded like.
				//
				// The player now refuses it, so the state is simply accepted
				// here with no audio operation at all. If this did NOT
				// acknowledge the key it would ask again next frame, be
				// refused again, and never settle.
				if (crossfader.Current == wanted)
				{
					currentKey = wantedKey;
					dockAtLastTransition = dockedNow;
					return;
				}

				// IS THIS WORTH CUTTING OFF A TRACK FOR?
				//
				// Only some changes are. Something is shooting at you, you
				// docked, you went through a wormhole, the menu is involved -
				// those interrupt, because something has actually happened and
				// the music is part of it.
				//
				// The ENEMY COUNT is not one of those, and treating it as one
				// is what made the music aggressive. Bandits cross sensor
				// range constantly; the count goes over the threshold and back
				// under it several times in a minute, and every crossing was
				// an instant cut to a different theme and then back. A track
				// that is cut short is worse than a track that is briefly wrong
				// about how many ships are around.
				//
				// So a count-driven change is NOT taken now. It is simply left
				// as the answer, and picked up at the next natural handover -
				// which is the point where the track changes anyway, so the
				// music still responds, just without a cut to do it. It also
				// cancels itself: if the count goes back down before that
				// handover, the answer is the one for calm again and nothing
				// happens at all.
				bool isEngagement = wantedKey.StartsWith("Combat/Engagement",
					System.StringComparison.Ordinal)
					|| currentKey.StartsWith("Combat/Engagement",
						System.StringComparison.Ordinal);

				// A change of CATEGORY, not merely of key. Two intensities of
				// one category are the same mood with a different number
				// attached, and cutting the music to say so is what produced
				// the log's fourteen restarts: Medium, High, Medium, each one
				// a hard cut and each one restarting the section from zero.
				//
				// The old test asked "is EITHER side Engagement", which is
				// true of Engagement-Medium to Engagement-High - the same
				// fight, a few ships nearer - so every intensity wobble
				// became an interruption. The intent was engagement; the
				// test was simply the wrong one.
				bool isCategoryChange = CategoryOf(wantedKey) != CategoryOf(currentKey);

				// BOTH sides have to be places. Comparing them with
				// != treated every space-to-combat change as leaving a
				// place, because a combat key is not one - which made the
				// count crossing the threshold urgent again and undid the
				// deferral it was supposed to have.
				//
				// Leaving a starport for open space is leaving a place.
				// Leaving a starport for threat music is the same sector
				// with more ships in it.
				bool isPlaceChange = IsPlace(wantedKey) && IsPlace(currentKey)
					&& wantedKey != currentKey;

				// URGENT means something happened and the music is part of it:
				//
				//   a change of place, a dock, the menu
				//   a change of combat CATEGORY - threat becomes engagement, or
				//     an engagement ends and the threat theme comes back
				//
				// NOT urgent:
				//
				//   the count crossing the threshold either way: a mood, and
				//     it waits for the next track
				//   an intensity change within a category: the same mood with
				//     a bigger number on it
				bool urgent = isPlaceChange
					|| dockedNow != dockAtLastTransition
					|| currentKey == "Menu" || wantedKey == "Menu"
					|| (isCategoryChange && isEngagement);

				if (!urgent)
				{
					// Fall through to the handover below, which will arrive at
					// `wanted` instead of the next track in the same place.
					// currentKey is NOT updated, so the difference is still
					// there to be noticed next frame.
				}
				else if (crossfader.Current == null)
				{
					if (crossfader.Start(wanted))
					{
						currentKey = wantedKey;
						lastRequested = wanted;
						dockAtLastTransition = dockedNow;
					}
				}
				else
				{
					// How the world changed decides how the music moves.
					//
					// Leaving the main menu fades all the way to silence
					// first, and takes longer doing it. It is the one place a
					// real gap is right: the menu is its own place, and
					// arriving in the galaxy should sound like arriving
					// rather than like one room bleeding into the next. A
					// crossfade here would put both at once and neither would
					// sound like a place.
					//
					// Tested on EITHER side, because the menu boundary is one
					// boundary. Coming back out of the galaxy is the same
					// moment as going into it, and the same piece of music is
					// being let go of.
					//
					// Docking crossfades - somewhere you have arrived, not
					// something that interrupted you.
					//
					// Everything else is cut, sized for the suddenness of the
					// thing that caused it.
					bool moved;

					if (currentKey == "Menu" || wantedKey == "Menu")
					{
						moved = crossfader.Interrupt(wanted, crossfader.MenuFadeSeconds);
					}
					else if (dockedNow != dockAtLastTransition)
					{
						moved = crossfader.Crossfade(wanted);
					}
					else
					{
						moved = crossfader.Interrupt(wanted);
					}

					if (moved)
					{
						currentKey = wantedKey;
						lastRequested = wanted;

						// Latched ONLY on success. A refused crossfade leaves
						// this false, so the next frame asks again instead of
						// quietly giving up and cutting instead.
						dockAtLastTransition = dockedNow;
					}

					return;
				}
			}

			// Same answer, nothing playing. A track that failed to load, or a
			// session that started silent.
			if (crossfader.Current == null)
			{
				if (crossfader.Start(wanted))
				{
					lastRequested = wanted;
				}

				return;
			}

			// The track reached its ending. Hand over, and let the old one
			// finish under it.
			//
			// If a DEFERRED change is waiting, this is where it lands: the
			// track changes anyway, so the music has responded to the new
			// state without anything being cut off to make room for it. When
			// the state is unchanged the handover stays inside the current
			// place or mood.
			if (crossfader.ReachedEndingOnset && !crossfader.IsFadingOut)
			{
				MusicTimingTable.Entry next = wantedKey == currentKey
					? NextFor(wantedKey, wanted)
					: wanted;

				if (next == null)
				{
					next = NextFor(currentKey, wanted);
				}

				if (next != null && crossfader.HandOver(next))
				{
					lastRequested = next;
				}
			}
		}

		/// <summary>
		/// The next track when one ends naturally and the world has not moved.
		/// </summary>
		/// <remarks>
		/// For combat, the sequencer's next section - a firefight runs longer
		/// than any one section, and shuffling ambient tracks in the middle of
		/// one would throw away the sequencing the sections exist for.
		///
		/// For everything else, another random track from the same place. The
		/// previous one is excluded so a track cannot hand straight over to
		/// itself, which would sound like a skip rather than a change.
		/// </remarks>
		/// <summary>
		/// Whether a key names a PLACE - somewhere the player is - rather than
		/// a mood the sector happens to be in.
		/// </summary>
		/// <remarks>
		/// Places interrupt. Moods wait for the next track.
		///
		/// Going from a starport to open space is somewhere you have left;
		/// going from calm to threat music is the same sector with a
		/// different number of ships in it.
		/// </remarks>
		/// <summary>
		/// The mood a key names: a place, a combat category, hypersleep, menu.
		/// </summary>
		/// <remarks>
		/// Deliberately coarse. The intensity is not part of it, because Low
		/// and High are the same mood with a different number of ships
		/// attached, and treating them as different categories is what turned
		/// every wobble in the count into a cut.
		/// </remarks>
		private static string CategoryOf(string key)
		{
			if (key.StartsWith("Combat/Engagement", System.StringComparison.Ordinal))
			{
				return "Engagement";
			}

			if (key.StartsWith("Combat/Threat", System.StringComparison.Ordinal))
			{
				return "Threat";
			}

			if (IsPlace(key))
			{
				return "Place";
			}

			return key;
		}

		private static bool IsPlace(string key)
		{
			return key.StartsWith("Space/", System.StringComparison.Ordinal)
				|| key.StartsWith("Starport/", System.StringComparison.Ordinal);
		}

		private MusicTimingTable.Entry NextFor(string key, MusicTimingTable.Entry current)
		{
			if (key.StartsWith("Combat/", System.StringComparison.Ordinal))
			{
				// A fight has ended and the threat music is being allowed to
				// play out. When it reaches its own ending, the ordinary music
				// goes back in - not another threat section, which would be
				// announcing a threat that is no longer there.
				//
				// This is the only thing that consumes the linger, and it is
				// what the linger is waiting for.
				if (detector.AllClearPending)
				{
					detector.ConsumeAllClear();

					MusicTimingTable.Entry ordinary = OrdinaryEntry();

					return ordinary ?? current;
				}

				MusicTimingTable.Entry next = sequencer.Advance();
				return next ?? current;
			}

			return MusicLibrary.RandomFrom(current.Category, current) ?? current;
		}

		/// <summary>
		/// The track that would be playing if the threat logic were not
		/// involved.
		/// </summary>
		/// <remarks>
		/// Only asked for while handing over out of a threat track, which
		/// means never docked - docking resets the linger, so by the time this
		/// is called there is no station state to honour.
		/// </remarks>
		private MusicTimingTable.Entry OrdinaryEntry()
		{
			EngineASX engine = EngineASX.Instance;

			if (engine == null || engine.ActiveSector == null)
			{
				return null;
			}

			MusicFamily family = MusicFactionClassifier.FamilyOf(
				engine.ActiveSector.ControllingFaction, engine.LocalFaction);

			return MusicLibrary.RandomFrom(
				MusicFactionClassifier.NameOf(family), PlayingOrLast());
		}

		/// <summary>
		/// What to keep the random picker away from: the track that is
		/// actually playing, falling back to the last one requested.
		/// </summary>
		/// <remarks>
		/// lastRequested is not the same thing. After a handover, a crossfade
		/// or an interrupted transition the two drift apart, and the picker
		/// was then free to choose the track currently sounding - which is
		/// how a transition came to ask for the music it was already
		/// playing.
		///
		/// Refusing that in the player is the real fix, because it makes the
		/// mistake harmless wherever it comes from. Asking the right question
		/// here stops it being offered in the first place.
		/// </remarks>
		private MusicTimingTable.Entry PlayingOrLast()
		{
			MusicCrossfadePlayer crossfader = player;

			if (crossfader != null && crossfader.Current != null)
			{
				return crossfader.Current;
			}

			return lastRequested;
		}

		private void Decide(out string key, out MusicTimingTable.Entry entry)
		{
			EngineASX engine = EngineASX.Instance;

			entry = null;

			// Read the dock BEFORE any early return, because it is a fact
			// about the ship and not about the music.
			//
			// It used to be computed after the menu and combat branches had
			// already returned, which left dockedChanged STALE in both of
			// them - and that is where it did its damage:
			//
			//  - Dock while being shot at on the way in. Decide takes the
			//    combat branch, never reaches the dock test, and the arrival
			//    at the station is handled as a plain interrupt. The station
			//    crossfade appeared broken whenever you got shot at, which
			//    is most of the times anyone docks.
			//
			//  - The whole main menu session kept whatever value it had on
			//    the way out. Sitting in the menu for a minute and loading a
			//    save started with a dock flag from a previous world, and
			//    that is the menu transfer the crossfade was interfering
			//    with.
			Unit mine = engine == null || engine.LocalPlayer == null
				|| engine.LocalPlayer.Person == null
				? null
				: engine.LocalPlayer.Person.CurrentUnit;

			bool docked = mine != null
				&& mine.Components != null
				&& mine.Components.IsDocked;

			// Read here rather than inferred from the state key, because the
			// key cannot be trusted to change on a dock. Docking at a station
			// nobody owns falls back to Neutral ambience, and if the sector
			// was unowned too then the key does not change AT ALL - the ship
			// docked, and the music would be told nothing happened.
			dockedNow = docked;

			if (IsMainMenu(engine))
			{
				// The one track, looping - not a random draw from the Menu
				// category. See MusicLibrary.MenuTrack.
				key = "Menu";
				entry = MusicLibrary.MenuTrack();
				return;
			}

			// DOCKING OVERRIDES THE THREAT LOGIC, and is checked first for
			// that reason. It used to be checked after combat, so a fight
			// still running when you reached a station kept the combat music
			// playing over the starport theme and the dock was inaudible as
			// an event.
			//
			// Reset as well as choosing: a threat linger pending here would
			// drop the player back into threat music on undocking, for a
			// fight that ended several sectors ago.
			if (docked)
			{
				detector.Reset();

				MusicFamily owner =
					MusicFactionClassifier.FamilyOf(DockOwner(mine), engine.LocalFaction);

				MusicTimingTable.Entry starport = MusicLibrary.StarportOf(owner);

				if (starport != null)
				{
					key = "Starport/" + MusicFactionClassifier.NameOf(owner);
					entry = starport;

					return;
				}

				// No starport track for this family - which is the case for
				// Neutral, because there is no Neutral starport. Falling back
				// to Neutral ambience is the same answer the space state gives
				// for an unowned sector, so docking somewhere nobody owns
				// needs no rule of its own.
				key = "Space/Neutral";
				entry = MusicLibrary.RandomFrom("Neutral", PlayingOrLast());
				return;
			}


			MusicCombatState combat = detector.Sample();

			// Not `InCombat`, which was "one hostile is enough". The threat
			// logic decides for itself now, and a quiet sector reads Normal.
			if (combat.IsThreat)
			{
				key = "Combat/" + combat.Category + "/" + combat.Intensity;

				if (sequencer.SetKind(combat.Category, combat.Intensity))
				{
					lastCombat = combat;
					entry = sequencer.Current;
				}
				else
				{
					entry = sequencer.Current;
				}

				return;
			}

			if (HypersleepHelper.IsHypersleepActive())
			{
				key = "Hypersleep";
				entry = MusicLibrary.RandomFrom("Hypersleep", PlayingOrLast());
				return;
			}

			MusicFamily family = MusicFactionClassifier.FamilyOf(
				engine.ActiveSector == null ? null : engine.ActiveSector.ControllingFaction,
				engine.LocalFaction);

			key = "Space/" + MusicFactionClassifier.NameOf(family);
			entry = MusicLibrary.RandomFrom(MusicFactionClassifier.NameOf(family), PlayingOrLast());
		}

		/// <summary>The faction that owns the station this ship is docked in.</summary>
		private static Faction DockOwner(Unit ship)
		{
			if (ship == null || ship.Components == null)
			{
				return null;
			}

			UnitHangarBay bay = ship.Components.DockedInHangarBay;

			if (bay == null || bay.Hangar == null || bay.Hangar.UnitComponents == null)
			{
				return null;
			}

			Unit station = bay.Hangar.UnitComponents.Unit;

			return station == null ? null : station.Faction;
		}

		private static bool IsMainMenu(EngineASX engine)
		{
			if (engine == null || engine.World == null || GameController.Instance == null)
			{
				return false;
			}

			ScenarioInfo here = engine.World.ScenarioInfo;
			ScenarioInfo menu = GameController.Instance.MainMenuScenario;

			return here != null && menu != null
				&& (here == menu || here.UniqueId == menu.UniqueId);
		}

		/// <summary>Everything, on one line, for a log or a diagnostic overlay.</summary>
		public string Describe()
		{
			MusicCombatState combat = detector.Sample();

			return "state " + currentKey
				+ " | combat " + combat
				+ " | playing " + Player.Describe()
				+ " | " + MusicLibrary.LoadedClipCount + " clips loaded";
		}
	}
}