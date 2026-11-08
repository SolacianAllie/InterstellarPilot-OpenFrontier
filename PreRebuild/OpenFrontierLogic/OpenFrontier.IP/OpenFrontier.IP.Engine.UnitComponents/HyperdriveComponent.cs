using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class HyperdriveComponent : TurretComponent
	{
		public enum HyperDriveState
		{
			Deactivated,
			Charging,
			Activated
		}

		private float chargeProgress;

		public HyperdriveClass HyperdriveClass;

		private HyperDriveState state;

		private Vector3 velocity = Vector3.zero;

		public float ChargeProgress
		{
			get
			{
				return chargeProgress;
			}
			set
			{
				chargeProgress = value;
			}
		}

		public HyperDriveState State
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
					switch (state)
					{
					case HyperDriveState.Deactivated:
						chargeProgress = 0f;
						break;
					case HyperDriveState.Activated:
						chargeProgress = 1f;
						break;
					}
				}
			}
		}

		public override bool IsFiring => state != HyperDriveState.Deactivated;

		public bool IsActivated => state == HyperDriveState.Activated;

		public override bool CanCancelFire()
		{
			return IsActivated;
		}

		protected override void onFired(Unit target)
		{
			base.onFired(target);
			State = HyperDriveState.Charging;
			chargeProgress = 0f;
		}

		protected override void OnFiringCancelled()
		{
			base.OnFiringCancelled();
			State = HyperDriveState.Deactivated;
			chargeProgress = 0f;
		}

		protected override void onAttachedToUnit(UnitComponentHolder unit)
		{
			base.onAttachedToUnit(unit);
			unit.Hyperdrive = this;
		}

		protected override void onDetachedFromUnit(UnitComponentHolder unit)
		{
			base.onDetachedFromUnit(unit);
			unit.Hyperdrive = null;
		}

		protected override void rechargeTick(float elapsedTime)
		{
			base.rechargeTick(elapsedTime);
			switch (state)
			{
			case HyperDriveState.Charging:
				chargeProgress += elapsedTime / HyperdriveClass.ChargeTime;
				if (chargeProgress >= 1f)
				{
					chargeProgress = 1f;
					State = HyperDriveState.Activated;
				}
				break;
			case HyperDriveState.Activated:
				Unit.transform.position += velocity * elapsedTime;
				break;
			}
		}
	}
}
