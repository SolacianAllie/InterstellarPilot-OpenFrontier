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

			// The old controller is stood down from Update rather than from
			// here, because it lives on the EngineASX prefab and that does
			// not exist yet at BeforeSceneLoad. Looking now finds nothing,
			// says nothing, and leaves two systems playing music over each
			// other for the whole session.
		}

		private bool stoodDownOldController;

		private void StandDownTheOldController()
		{
			if (stoodDownOldController)
			{
				return;
			}

			EngineMusicPlayerController old = Object.FindObjectOfType<EngineMusicPlayerController>();

			if (old == null)
			{
				return;
			}

			old.enabled = false;
			stoodDownOldController = true;

			Debug.Log("Music: the previous music controller has been stood down."
				+ " Its SimpleMusicPlayer is still read for volume, so the options"
				+ " screen is unchanged.");
		}

		private void Awake()
		{
			instance = this;
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
					EngineASX.Instance.ActiveSector.ControllingFaction);

		private void Update()
		{
			StandDownTheOldController();

			EngineASX engine = EngineASX.Instance;

			if (engine == null || !EngineASX.LoadedAndReady)
			{
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

			// The world changed. Cut.
			if (wantedKey != currentKey)
			{
				if (crossfader.Current == null)
				{
					if (crossfader.Start(wanted))
					{
						currentKey = wantedKey;
						lastRequested = wanted;
					}
				}
				else if (crossfader.Interrupt(wanted))
				{
					currentKey = wantedKey;
					lastRequested = wanted;
				}

				return;
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

			// The track reached its ending, and nothing has changed since.
			// Hand over to the next one and let the old one finish.
			if (crossfader.ReachedEndingOnset && !crossfader.IsFadingOut)
			{
				MusicTimingTable.Entry next = NextFor(wantedKey, wanted);

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
		private MusicTimingTable.Entry NextFor(string key, MusicTimingTable.Entry current)
		{
			if (key.StartsWith("Combat/", System.StringComparison.Ordinal))
			{
				MusicTimingTable.Entry next = sequencer.Advance();
				return next ?? current;
			}

			return MusicLibrary.RandomFrom(current.Category, current) ?? current;
		}

		/// <summary>
		/// What should be playing, and why.
		/// </summary>
		private void Decide(out string key, out MusicTimingTable.Entry entry)
		{
			EngineASX engine = EngineASX.Instance;

			entry = null;

			if (IsMainMenu(engine))
			{
				// The one track, looping - not a random draw from the Menu
				// category. See MusicLibrary.MenuTrack.
				key = "Menu";
				entry = MusicLibrary.MenuTrack();
				return;
			}

			MusicCombatState combat = detector.Sample();

			if (combat.InCombat)
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

			Unit mine = engine.LocalPlayer == null || engine.LocalPlayer.Person == null
				? null
				: engine.LocalPlayer.Person.CurrentUnit;

			bool docked = mine != null
				&& mine.Components != null
				&& mine.Components.IsDocked;

			if (docked)
			{
				MusicFamily owner = MusicFactionClassifier.FamilyOf(DockOwner(mine));

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
				entry = MusicLibrary.RandomFrom("Neutral", lastRequested);
				return;
			}

			if (HypersleepHelper.IsHypersleepActive())
			{
				key = "Hypersleep";
				entry = MusicLibrary.RandomFrom("Hypersleep", lastRequested);
				return;
			}

			MusicFamily family = MusicFactionClassifier.FamilyOf(
				engine.ActiveSector == null ? null : engine.ActiveSector.ControllingFaction);

			key = "Space/" + MusicFactionClassifier.NameOf(family);
			entry = MusicLibrary.RandomFrom(MusicFactionClassifier.NameOf(family), lastRequested);
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