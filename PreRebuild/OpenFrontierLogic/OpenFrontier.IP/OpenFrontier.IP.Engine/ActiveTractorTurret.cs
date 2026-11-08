using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class ActiveTractorTurret : ActiveLaserTurret
	{
		private Vector3 targetVector = Vector3.zero;

		private Cargo tractorCargo;

		public TractorTurretComponent TractorTurret;

		public TractorTurretClass TractorTurretClass;

		public bool IsRetracting
		{
			get
			{
				if (tractorCargo != null)
				{
					return LaserState == ActiveLaserState.HitTarget;
				}
				return false;
			}
		}

		public bool IsPullingUnit => TractorTurret.IsPullingUnit;

		public override bool CanFireAt(Unit target, bool ignoreFiringArc = false)
		{
			if (base.CanFireAt(target, ignoreFiringArc))
			{
				if (!(target.Tractorer == null))
				{
					return target.Tractorer == TractorTurret;
				}
				return true;
			}
			return false;
		}

		public void NotifyJointBroken(float breakForce)
		{
			if (LogWrapper.LogMsgs)
			{
				LogWrapper.Log($"{this}: Spring joint broken. Force: {breakForce}", this, 1);
			}
			if (LaserState == ActiveLaserState.HitTarget)
			{
				LaserState = ActiveLaserState.FadeOut;
			}
		}

		protected override void onInit()
		{
			base.onInit();
			TractorTurretClass = (TractorTurretClass)TurretComponent.TurretClass;
			RestoreTractorBeam();
		}

		public void RestoreTractorBeam()
		{
			if (TractorTurret.IsPullingUnit)
			{
				lastFireTarget = TractorTurret.TractorTarget;
				FireState = TurretFireState.Firing;
				LaserState = ActiveLaserState.HitTarget;
				hasHitTarget = true;
				lastLaserHitRelativePosition = Vector3.zero;
				Vector3 value = lastFireTarget.transform.position - transform.position;
				lastDistToTarget = value.magnitude;
				float num = Mathf.Max(LaserTurretClass.MaxFiringRange, Engine.GameSettings.TractorBeamSettings.TractorBreakDistance) * 0.8f;
				if (lastDistToTarget > num)
				{
					Vector3 position = transform.position + Vector3.Normalize(value) * num;
					position.y = 0f;
					lastFireTarget.transform.position = position;
				}
			}
		}

		protected override void OnLaserHitTarget()
		{
			if (lastFireTarget.Tractorer == null)
			{
				if (Unit.GetComponent<Rigidbody>() != null && LastFireTarget != null && LastFireTarget.GetComponent<Rigidbody>() != null)
				{
					tractorCargo = lastFireTarget.gameObject.GetComponent<Cargo>();
					if (tractorCargo != null)
					{
						SetTargetVector();
					}
				}
				TractorTurret.TractorTarget = lastFireTarget;
				LaserState = ActiveLaserState.HitTarget;
			}
			else
			{
				LaserState = ActiveLaserState.NotActive;
			}
		}

		protected override void update()
		{
			base.update();
			if (LaserState == ActiveLaserState.NotActive)
			{
				return;
			}
			if (lastFireTarget != null && lastFireTarget.IsTargettable(null))
			{
				if (lastDistToTarget > Mathf.Max(LaserTurretClass.MaxFiringRange, Engine.GameSettings.TractorBeamSettings.TractorBreakDistance))
				{
					CancelFiring();
				}
			}
			else
			{
				CancelFiring();
			}
		}

		protected override void OnFireStateChanged(TurretFireState oldFireState)
		{
			if (oldFireState == TurretFireState.Firing)
			{
				unit.ResetMass();
			}
			base.OnFireStateChanged(oldFireState);
		}

		protected override void onTargetChanged(Unit oldTarget, Unit newTarget)
		{
			if (LaserState == ActiveLaserState.HitTarget)
			{
				LaserState = ActiveLaserState.FadeOut;
			}
		}

		protected override void OnLaserStateChanged(ActiveLaserState oldState)
		{
			base.OnLaserStateChanged(oldState);
			if (LaserState == ActiveLaserState.FadeOut)
			{
				LaserState = ActiveLaserState.NotActive;
			}
		}

		protected override void ApplyEnergyCostPerSecond(float elapsedTime)
		{
			if (lastFireTarget != null && lastFireTarget.UnitType != UnitType.Cargo)
			{
				base.ApplyEnergyCostPerSecond(elapsedTime);
			}
		}

		private void FixedUpdate()
		{
			if (!IsFiring || LaserState != ActiveLaserState.HitTarget)
			{
				return;
			}
			if (tractorCargo != null)
			{
				SetTargetVector();
				float magnitude = targetVector.magnitude;
				magnitude -= Engine.GameSettings.TractorBeamSettings.TractorBeamRetractSpeed * Time.deltaTime;
				targetVector = targetVector.normalized * magnitude;
				if (magnitude < 1f)
				{
					OnCargoTractored();
					return;
				}
				tractorCargo.Unit.RBody.MovePosition(transform.position + targetVector);
				if (unit.RBody != null)
				{
					tractorCargo.Unit.RBody.linearVelocity = unit.RBody.linearVelocity;
				}
			}
			else
			{
				if (!TractorTurret.IsPullingUnit || !(lastFireTarget != null))
				{
					return;
				}
				ActiveUnit activeUnit = lastFireTarget.ActiveUnit;
				if (!(activeUnit != null) || !(lastFireTarget.RBody != null))
				{
					return;
				}
				UnitEngineComponent engineComponent = UnitComponents.EngineComponent;
				if (engineComponent != null)
				{
					Vector3 value = lastFireTarget.transform.position - transform.position;
					Vector3 vector = Vector3.Normalize(value);
					float magnitude2 = value.magnitude;
					float tractorMinForceApplyDist = Engine.GameSettings.TractorBeamSettings.TractorMinForceApplyDist;
					float tractorMaxForceApplyDist = Engine.GameSettings.TractorBeamSettings.TractorMaxForceApplyDist;
					if (magnitude2 > tractorMinForceApplyDist)
					{
						float num = Mathf.Clamp01((magnitude2 - tractorMinForceApplyDist) / (tractorMaxForceApplyDist - tractorMinForceApplyDist));
						unit.RBody.mass = unit.Mass + num * lastFireTarget.Mass;
						float num2 = engineComponent.CalculateRelativeForceAndReduceCapacitor(reduceChargeEnergy: false, Time.deltaTime) * num;
						activeUnit.UnitRigidBody.AddForce(-vector * num2, ForceMode.Impulse);
					}
				}
			}
		}

		private void OnCargoTractored()
		{
			TractorTurret.TractorInCargo(tractorCargo);
			CancelFiring();
		}

		private void SetTargetVector()
		{
			targetVector = tractorCargo.gameObject.transform.position - transform.position;
		}

		protected override Quaternion GenerateInaccuracy()
		{
			return Quaternion.identity;
		}

		protected override bool DetectTargetUnitCollision(Unit unit, ref Vector3 vectorToTarget, out float distance, out Vector3 intersectPoint)
		{
			distance = vectorToTarget.magnitude;
			float num = unit.Radius * 0.4f;
			intersectPoint = Vector3.zero;
			if (laserLength > distance - num)
			{
				intersectPoint = transform.position + Vector3.Normalize(vectorToTarget) * (distance - num);
				return true;
			}
			return false;
		}
	}
}
