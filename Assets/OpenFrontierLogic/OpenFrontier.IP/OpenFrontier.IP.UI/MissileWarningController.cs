using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	/// <summary>
	/// The new-system missile warning: an alert tone when something locks on,
	/// an alarm loop while it is coming, and a harder loop once it is close.
	/// </summary>
	/// <remarks>
	/// Three layers rather than one repeating beep, because the old cue could
	/// only change rate. An alert has to be distinguishable from a loop that
	/// has been running for a while, and the close-in tone has to be
	/// distinguishable from the alarm, or neither tells the player anything.
	/// Each layer is its own AudioSource so the alert can ring out over the top
	/// of the alarm it starts.
	///
	/// Closeness is the worse of distance and time to impact, not either one
	/// alone. Some missiles are fast and long range and some are slow and short,
	/// and a missile that only warned on distance would stay quiet about a fast
	/// one until it was far too late, while a missile that only warned on time
	/// to impact would stay quiet about a slow one until it was on top of you.
	/// Taking the maximum means either kind of threat escalates on its own
	/// terms.
	///
	/// Only new-system launchers get this. MissileLockAudioController keeps
	/// warning the old way for everything else, so nothing a player hears today
	/// changes.
	/// </remarks>
	public class MissileWarningController : MonoBehaviour
	{
		public enum WarningStage
		{
			/// <summary>Nothing has this ship locked.</summary>
			Clear = 0,

			/// <summary>The alert tone, as a missile acquires.</summary>
			Alert = 1,

			/// <summary>The alarm loop, once the threat is on its way.</summary>
			Alarm = 2,

			/// <summary>The close-in loop, once it is nearly here.</summary>
			ClosingIn = 3
		}

		[Header("Sound layers, per guidance type")]
		[Tooltip("Optical. The quietest set - a weapon that only sees a target by sight.")]
		public AudioClip OpticalAlert;

		public AudioClip OpticalAlarm;

		public AudioClip OpticalClosingIn;

		[Tooltip("Infrared. Sees a target by its own heat.")]
		public AudioClip IRAAlert;

		public AudioClip IRAAlarm;

		public AudioClip IRAClosingIn;

		[Tooltip("Semi-active radar. Needs the target to light it up.")]
		public AudioClip SarhAlert;

		public AudioClip SarhAlarm;

		public AudioClip SarhClosingIn;

		[Tooltip("Active radar. The loudest set - it finds you on its own.")]
		public AudioClip ArhAlert;

		public AudioClip ArhAlarm;

		public AudioClip ArhClosingIn;

		[Header("How loud")]
		[Tooltip("Loudness of every warning clip, 0 to 1. The alerts stack when a salvo arrives, so they add up fast and want to sit well below full.")]
		[Range(0f, 1f)]
		public float Volume = 0.5f;

		[Header("How the radar flashes")]
		[Tooltip("Colour the missile-lock indicator pulses from, per guidance type. Ordered quietest to loudest, so a redder flash means a better-guided missile. ARH is the loudest tier and the only one that escalates, so it is the brightest.")]
		public Color OpticalFlash = new Color(1f, 1f, 1f, 1f);

		public Color IRFlash = new Color(1f, 0.85f, 0.4f, 1f);

		public Color SarhFlash = new Color(1f, 0.55f, 0.2f, 1f);

		public Color ArhFlash = new Color(1f, 0.2f, 0.15f, 1f);

		[Tooltip("Colour it pulses to. Kept bright so the pulse reads as a flash rather than a fade to grey.")]
		public Color FlashPeak = Color.white;

		[Tooltip("Pulses per second while a missile is inbound but not yet close.")]
		public float FlashSpeedAlarm = 1.5f;

		[Tooltip("Pulses per second once an ARH is inside ClosingInDistance. Only ARH has a close-in tone, so this only ever runs for ARH.")]
		public float FlashSpeedClosingIn = 6f;

		[Header("When to escalate")]
		[Tooltip("Distance at which a missile counts as closing in, in metres. This is the half that actually works - see the note on ClosingInTime.")]
		public float ClosingInDistance = 500f;

		[Tooltip("Seconds to impact at which a missile counts as closing in. Currently inert: every homing missile has MoveSpeed 0, so time to impact cannot be computed. It starts working if a missile is given a real MoveSpeed.")]
		public float ClosingInTime = 4f;

		[Tooltip("Seconds the alert tone takes to fade out on its own if no missile has actually locked yet.")]
		public float AlertHoldTime = 1.5f;

		[Tooltip("How long after the last lock is released the alarm is left running, so a missile breaking off does not cut the sound dead.")]
		public float ReleaseGraceTime = 0.5f;


		/// <summary>The three clips for one guidance type.</summary>
		private AudioClip[] ClipsFor(MissileGuidanceType type)
		{
			switch (type)
			{
				case MissileGuidanceType.IR: return new[] { IRAAlert, IRAAlarm, IRAClosingIn };
				case MissileGuidanceType.SARH: return new[] { SarhAlert, SarhAlarm, SarhClosingIn };
				case MissileGuidanceType.ARH: return new[] { ArhAlert, ArhAlarm, ArhClosingIn };
				default: return new[] { OpticalAlert, OpticalAlarm, OpticalClosingIn };
			}
		}

		/// <summary>One-shots. PlayOneShot, so several can overlap and none of
		/// them cuts another off.</summary>
		private AudioSource acquireSource;

		/// <summary>The one loop. The alarm and the close-in tone are mutually
		/// exclusive, so they swap the clip rather than each needing a source.</summary>
		private AudioSource loopSource;

		private WarningStage stage = WarningStage.Clear;

		private float alertUntil;

		private float clearAt;

		/// <summary>The guidance type of the alert most recently played.</summary>
		private MissileGuidanceType sounding = MissileGuidanceType.Optical;

		/// <summary>The guidance type the loop is playing. Tracked apart from the
		/// alert's so a change of one does not restart the other.</summary>
		private MissileGuidanceType loopGuidance = MissileGuidanceType.Optical;

		/// <summary>Which layer is currently sounding. For tests and the HUD.</summary>
		public WarningStage Stage => stage;

		/// <summary>Which guidance type is sounding, and so what is threatening you.</summary>
		public MissileGuidanceType Sounding => sounding;

		/// <summary>
		/// The sine pulse every missile-lock indicator in the HUD reads from.
		/// </summary>
		/// <remarks>
		/// This is the same SpriteSineColor the compass and the lock buttons
		/// already share, so driving it from here moves every indicator at once
		/// and they cannot drift from what is being heard - the flash is a
		/// readout of the same guidance type and stage that chose the clip.
		/// Null until the HUD exists, which is why it is resolved lazily.
		/// </remarks>
		private SpriteSineColor HudSine
		{
			get
			{
				if (EngineASX.LoadedAndReady && EngineASX.Instance.Hud != null && EngineASX.Instance.Hud.MissileLockButtonsController != null)
				{
					return EngineASX.Instance.Hud.MissileLockButtonsController.SpriteSineColor;
				}
				return null;
			}
		}

		private Color FlashColorFor(MissileGuidanceType type)
		{
			switch (type)
			{
				case MissileGuidanceType.IR: return IRFlash;
				case MissileGuidanceType.SARH: return SarhFlash;
				case MissileGuidanceType.ARH: return ArhFlash;
				default: return OpticalFlash;
			}
		}

		/// <summary>
		/// Points the shared pulse at the current threat, or restores it when
		/// nothing is inbound.
		/// </summary>
		private void UpdateFlash()
		{
			SpriteSineColor sine = HudSine;

			if (sine == null)
			{
				return;
			}

			if (stage == WarningStage.Clear)
			{
				if (flashRestored)
				{
					return;
				}

				// Put back whatever the prefab shipped with rather than a guessed
				// value, so the idle indicator looks exactly as it did before any
				// of this ran.
				sine.from = flashOriginalFrom;
				sine.to = flashOriginalTo;
				sine.Speed = flashOriginalSpeed;
				flashRestored = true;
				return;
			}

			flashRestored = false;
			sine.from = FlashColorFor(loopGuidance);
			sine.to = FlashPeak;
			sine.Speed = stage == WarningStage.ClosingIn ? FlashSpeedClosingIn : FlashSpeedAlarm;
		}

		/// <summary>Captures the prefab's pulse the first time the HUD is reachable.</summary>
		private void CaptureFlashDefaults()
		{
			if (flashCaptured)
			{
				return;
			}

			SpriteSineColor sine = HudSine;

			if (sine == null)
			{
				return;
			}

			flashOriginalFrom = sine.from;
			flashOriginalTo = sine.to;
			flashOriginalSpeed = sine.Speed;
			flashCaptured = true;
		}

		private bool flashCaptured;

		private bool flashRestored = true;

		private Color flashOriginalFrom;

		private Color flashOriginalTo;

		private float flashOriginalSpeed = 1f;

		private void Awake()
		{
			// Two sources, which is all this needs. One for the acquire
			// one-shots, one for whichever loop is currently running.
			acquireSource = AddSource("MissileAcquire");
			loopSource = AddSource("MissileLoop");
		}

		private AudioSource AddSource(string name)
		{
			AudioSource source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = false;
			source.spatialBlend = 0f;
			source.volume = Volume;
			return source;
		}

		private void OnEnable()
		{
			TrySubscribe();
		}

		private void OnDisable()
		{
			Unsubscribe();
			GoQuiet();
			// Update does not run while disabled, so without this the indicator
			// would stay on the threat colour after the screen goes away.
			CaptureFlashDefaults();
			UpdateFlash();
		}

		/// <summary>
		/// Subscribes to the lock events, if the engine is up and we are not
		/// already listening.
		/// </summary>
		/// <remarks>
		/// This is retried from Update rather than being done once in OnEnable.
		/// OnEnable runs when the GameObject becomes active, which is scene load
		/// - and EngineASX is not ready then, so a single guarded call in
		/// OnEnable never subscribed, the controller silently listened to
		/// nothing, and the symptom was silence with everything wired and the
		/// clips assigned.
		/// </remarks>
		private void TrySubscribe()
		{
			if (subscribed || !EngineASX.LoadedAndReady)
			{
				return;
			}

			EngineASX.Instance.MissileLockController.EntityGotMissileLock += OnGotLock;
			EngineASX.Instance.MissileLockController.EntityGotMissile += OnGotMissile;
			EngineASX.Instance.MissileLockController.EntityLostMissleLock += OnLostLock;
			subscribed = true;
		}

		private void Unsubscribe()
		{
			if (!subscribed)
			{
				return;
			}

			EngineASX.Instance.MissileLockController.EntityGotMissileLock -= OnGotLock;
			EngineASX.Instance.MissileLockController.EntityGotMissile -= OnGotMissile;
			EngineASX.Instance.MissileLockController.EntityLostMissleLock -= OnLostLock;
			subscribed = false;
		}

		private bool subscribed;


		private void OnGotLock(MissileLockController sender, Unit entity)
		{
			if (IsPlayerShip(entity))
			{
				clearAt = float.MaxValue;
			}
		}

		/// <summary>
		/// Every missile fires its own alert, so the player hears what is coming
		/// each time. The loop is not re-triggered here - UpdateStage gives the
		/// loop to the highest-priority missile and leaves it there.
		/// </summary>
		private void OnGotMissile(MissileLockController sender, Unit entity, Missile missile)
		{
			if (!IsPlayerShip(entity) || missile == null)
			{
				return;
			}

			clearAt = float.MaxValue;

			MissileGuidanceType type = GuidanceOf(missile);
			AudioClip alert = ClipsFor(type)[0];

			// Every missile announces itself. PlayOneShot does not restart a busy
			// source, so a salvo overlaps instead of chopping one tone into
			// fragments - which is why no rate limit is needed here any more.
			if (alert != null)
			{
				sounding = type;
				acquireSource.PlayOneShot(alert);
			}

			alertUntil = Time.time + AlertHoldTime;

			// The first arrival owns the loop until something worse turns up.
			if (stage == WarningStage.Clear)
			{
				stage = WarningStage.Alert;
				StartLoop(WarningStage.Alarm);
			}
		}

		private void OnLostLock(MissileLockController sender, Unit entity)
		{
			if (!IsPlayerShip(entity))
			{
				return;
			}

			clearAt = Time.time + ReleaseGraceTime;
		}

		private void Update()
		{
			if (!EngineASX.LoadedAndReady)
			{
				return;
			}

			TrySubscribe();

			if (stage == WarningStage.Clear)
			{
				CaptureFlashDefaults();
				UpdateFlash();
				return;
			}

			if (Time.time > clearAt)
			{
				GoQuiet();
				CaptureFlashDefaults();
				UpdateFlash();
				return;
			}

			UpdateStage();
			UpdateFlash();
		}

		/// <summary>
		/// Picks the loudest layer the current nearest threat justifies.
		/// </summary>
		private void UpdateStage()
		{
			Unit player = EngineASX.Instance.PlayerUnit;

			if (player == null || !player.IsActiveInEngine || !player.IsPlayerCurrentUnit)
			{
				GoQuiet();
				return;
			}

			Missile worst = WorstThreat(player);

			if (worst == null)
			{
				// locked but not yet tracking, or briefly out of the lookup.
				// Hold the alarm rather than dropping to silence and back.
				SetStage(WarningStage.Alarm);
				return;
			}

			float distance = Vector3.Distance(player.transform.position, worst.transform.position);

			if (GuidanceOf(worst) != loopGuidance)
			{
				// Something better-guided arrived, or the one sounding died. The
				// loop follows it. The alert is deliberately not replayed: every
				// missile already announced itself when it acquired.
				SwitchLoop(GuidanceOf(worst));
			}

			SetStage(IsClosingIn(worst, distance) && HasClosingInTone(loopGuidance)
				? WarningStage.ClosingIn
				: WarningStage.Alarm);
		}

		/// <summary>
		/// Whether this guidance type has a close-in tone at all. Only ARH has
		/// one, so it is the only type that escalates - the others hold their
		/// alarm all the way in rather than changing tone.
		/// </summary>
		/// <remarks>
		/// This also stops the silent hole. Without the check, a type with no
		/// closing-in clip still advanced the stage, StartLoop stopped the
		/// source, found a null clip and played nothing - so every non-ARH
		/// missile went quiet at the 500-unit mark instead of escalating, and
		/// the alarm never came back.
		/// </remarks>
		private bool HasClosingInTone(MissileGuidanceType type)
		{
			return ClipsFor(type)[2] != null;
		}

		/// <summary>
		/// The loop clip for a stage, falling back to that type's alarm.
		/// </summary>
		/// <remarks>
		/// Belt and braces behind HasClosingInTone: whatever routes in here,
		/// a missing clip degrades to the alarm rather than to silence.
		/// </remarks>
		private AudioClip LoopClipFor(MissileGuidanceType type, WarningStage stage)
		{
			AudioClip[] clips = ClipsFor(type);
			AudioClip loop = stage == WarningStage.ClosingIn ? clips[2] : clips[1];
			return loop != null ? loop : clips[1];
		}

		/// <summary>
		/// The most dangerous thing locked on, which is not the nearest thing.
		/// </summary>
		/// <remarks>
		/// Optical, IR, SARH, ARH - the highest type wins, because that is what
		/// tells the player what they are about to be hit by. Ties on the same
		/// type go to whichever is closer, so two ARH missiles escalate
		/// together on the nearer one.
		/// </remarks>
		private static Missile WorstThreat(Unit player)
		{
			Missile worst = null;
			float worstDist = 0f;
			var type = MissileGuidanceType.Optical;

			foreach (Missile missile in EngineASX.Instance.MissileLockController.GetMissileLocks(player))
			{
				if (missile == null || missile.IsDisrupted)
				{
					continue;
				}

				float dist = Vector3.Distance(player.transform.position, missile.transform.position);
				var t = GuidanceOf(missile);

				if (worst == null || t > type || (t == type && dist < worstDist))
				{
					worst = missile;
					worstDist = dist;
					type = t;
				}
			}

			return worst;
		}

		private static MissileGuidanceType GuidanceOf(Missile missile)
		{
			return missile.ProjectileClass != null
				? missile.ProjectileClass.GuidanceType
				: MissileGuidanceType.Optical;
		}

		/// <summary>Points the loop at one guidance type's clips.</summary>
		/// <remarks>
		/// Deliberately does not touch the alert and does not reset stage or
		/// clearAt. It used to call GoQuiet, which cleared clearAt to 0, so the
		/// next Update saw Time.time > 0 and silenced the loop it had just
		/// started - and GoQuiet also killed the alert mid-tone, which is what
		/// made a burst of missiles sound like the audio was glitching.
		/// </remarks>
		private void SwitchLoop(MissileGuidanceType type)
		{
			loopGuidance = type;

			loopSource.Stop();

			AudioClip loop = LoopClipFor(type, stage);

			if (loop != null)
			{
				loopSource.clip = loop;
				loopSource.loop = true;
				loopSource.Play();
			}
		}

		/// <summary>
		/// Whether a missile is close enough to be the urgent tone, by whichever
		/// of distance and time to impact says so first.
		/// </summary>
		private bool IsClosingIn(Missile missile, float distance)
		{
			if (distance <= ClosingInDistance)
			{
				return true;
			}

			// MoveSpeed is 0 on every homing missile in the base game, so this
			// half never fires and closing-in has always been decided by distance
			// alone. Kept because it is the right rule, and because it starts
			// working the moment a missile is given a real speed.
			float speed = missile.ProjectileClass != null
				? missile.ProjectileClass.MoveSpeed
				: 0f;

			if (speed <= 0f)
			{
				return false;
			}

			return distance / speed <= ClosingInTime;
		}

		/// <summary>
		/// Whether this is the ship the local player is actually flying.
		/// </summary>
		/// <remarks>
		/// Not IsOwnedByPlayer, which is `faction == LocalFaction` and so is
		/// true for every human in a multiplayer session. The lock event fires
		/// for any unit that gets locked, so the warning was firing when an ally
		/// was fired at, and playing that ship's missile assets - which carry no
		/// GuidanceType and read as Optical from the enum default. That is why
		/// every missile in the game sounded optical.
		/// </remarks>
		private static bool IsPlayerShip(Unit unit)
		{
			return unit != null && unit.IsPlayerCurrentUnit && unit.IsActiveInEngine;
		}

		private void PlayAlert()
		{
			alertUntil = Time.time + AlertHoldTime;
			stage = WarningStage.Alert;

			AudioClip alert = ClipsFor(sounding)[0];

			if (alert != null)
			{
				acquireSource.PlayOneShot(alert);
			}
		}

		private void SetStage(WarningStage next)
		{
			if (next == stage)
			{
				return;
			}

			// The alert rings out on its own; going straight from alert to a
			// loop is the point, and cutting the alert off would lose the one
			// sound that says something new just happened.
			if (next < WarningStage.Alarm)
			{
				return;
			}

			stage = next;
			StartLoop(next);
		}

		private void StartLoop(WarningStage next)
		{
			AudioClip loop = LoopClipFor(loopGuidance, next);

			loopSource.Stop();

			if (loop != null)
			{
				loopSource.clip = loop;
				loopSource.loop = true;
				loopSource.Play();
			}
		}

		private void GoQuiet()
		{
			stage = WarningStage.Clear;
			alertUntil = 0f;
			clearAt = 0f;
			loopSource.Stop();
			acquireSource.Stop();
		}
	}
}