using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units.ActiveUnits
{
	public class ActiveUnitThrottleControlledParts : MonoBehaviour
	{
		public ActiveUnit ActiveUnit;

		public List<ActiveUnitThrottleControlledPart> Parts;

		private float currentLerp = -1f;

		public float ChangeRate = 1f;

		private void OnEnable()
		{
			SetLerp(IsMoving() ? 1f : 0f);
		}

		private void Update()
		{
			float desiredLerp = IsMoving() ? 1f : 0f;
			if (desiredLerp != currentLerp)
			{
				float lerp = Mathf.MoveTowards(currentLerp, desiredLerp, ChangeRate * Time.deltaTime);
				SetLerp(lerp);
			}
		}

		// Parts deploy on motion now: out while the hull is above zero
		// speed, in the moment it stops. Two old triggers are gone - turning
		// used to deploy the parts on its own (a parked ship could sit there
		// with nozzles out), and a closed throttle held them open for an
		// eight second count before folding. Neither survives: only the
		// hull's actual movement moves them.
		//
		// The throttle clause backs the speed read up, because a speed read
		// cannot answer for every unit: a null rigidbody reads as stopped,
		// and a remote ship's motion is written by UnitInterpolator straight
		// onto the transform, not into its rigidbody. GetEffectiveThrottle
		// answers zero for a dead engine, so a ship with the throttle pushed
		// up but no power never shows deployed parts.
		private bool IsMoving()
		{
			if (ActiveUnit.Unit != null && ActiveUnit.Unit.CurrentSpeed > 0f)
			{
				return true;
			}
			return ActiveUnit.GetEffectiveThrottle() > 0f;
		}

		public void SetLerp(float lerp)
		{
			if (lerp == currentLerp)
			{
				return;
			}
			foreach (ActiveUnitThrottleControlledPart part in Parts)
			{
				if (part == null || part.TargetTransform == null)
				{
					continue;
				}
				if (part.ControlPosition)
				{
					CaptureRestPosition(part);
					part.TargetTransform.localPosition = part.RestPosition + part.PositionOffset * lerp;
				}
				part.TargetTransform.localRotation = Quaternion.Euler(Vector3.Lerp(part.NormalRotation, part.TargetRotation, lerp));
			}
			currentLerp = lerp;
		}

		// Notes a part's rest position down on first use, before anything
		// writes it, so lerp 0 always restores the pose it was authored in.
		private void CaptureRestPosition(ActiveUnitThrottleControlledPart part)
		{
			if (!part.RestPositionCaptured)
			{
				part.RestPosition = part.TargetTransform.localPosition;
				part.RestPositionCaptured = true;
			}
		}
	}
}
