using OpenFrontier.IP.Engine;
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

		[Header("Sound layers")]
		[Tooltip("Played once as each missile acquires a lock on the player.")]
		public AudioClip AlertClip;

		[Tooltip("Looped while a missile is on its way.")]
		public AudioClip AlarmLoopClip;

		[Tooltip("Looped once a missile is closing in. The urgent one.")]
		public AudioClip ClosingInLoopClip;

		[Header("When to escalate")]
		[Tooltip("Distance at which a missile counts as closing in, in metres. A short-range slow missile escalates on this.")]
		public float ClosingInDistance = 1500f;

		[Tooltip("Seconds to impact at which a missile counts as closing in. A fast long-range missile escalates on this, while it is still far away.")]
		public float ClosingInTime = 4f;

		[Tooltip("Seconds the alert tone takes to fade out on its own if no missile has actually locked yet.")]
		public float AlertHoldTime = 1.5f;

		[Tooltip("How long after the last lock is released the alarm is left running, so a missile breaking off does not cut the sound dead.")]
		public float ReleaseGraceTime = 0.5f;

		private AudioSource alertSource;

		private AudioSource alarmSource;

		private AudioSource closingSource;

		private WarningStage stage = WarningStage.Clear;

		private float alertUntil;

		private float clearAt;

		/// <summary>Which layer is currently sounding. For tests and the HUD.</summary>
		public WarningStage Stage => stage;

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
				PlayAlert();
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

			if (player == null || !player.IsActiveInEngine || !player.IsOwnedByPlayer)
			{
				GoQuiet();
				return;
			}

			float distance;
			Missile nearest = player.GetNearestNonDisruptedMissileLock(out distance);

			if (nearest == null)
			{
				// locked but not yet tracking, or briefly out of the lookup.
				// Hold the alarm rather than dropping to silence and back.
				SetStage(WarningStage.Alarm);
				return;
			}

			if (IsClosingIn(nearest, distance))
			{
				SetStage(WarningStage.ClosingIn);
			}
			else
			{
				SetStage(WarningStage.Alarm);
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

		private static bool IsPlayerShip(Unit unit)
		{
			return unit != null && unit.IsOwnedByPlayer && unit.IsActiveInEngine;
		}

		private void PlayAlert()
		{
			alertUntil = Time.time + AlertHoldTime;
			stage = WarningStage.Alert;

			if (AlertClip != null)
			{
				alertSource.clip = AlertClip;
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
			if (next == WarningStage.ClosingIn)
			{
				alarmSource.Stop();

				if (ClosingInLoopClip != null)
				{
					closingSource.clip = ClosingInLoopClip;
					closingSource.loop = true;
					closingSource.Play();
				}
			}
			else if (AlarmLoopClip != null)
			{
				closingSource.Stop();

				if (!alarmSource.isPlaying)
				{
					alarmSource.clip = AlarmLoopClip;
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