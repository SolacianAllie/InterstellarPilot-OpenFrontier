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

		[Header("When to escalate")]
		[Tooltip("Distance at which a missile counts as closing in, in metres. A short-range slow missile escalates on this.")]
		public float ClosingInDistance = 1500f;

		[Tooltip("Seconds to impact at which a missile counts as closing in. A fast long-range missile escalates on this, while it is still far away.")]
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

		private AudioSource alertSource;

		private AudioSource alarmSource;

		private AudioSource closingSource;

		private WarningStage stage = WarningStage.Clear;

		private float alertUntil;

		private float clearAt;

		/// <summary>The guidance type whose clips are playing.</summary>
		private MissileGuidanceType sounding = MissileGuidanceType.Optical;

		/// <summary>Which layer is currently sounding. For tests and the HUD.</summary>
		public WarningStage Stage => stage;

		/// <summary>Which guidance type is sounding, and so what is threatening you.</summary>
		public MissileGuidanceType Sounding => sounding;

		private void Awake()
		{
			// Three sources on one GameObject. PlayOneShot on a shared camera
			// source would work for one-shots and cannot overlap loops, which is
			// the entire point of having layers.
			alertSource = AddSource("MissileAlert");
			alarmSource = AddSource("MissileAlarm");
			closingSource = AddSource("MissileClosingIn");
		}

		private AudioSource AddSource(string name)
		{
			AudioSource source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = false;
			source.spatialBlend = 0f;
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
			EngineASX.Instance.MissileLockController.EntityLostMissleLock -= OnLostLock;
			subscribed = false;
		}

		private bool subscribed;

		private void OnGotLock(MissileLockController sender, Unit entity)
		{
			if (!IsPlayerShip(entity))
			{
				return;
			}

			if (stage == WarningStage.Clear)
			{
				Missile first = WorstThreat(entity);
				sounding = first != null ? GuidanceOf(first) : MissileGuidanceType.Optical;
				PlayFor(sounding);
				alertUntil = Time.time + AlertHoldTime;
			}

			clearAt = float.MaxValue;
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
				return;
			}

			if (Time.time > clearAt)
			{
				GoQuiet();
				return;
			}

			UpdateStage();
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

			if (GuidanceOf(worst) != sounding)
			{
				// A better-guided missile arrived, or the one that was sounding
				// died. Re-pick the layers so the worst thing on the scope is
				// what is being heard.
				sounding = GuidanceOf(worst);
				PlayFor(sounding);
			}

			SetStage(IsClosingIn(worst, distance)
				? WarningStage.ClosingIn
				: WarningStage.Alarm);
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

		/// <summary>Swaps all three layers to one guidance type's clips.</summary>
		private void PlayFor(MissileGuidanceType type)
		{
			AudioClip[] clips = ClipsFor(type);

			GoQuiet();
			sounding = type;

			if (clips[0] != null)
			{
				alertSource.clip = clips[0];
				alertSource.Play();
			}

			if (clips[1] != null)
			{
				alarmSource.clip = clips[1];
				alarmSource.loop = true;
				alarmSource.Play();
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
				alertSource.clip = alert;
				alertSource.Play();
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
			AudioClip[] clips = ClipsFor(sounding);

			if (next == WarningStage.ClosingIn)
			{
				alarmSource.Stop();

				if (clips[2] != null)
				{
					closingSource.clip = clips[2];
					closingSource.loop = true;
					closingSource.Play();
				}
			}
			else if (clips[1] != null)
			{
				closingSource.Stop();

				if (!alarmSource.isPlaying)
				{
					alarmSource.clip = clips[1];
					alarmSource.loop = true;
					alarmSource.Play();
				}
			}
		}

		private void GoQuiet()
		{
			stage = WarningStage.Clear;
			alertUntil = 0f;
			clearAt = 0f;
			alarmSource.Stop();
			closingSource.Stop();
			alertSource.Stop();
		}
	}
}