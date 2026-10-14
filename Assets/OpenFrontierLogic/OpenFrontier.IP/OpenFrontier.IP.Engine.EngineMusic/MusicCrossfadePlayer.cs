using UnityEngine;

namespace OpenFrontier.IP.Engine.EngineMusic
{
	/// <summary>
	/// Plays one track, hands over to the next without a cut, and never
	/// silences itself because it had nothing to say.
	/// </summary>
	/// <remarks>
	/// Two audio sources, because one cannot crossfade. The outgoing track
	/// keeps its own AudioSource and is allowed to finish; the incoming gets
	/// the other. Two paths out, and which one is used depends on whether the
	/// track was allowed to reach its ending:
	///
	///   Natural - the track reached its pinned ending onset, so the handover
	///   happens there and the outgoing plays its own composed ending under
	///   the incoming. Nothing is cut, and the overlap is exactly as long as
	///   the music wants it to be.
	///
	///   Interrupted - something changed the world first: you undocked, you
	///   jumped, a fight started. The outgoing is faded to silence over
	///   InterruptFadeSeconds and the incoming starts after.
	///
	/// The second path exists because the endings are long. Federal Starport
	/// has a 49 second ending. Waiting it out when a player undocks after
	/// twenty seconds would be worse than any cut.
	///
	/// The volume is read from the existing music player rather than kept
	/// here, so the options screen keeps working with no changes to it and no
	/// second volume setting to disagree with the first.
	/// </remarks>
	public sealed class MusicCrossfadePlayer : MonoBehaviour
	{
		/// <summary>How long a cut track takes to reach silence.</summary>
		[Tooltip("How long an interrupted track is faded out before the next one starts.")]
		public float InterruptFadeSeconds = 1.5f;

		/// <summary>How long a dock or undock crossfade takes.</summary>
		[Tooltip("How long entering or leaving a station crossfades between the two tracks.")]
		public float StationCrossfadeSeconds = 2f;

		/// <summary>How long the menu takes to fade to silence on the way out.</summary>
		[Tooltip("How long the main menu music takes to reach silence before gameplay music starts.")]
		public float MenuFadeSeconds = 3f;

		private AudioSource currentSource;
		private AudioSource incomingSource;
		private MusicTimingTable.Entry currentEntry;
		private MusicTimingTable.Entry incomingEntry;
		private float currentStartedAt;
		private float incomingStartedAt;
		private float fadeStartedAt;
		private float fadeSeconds = 1.5f;
		private bool fadingOut;
		private bool crossfading;
		private float crossfadeStartedAt;
		private bool incomingWaiting;

		/// <summary>What is playing, or null.</summary>
		public MusicTimingTable.Entry Current => currentEntry;

		/// <summary>What is coming, once a handover has been decided.</summary>
		public MusicTimingTable.Entry Incoming => incomingEntry;

		/// <summary>True while the outgoing track is being faded to silence.</summary>
		public bool IsFadingOut => fadingOut;

		/// <summary>True while two tracks are ramping against each other.</summary>
		public bool IsCrossfading => crossfading;

		/// <summary>Seconds since the current track started.</summary>
		/// <remarks>
		/// Unscaled time, deliberately. Time.time stops dead at timeScale 0,
		/// and the main menu is a ScreenBase - ScreenNavigator sets
		/// timeScale = 0 for it - so a Time.time clock froze the moment the
		/// menu opened. The overlay showed it: 186s of 218s, ends in 23s, and
		/// it stayed exactly there, because 23 seconds of scaled time never
		/// elapsed. The ending onset was therefore never reached, so no track
		/// ever handed over to another.
		///
		/// The old SimpleMusicPlayer already used Time.realtimeSinceStartup for
		/// its fades for the same reason. This had to match it.
		/// </remarks>
		public float CurrentElapsed => currentEntry == null
			? 0f
			: Time.realtimeSinceStartup - currentStartedAt;

		/// <summary>
		/// Whether the current track has reached its pinned ending onset AND
		/// nobody has taken over yet.
		/// </summary>
		/// <remarks>
		/// The measured point at which the music starts coming down. It is
		/// median 68 seconds into a median 88 second track, and it is where
		/// the handover belongs, because that is the point at which the
		/// composer started ending the piece.
		///
		/// The incoming clause is what keeps a handover from repeating: while
		/// the outgoing track is playing its ending underneath the incoming
		/// one, the elapsed time keeps growing past the onset, so without it
		/// this stays true for the whole overlap and the caller asks for
		/// another handover every frame - which restarts the incoming track
		/// from its first sample and sounds like a skip.
		/// </remarks>
		/// A looped track has no ending to reach, so it never triggers this.
		public bool ReachedEndingOnset =>
			currentEntry != null && !currentEntry.Loops && !incomingWaiting
				&& CurrentElapsed >= currentEntry.EndingOnset;

		/// <summary>How long until the ending onset, or 0 if not known.</summary>
		public float SecondsToEndingOnset
		{
			get
			{
				if (currentEntry == null)
				{
					return 0f;
				}

				return Mathf.Max(0f, currentEntry.EndingOnset - CurrentElapsed);
			}
		}

		private void Awake()
		{
			EnsureSources();
		}

		private void EnsureSources()
		{
			if (currentSource == null)
			{
				currentSource = gameObject.AddComponent<AudioSource>();
				currentSource.playOnAwake = false;
				currentSource.loop = false;
				currentSource.spatialBlend = 0f;
			}

			if (incomingSource == null)
			{
				incomingSource = gameObject.AddComponent<AudioSource>();
				incomingSource.playOnAwake = false;
				incomingSource.loop = false;
				incomingSource.spatialBlend = 0f;
			}
		}

		/// <summary>
		/// The music volume, from the existing player.
		/// </summary>
		/// <remarks>
		/// Read rather than stored so that moving the volume slider in the
		/// options takes effect immediately, which it would not if the value
		/// were latched when a track started - a track can run for three
		/// minutes and the player expects the slider to do something.
		/// </remarks>
		public float MasterVolume
		{
			get
			{
				if (GameController.Instance != null && GameController.Instance.MusicPlayer != null)
				{
					return GameController.Instance.MusicPlayer.Volume;
				}

				return 0.5f;
			}
		}

		/// <summary>
		/// Starts a track immediately, cutting whatever was playing.
		/// </summary>
		/// <remarks>
		/// For the first track of a session and for anything that has to
		/// sound like it began there. Everything else goes through
		/// <see cref="HandOver"/> or <see cref="Interrupt"/>, so that this
		/// stays the rare path and a cut is always deliberate.
		/// </remarks>
		public bool Start(MusicTimingTable.Entry entry)
		{
			if (entry == null)
			{
				return false;
			}

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				return false;
			}

			EnsureSources();

			StopBoth();
			incomingWaiting = false;
			fadingOut = false;

			currentSource.clip = clip;
			currentSource.volume = MasterVolume;
			currentSource.time = 0f;
			currentSource.loop = entry.Loops;
			currentSource.Play();

			currentEntry = entry;
			currentStartedAt = Time.realtimeSinceStartup;

			Debug.Log("Music: playing '" + entry.Name + "' (" + entry.Category
				+ ") from the start. " + entry.Duration.ToString("0")
				+ "s long, ends at " + entry.EndingOnset.ToString("0") + "s.");

			return true;
		}

		/// <summary>
		/// Hands over at the composed ending. The outgoing track is left to
		/// finish underneath.
		/// </summary>
		public bool HandOver(MusicTimingTable.Entry entry)
		{
			if (entry == null || currentEntry == null || entry == currentEntry)
			{
				return false;
			}

			// Refuse to stack handovers. ReachedEndingOnset already excludes a
			// pending one, so this should be unreachable - but if it is ever
			// reached, the second call would reassign the clip of a source
			// that is mid-playback and restart it from zero, which is the
			// skip sound this whole class exists to avoid.
			//
			// REFUSE, DO NOT PROMOTE. Promoting the pending incoming is right
			// for a transition the WORLD asked for - a dock, a wormhole - and
			// wrong here. A handover is the system following its own plan;
			// there is nothing to take over from, and promoting would adopt
			// the incoming as current and then hand over again on top of it.
			//
			// A scripted replace put the promote block here instead of in
			// Interrupt, because both ended in the same four lines. Read the
			// guards in the methods you think you edited.
			if (incomingWaiting || fadingOut || crossfading)
			{
				return false;
			}

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				return false;
			}

			EnsureSources();

			incomingSource.Stop();
			incomingSource.clip = clip;
			incomingSource.volume = MasterVolume;
			incomingSource.time = 0f;
			incomingSource.Play();

			incomingEntry = entry;
			incomingStartedAt = Time.realtimeSinceStartup;
			incomingWaiting = true;

			Debug.Log("Music: handing over to '" + entry.Name + "' ("
				+ entry.Category + ") over the ending of '"
				+ currentEntry.Name + "', " + currentEntry.Duration.ToString("0")
				+ "s long with " + (currentEntry.Duration - currentEntry.EndingOnset).ToString("0")
				+ "s of ending left.");

			return true;
		}

		/// <summary>
		/// Cuts to a new track, fading the outgoing one to silence first.
		/// </summary>
		/// <remarks>
		/// The incoming track waits for the fade rather than starting under
		/// it. Starting both at once would be a crossfade, and a crossfade
		/// here means overlapping two pieces of music that were composed to be
		/// heard alone - which is how a transition ends up sounding like a
		/// mistake rather than a change of scene.
		/// </remarks>
		public bool Interrupt(MusicTimingTable.Entry entry) =>
			Interrupt(entry, InterruptFadeSeconds);

		/// <summary>
		/// Fades to silence over a given duration, then plays the new track.
		/// </summary>
		/// <remarks>
		/// The duration is a parameter because the fades are not
		/// interchangeable. The default is short and blunt, sized for a
		/// wormhole where the world has already moved on and the player is
		/// looking somewhere else. Leaving the main menu is the opposite: a
		/// deliberate, calm exit from a five minute piece, where 1.5s sounds
		/// like the music was cut off rather than let go.
		///
		/// Only the menu needs this. Everywhere else a fade out is a response
		/// to something sudden, and a long silence after a sudden thing is
		/// worse than a quick one.
		/// </remarks>
		public bool Interrupt(MusicTimingTable.Entry entry, float seconds)
		{
			if (entry == null)
			{
				return false;
			}

			if (currentEntry == null || currentSource == null || !currentSource.isPlaying)
			{
				return Start(entry);
			}

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				// Nothing to fade to. Leave the current track alone rather
				// than fading it out into silence, which is what treating an
				// unloadable track as a successful interruption would do.
				Debug.LogWarning("Music: cannot interrupt to '" + entry.Name
					+ "', its clip would not load. '" + currentEntry.Name
					+ "' keeps playing.");

				return false;
			}

			// Armed but not started. The clip is loaded and put on the spare
			// source now, and PromoteIncoming starts it when the fade ends.
			//
			// It has to be loaded HERE. Deferring the load to the promote left
			// the promoted source with no clip at all - silence - and left
			// incomingStartedAt at zero, so CurrentElapsed became the time
			// since the level started and ReachedEndingOnset fired immediately
			// on the very next frame. Every interruption was a silent gap
			// followed by a skip.
			EnsureSources();

			incomingSource.Stop();
			incomingSource.clip = clip;
			incomingSource.time = 0f;

			incomingEntry = entry;
			incomingWaiting = true;
			crossfading = false;

			if (fadingOut)
			{
				// Already on the way to silence. Arm the track and LEAVE THE
				// FADE RUNNING - do not restart it.
				//
				// Restarting would snap the volume back to full at the moment
				// the new world appeared: audible, and the opposite of the
				// point, since the menu was already on its way out. The load
				// simply finished early and there is now somewhere for the
				// fade to arrive.
				Debug.Log("Music: '" + entry.Name + "' is ready in time for the"
					+ " fade already running, so it arrives behind it rather"
					+ " than starting a second fade.");

				return true;
			}

			// A HANDOVER, as opposed to a fade - and only now that the fade
			// case above is settled.
			//
			// The order is the whole point. Interrupt sets incomingWaiting
			// AND fadingOut together, so during any fade BOTH flags are true.
			// Promoting here would adopt the incoming track that the fade is
			// deliberately holding SILENT, stop the outgoing mid-fade, and
			// make the quiet track audible. The fade case must be tested
			// first, every time.
			if (incomingWaiting)
			{
				// Take over: the handover's track becomes current and the
				// transition runs from there. Its predecessor's ending is
				// cut, which is right - a composed ending belongs to a track
				// that ran its course, not one replaced halfway through its
				// fade-out.
				//
				// Refusing instead left the outgoing playing its ending for up
				// to 49 seconds, the longest in the library, while the caller
				// retried every frame and was refused every frame - so a dock
				// or a wormhole during a handover did not happen at all.
				PromoteIncoming();
			}

			fadingOut = true;
			fadeStartedAt = Time.realtimeSinceStartup;
			fadeSeconds = seconds;

			Debug.Log("Music: interrupted '" + currentEntry.Name + "' after "
				+ CurrentElapsed.ToString("0") + "s of " + currentEntry.Duration.ToString("0")
				+ "s - fading to silence over " + fadeSeconds
				+ "s, then '" + entry.Name + "' (" + entry.Category + ").");

			return true;
		}

		private void Update()
		{
			TrackVolume();

			if (crossfading)
			{
				StepCrossfade();

				return;
			}

			if (fadingOut)
			{
				StepFadeOut();

				return;
			}

			// A handover is under way: the incoming track is already playing
			// underneath the outgoing one, and the outgoing still has its
			// composed ending to finish. Nothing happens until it stops.
			//
			// This branch is separate from the one below rather than a test
			// inside it, because getting that wrong is a deadlock: the only
			// thing that clears currentEntry was guarded on
			// !incomingWaiting, so during a handover it never cleared, so
			// PromoteIncoming never ran, so currentEntry stayed on the
			// finished track. Worse, ReachedEndingOnset stayed true with it,
			// and the director kept calling HandOver - restarting the
			// incoming clip from zero every frame. That is why
			// ReachedEndingOnset also excludes a pending handover.
			if (incomingWaiting)
			{
				if (currentEntry == null || currentSource == null || !currentSource.isPlaying)
				{
					PromoteIncoming();
				}

				return;
			}

			// A LOOPED track reports isPlaying true forever, so it must be
			// excluded here. It is not broken and must not be "recovered" by
			// restarting it - and if it were, the restart would fight
			// AudioSource.loop and restart the menu track forever.
			if (currentEntry != null && currentSource != null && !currentEntry.Loops
				&& !currentSource.isPlaying && CurrentElapsed > 1f)
			{
				Debug.Log("Music: '" + currentEntry.Name + "' stopped playing at "
					+ CurrentElapsed.ToString("0") + "s of " + currentEntry.Duration.ToString("0")
					+ "s, before its ending onset at "
					+ currentEntry.EndingOnset.ToString("0") + "s.");

				currentEntry = null;
			}
		}

		/// <summary>
		/// Keeps both sources at the current volume, every frame.
		/// </summary>
		/// <remarks>
		/// The volume was previously only written at transitions - Start,
		/// HandOver, the fade, and PromoteIncoming - so it was a snapshot
		/// taken when the track began and never revised. Moving the volume
		/// slider then did nothing to whatever was already playing, and did
		/// not take effect until the next track happened to start, which on a
		/// 98 second track is a minute and a half of the slider appearing
		/// broken. It was read live in the property and then thrown away.
		///
		/// Re-reading it every frame is cheap - one property chain and two
		/// float comparisons - and it is the only way a value the player can
		/// change at any moment stays true.
		///
		/// Skipped during a fade-out, because there the fade is the authority:
		/// StepFadeOut owns the volume and scales it down on purpose.
		/// </remarks>
		private void TrackVolume()
		{
			// Both ramps own the volume while they run. Without this the
			// per-frame volume tracking would reset both sources to full
			// volume every frame and the crossfade would be a hard cut - the
			// exact thing it exists to avoid.
			if (fadingOut || crossfading)
			{
				return;
			}

			float wanted = MasterVolume;

			if (currentSource != null && !Mathf.Approximately(currentSource.volume, wanted))
			{
				currentSource.volume = wanted;
			}

			if (incomingSource != null && incomingSource != currentSource
				&& !Mathf.Approximately(incomingSource.volume, wanted))
			{
				incomingSource.volume = wanted;
			}
		}

		/// <summary>
		/// Crossfades to a new track: both play at once, one down and one up.
		/// </summary>
		/// <remarks>
		/// This is the third transition, and the one that makes docking sound
		/// like arriving somewhere instead of a gap with music in it.
		///
		/// <see cref="Interrupt"/> deliberately does NOT overlap - it fades
		/// the old track to silence and only then starts the new one, because
		/// these pieces were composed to be heard alone and a linear overlap
		/// of two of them sounds like a mistake. That reasoning holds when a
		/// track is being cut short by something abrupt, like a wormhole.
		///
		/// It does not hold for a dock. You have flown here, you are looking
		/// at the station, and the music arriving with you is a change of
		/// place rather than an interruption of anything - and a fade to
		/// silence followed by a fade up puts a hole in the middle of it.
		///
		/// EQUAL POWER, NOT EQUAL GAIN. The obvious ramp is linear, which sums
		/// the two AMPLITUDES to a constant 1 and therefore dips about 3 dB
		/// through the middle of every transition - the classic "hole" that
		/// makes a crossfade sound like a crossfade. Ramping with sin and cos
		/// instead sums the two POWERS to a constant 1, so the perceived
		/// loudness holds steady through the middle while each track still
		/// sounds like it is arriving or leaving rather than being switched.
		///
		/// That is the same pair of curves a DJ mixer uses, and it is the
		/// right answer here for the same reason: the two tracks are
		/// unrelated music, so there is no shared waveform for them to
		/// reinforce, and only the power sum is meaningful.
		/// </remarks>
		public bool Crossfade(MusicTimingTable.Entry entry)
		{
			if (entry == null)
			{
				return false;
			}

			if (currentEntry == null || currentSource == null || !currentSource.isPlaying)
			{
				return Start(entry);
			}

			// A fade already running means this is not a crossfade, it is the
			// tail of a fade with somewhere new to arrive. Unguarded it set
			// crossfading while fadingOut stayed true, and Update checks
			// crossfading FIRST - so the fade was stranded. The crossfade ran
			// to completion, the next frame found fadingOut still set, and
			// faded the brand new track away to silence.
			if (fadingOut)
			{
				Debug.Log("Music: a crossfade to '" + entry.Name + "' arrived"
					+ " during a fade, so it takes the place of the fade"
					+ " rather than starting a second one.");

				return Interrupt(entry);
			}

			// A HANDOVER, and only now the fade case is dealt with. Interrupt
			// sets incomingWaiting AND fadingOut together, so during a fade
			// both are true - and promoting first would adopt the very track
			// the fade is deliberately holding silent, stop the outgoing
			// mid-fade, and make the quiet track audible.
			//
			// Take over: the handover's promoted track becomes current and the
			// crossfade runs from there. Refusing left the outgoing playing
			// its ending for up to 49 seconds, the longest in the library,
			// while the director retried every frame and was refused every
			// frame - so docking during a handover did not happen at all.
			if (incomingWaiting)
			{
				PromoteIncoming();
			}

			AudioClip clip = MusicLibrary.ClipFor(entry);

			if (clip == null)
			{
				// Same reasoning as Interrupt: do not fade toward a track that
				// cannot play, or the player gets silence instead of music.
				Debug.LogWarning("Music: cannot crossfade to '" + entry.Name
					+ "', its clip would not load. '" + currentEntry.Name
					+ "' keeps playing.");

				return false;
			}

			EnsureSources();

			// Start the incoming SILENTLY rather than silently-but-stopped, so
			// it is already decoding and playing when the ramp begins. Playing
			// it at zero and ramping up avoids the click that starting it at
			// full volume and ramping down would give the outgoing track.
			incomingSource.Stop();
			incomingSource.clip = clip;
			incomingSource.time = 0f;
			incomingSource.loop = entry.Loops;
			incomingSource.volume = 0f;
			incomingSource.Play();

			incomingEntry = entry;
			incomingStartedAt = Time.realtimeSinceStartup;
			incomingWaiting = true;
			crossfading = true;

			// Explicit, though the fade case returned above and so this is
			// already false. Update checks crossfading BEFORE fadingOut, so
			// both being true would strand the fade - the crossfade would run
			// to completion and the next frame would find fadingOut still set
			// and fade the brand new track away. Relying on "it cannot get
			// here" for a flag that strands a fade is how it got stranded.
			fadingOut = false;

			crossfadeStartedAt = Time.realtimeSinceStartup;

			Debug.Log("Music: crossfading from '" + currentEntry.Name + "' to '"
				+ entry.Name + "' (" + entry.Category + ") over "
				+ StationCrossfadeSeconds + "s, equal power.");

			return true;
		}

		private void StepCrossfade()
		{
			float elapsed = Time.realtimeSinceStartup - crossfadeStartedAt;
			float t = StationCrossfadeSeconds <= 0f
				? 1f
				: Mathf.Clamp01(elapsed / StationCrossfadeSeconds);

			float half = t * Mathf.PI * 0.5f;
			float master = MasterVolume;

			if (currentSource != null)
			{
				currentSource.volume = master * Mathf.Sin(half);
			}

			if (incomingSource != null)
			{
				incomingSource.volume = master * Mathf.Cos(half);
			}

			if (t >= 1f)
			{
				crossfading = false;

				// PromoteIncoming stops and clears the outgoing source and
				// swaps the fields. It will not restart the incoming track,
				// because wasAlreadyPlaying is true - it has been playing
				// through the whole ramp - and it restores its volume to
				// master, which is where the ramp left it.
				PromoteIncoming();
			}
		}

		/// <summary>
		/// Fades the current track to silence with nothing queued behind it.
		/// </summary>
		/// <remarks>
		/// A fade out that is not a transition. The next track is unknown
		/// yet, or there may not be one for some time - which is exactly the
		/// situation when leaving the main menu, because the menu hands over
		/// to a loading scene and the next world does not exist yet.
		///
		/// THE MENUS FADE COULD NOT START WITHOUT THIS. The director only
		/// decides what to play once the world is ready, and leaving the menu
		/// means the world is emphatically NOT ready - it is being torn down,
		/// then a loading scene runs, then a new world is built. So the fade
		/// began when the new world became observable, which is after the
		/// scene change the player was watching, and 3 seconds of it played
		/// over a world that had already loaded. Asking for the menu to fade
		/// out BEFORE the scene change is a request for a fade that does not
		/// have a destination yet.
		///
		/// Returns false when there is nothing playing or a fade is already
		/// running, so the caller can call it every frame without guarding.
		/// </remarks>
		public bool FadeOutOnly(float seconds)
		{
			if (currentEntry == null || currentSource == null || !currentSource.isPlaying)
			{
				return false;
			}

			// The fadingOut guard is load-bearing, not tidiness. During a fade
			// the track is still Playing and currentEntry is still set - it
			// is quieter, not finished - so without this the caller's
			// every-frame invocation would reset fadeStartedAt each frame and
			// the fade would never complete. It would sit just below full
			// volume forever, which is worse than not fading at all.
			if (fadingOut)
			{
				return false;
			}

			// Taking over from a crossfade is allowed, and the incoming source
			// MUST be stopped here.
			//
			// A crossfade in progress has its incoming track playing at
			// whatever volume the ramp had reached. Clearing incomingEntry
			// makes PromoteIncoming return early, so nothing else would ever
			// stop it - it would carry on at that volume with no owner. That
			// is two pieces of music at once, and it is the one place in this
			// class where a spare source can be left behind.
			if (incomingSource != null && incomingSource != currentSource)
			{
				incomingSource.Stop();
				incomingSource.clip = null;
				incomingSource.volume = 0f;
			}

			incomingEntry = null;
			incomingWaiting = false;
			crossfading = false;
			fadingOut = true;
			fadeStartedAt = Time.realtimeSinceStartup;
			fadeSeconds = seconds;

			Debug.Log("Music: letting go of '" + currentEntry.Name
				+ "' after " + CurrentElapsed.ToString("0") + "s - fading to"
				+ " silence over " + seconds + "s with nothing behind it.");

			return true;
		}

		private void StepFadeOut()
		{
			float elapsed = Time.realtimeSinceStartup - fadeStartedAt;
			float t = fadeSeconds <= 0f
				? 1f
				: Mathf.Clamp01(elapsed / fadeSeconds);

			if (currentSource != null)
			{
				currentSource.volume = MasterVolume * (1f - t);
			}

			if (t >= 1f)
			{
				if (currentSource != null)
				{
					currentSource.Stop();
					currentSource.volume = MasterVolume;
				}

				currentEntry = null;
				fadingOut = false;
				PromoteIncoming();
			}
		}

		private void PromoteIncoming()
		{
			if (incomingEntry == null)
			{
				incomingWaiting = false;
				return;
			}

			if (currentSource != null)
			{
				currentSource.Stop();
				currentSource.clip = null;
			}

			// Swap, do not re-find. GetComponent<AudioSource>() returns the
			// FIRST AudioSource on the object, which is the one just stopped
			// - it was added first in EnsureSources - so assigning it here
			// pointed both fields at the same component. The "free" slot was
			// the busy one, and the next handover would stop the track that
			// was playing.
			bool wasAlreadyPlaying = incomingSource != null && incomingSource.isPlaying;

			if (incomingSource != null)
			{
				AudioSource outgoing = currentSource;

				currentSource = incomingSource;
				incomingSource = outgoing;

				currentSource.volume = MasterVolume;
				currentSource.loop = currentEntry != null && currentEntry.Loops;

				// Two paths arrive here and they differ. A handover's track is
				// already playing underneath and must not be restarted. An
				// interruption's track was armed but deliberately silent while
				// the old one faded, and has to start now.
				//
				// The clock follows the same split: a handover keeps the time
				// it actually began at, so it stays in step with music that
				// has been audible since before the fade; an interruption
				// starts counting from the moment it became audible, or its
				// elapsed time would include the fade it spent not playing.
				if (!wasAlreadyPlaying && currentSource.clip != null)
				{
					currentSource.Play();
					incomingStartedAt = Time.realtimeSinceStartup;
				}
			}

			currentEntry = incomingEntry;
			currentStartedAt = incomingStartedAt;
			incomingEntry = null;
			incomingWaiting = false;
		}

		private void StopBoth()
		{
			if (currentSource != null)
			{
				currentSource.Stop();
				currentSource.clip = null;
			}

			if (incomingSource != null && incomingSource != currentSource)
			{
				incomingSource.Stop();
				incomingSource.clip = null;
			}

			currentEntry = null;
			incomingEntry = null;
			incomingWaiting = false;
			fadingOut = false;

			// A crossfade left mid-ramp would otherwise stay true forever and
			// Update would keep calling StepCrossfade against sources that are
			// no longer paired - and Start calls StopBoth, so this is on the
			// path of any transition that happens during a ramp.
			crossfading = false;
		}

		/// <summary>Stops everything, for leaving a game.</summary>
		public void StopAll()
		{
			StopBoth();
		}

		/// <summary>Is the AudioSource Unity thinks is playing, actually playing?</summary>
		public bool SourcePlaying => currentSource != null && currentSource.isPlaying;

		/// <summary>The source's real volume right now, -1 if there is no source.</summary>
		public float SourceVolume => currentSource != null ? currentSource.volume : -1f;

		/// <summary>Whether a clip is actually attached to the source.</summary>
		public bool SourceHasClip => currentSource != null && currentSource.clip != null;

		/// <summary>
		/// The raw audio state, for the diagnostic overlay.
		/// </summary>
		/// <remarks>
		/// Added because "the overlay says it is playing and nothing can be
		/// heard" is not a reportable bug. Everything above this can say a
		/// track is playing while Unity's AudioSource disagrees, and those two
		/// facts being different is the whole of the problem. This is the
		/// side that can be wrong.
		/// </remarks>
		public string AudioState()
		{
			if (currentSource == null)
			{
				return "NO SOURCE";
			}

			return "playing=" + SourcePlaying
				+ " clip=" + (SourceHasClip ? currentSource.clip.name : "NONE")
				+ " vol=" + SourceVolume.ToString("0.00")
				+ " master=" + MasterVolume.ToString("0.00")
				+ " from=" + (HasLiveMasterVolume ? "player" : "FALLBACK")
				+ " listener=" + AudioListener.volume.ToString("0.00")
				+ " loop=" + currentSource.loop
				+ " paused=" + AudioListener.pause;
		}

		/// <summary>
		/// Whether the master volume is the real setting or the fallback.
		/// </summary>
		/// <remarks>
		/// MasterVolume returns a hardcoded 0.5 when GameController or its
		/// MusicPlayer is not there yet, which looks exactly like a real
		/// volume and is not. Saying which one it is turns "the volume is
		/// wrong" into a reportable fact - the menu screenshot showed
		/// vol=0.00 while the stored preference was 1, and nothing on screen
		/// could have said whether that was a live zero, a stale snapshot, or
		/// a player that was not there.
		/// </remarks>
		public bool HasLiveMasterVolume =>
			GameController.Instance != null && GameController.Instance.MusicPlayer != null;

		/// <summary>
		/// Both audio sources, named, with what each is actually doing.
		/// </summary>
		/// <remarks>
		/// The second source is included because it is SUPPOSED to be playing
		/// during a handover and during a crossfade. Reporting only the
		/// current one makes a perfectly correct overlap look exactly like a
		/// second music system running - which is the mistake being made here
		/// twice.
		/// </remarks>
		public string SourceReport()
		{
			return "[" + DescribeSource(currentSource, "out")
				+ " " + DescribeSource(incomingSource, "in") + "]";
		}

		private static string DescribeSource(AudioSource source, string tag)
		{
			if (source == null)
			{
				return tag + "=none";
			}

			string clip = source.clip == null ? "noclip" : source.clip.name;

			return tag + "=" + clip + (source.isPlaying ? "/PLAYING" : "/stopped");
		}

		/// <summary>One line describing what is playing and why.</summary>
		public string Describe()
		{
			if (currentEntry == null)
			{
				return fadingOut ? "fading out, nothing playing" : "silent";
			}

			return currentEntry.Name + " (" + currentEntry.Category + ") at "
				+ CurrentElapsed.ToString("0") + "s of " + currentEntry.Duration.ToString("0")
				+ "s, ending at " + currentEntry.EndingOnset.ToString("0") + "s"
				+ (incomingEntry != null ? ", next " + incomingEntry.Name : "");
		}
	}
}