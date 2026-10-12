using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class CloakComponent : TurretComponent
	{
		public CloakComponentClass CloakComponentClass;

		private float cloakProgress;

		[SerializeField]
		private CloakState state;

		public override ComponentClass ComponentClass => CloakComponentClass;

		public CloakState State
		{
			get
			{
				return state;
			}
			set
			{
				if (state != value)
				{
					state = value;
					if (Unit != null)
					{
						ApplyState();
					}
					else if (state == CloakState.Cloaked || state == CloakState.Cloaking)
					{
						Debug.LogError("Cannot apply cloak state. No unit attached", this);
					}
				}
			}
		}

		public float CloakProgress
		{
			get
			{
				return cloakProgress;
			}
			set
			{
				if (cloakProgress != value)
				{
					if (state == CloakState.Cloaked || state == CloakState.Decloaked)
					{
						throw new ArgumentException("Cannot manually set cloak state when state is cloaked or decloaked");
					}
					cloakProgress = value;
					if (cloakProgress <= 0f)
					{
						State = CloakState.Decloaked;
					}
					else if (cloakProgress >= 1f)
					{
						State = CloakState.Cloaked;
					}
					Unit.UpdateDetectionRadius();
				}
			}
		}

		public bool CanCloak
		{
			get
			{
				if (state == CloakState.Decloaked)
				{
					return UnitComponents.Capacitor.Charge >= CloakComponentClass.EnergyCost;
				}
				return false;
			}
		}

		public override bool IsFiring => state != CloakState.Decloaked;

		public override bool IsReadyToFire(Unit target, bool ignoreFiringArc = false)
		{
			if (state == CloakState.Cloaked || state == CloakState.Decloaked)
			{
				return base.IsReadyToFire(target, ignoreFiringArc);
			}
			return false;
		}

		public override bool CanCancelFire()
		{
			return state == CloakState.Cloaked;
		}

		public void StartCloak()
		{
			Fire(null);
		}

		public void StartDecloak()
		{
			CancelFiring();
		}

		public void PlayCloakAudio()
		{
			AudioSource cloakAudio = GetCloakAudio();
			if (cloakAudio != null)
			{
				AudioSource audioSource = Engine.PlayPooledAudioSource(cloakAudio, transform.position);
				audioSource.transform.SetParent(transform);
				audioSource.transform.localPosition = Vector3.zero;
			}
		}

		public void PlayDecloakAudio()
		{
			AudioSource decloakAudio = GetDecloakAudio();
			if (decloakAudio != null)
			{
				AudioSource audioSource = Engine.PlayPooledAudioSource(decloakAudio, transform.position);
				audioSource.transform.SetParent(transform);
				audioSource.transform.localPosition = Vector3.zero;
			}
		}

		public void UpdateCloak(float elapsedTime)
		{
			switch (state)
			{
			case CloakState.Cloaking:
				CloakProgress += 1f / CloakComponentClass.CloakTime * elapsedTime;
				break;
			case CloakState.Decloaking:
				CloakProgress -= 1f / CloakComponentClass.DecloakTime * elapsedTime;
				break;
			}
		}

		public float GetDetectionRangeMultiplier()
		{
			switch (state)
			{
			case CloakState.Decloaked:
				return 1f;
			case CloakState.Cloaking:
			case CloakState.Decloaking:
				return 1f - cloakProgress * (1f - CloakComponentClass.DetectionMultiplier);
			case CloakState.Cloaked:
				return CloakComponentClass.DetectionMultiplier;
			default:
				return 1f;
			}
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.CloakComponent = this;
			if (state != CloakState.Decloaked)
			{
				ApplyState();
			}
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			if (state != CloakState.Decloaked)
			{
				State = CloakState.Decloaked;
			}
			base.onDetachedFromUnit(unit);
			unit.CloakComponent = null;
		}

		protected override void onFired(Unit target)
		{
			base.onFired(target);
			if (state == CloakState.Decloaked)
			{
				State = CloakState.Cloaking;
			}
		}

		protected override void OnFiringCancelled()
		{
			if (state == CloakState.Cloaked)
			{
				State = CloakState.Decloaking;
			}
		}

		private void ApplyState()
		{
			float num = CloakProgress;
			switch (state)
			{
			case CloakState.Decloaked:
				AffectOtherComponents(enabled: true);
				num = 0f;
				ChargedEnergy = 0f;
				break;
			case CloakState.Cloaked:
				num = 1f;
				break;
			case CloakState.Decloaking:
				if (Unit.IsActiveInEngine)
				{
					PlayDecloakAudio();
				}
				ChargedEnergy = 0f;
				break;
			case CloakState.Cloaking:
				AffectOtherComponents(enabled: false);
				if (Unit.IsActiveInEngine)
				{
					PlayCloakAudio();
				}
				break;
			}
			cloakProgress = num;
			Unit.UpdateDetectionRadius();
		}

		private AudioSource GetCloakAudio()
		{
			return Engine.DefaultCloakAudioSource;
		}

		private AudioSource GetDecloakAudio()
		{
			return Engine.DefaultDecloakAudioSource;
		}

		private void AffectOtherComponents(bool enabled)
		{
			foreach (TurretComponent turret in UnitComponents.Turrets)
			{
				if (!enabled)
				{
					turret.CancelFiring();
				}
			}
		}

		public float GetCloakMinAlpha()
		{
			if (Unit.IsPlayerOrAllied())
			{
				return GameController.Instance.GameSettings.VideoSettings.CloakMinAlphaForPlayer;
			}
			return GameController.Instance.GameSettings.VideoSettings.CloakMinAlpha;
		}

		public override bool CanChargeTurret()
		{
			return true;
		}
	}
}
