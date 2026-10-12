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

		private float lastTimeThrottleZerod = float.MinValue;

		private bool lastThrottleActive;

		private void OnEnable()
		{
			SetLerp(GetDesiredLerp());
		}

		private void Update()
		{
			float desiredLerp = GetDesiredLerp();
			if (desiredLerp != currentLerp)
			{
				float lerp = Mathf.MoveTowards(currentLerp, desiredLerp, ChangeRate * Time.deltaTime);
				SetLerp(lerp);
			}
		}

		private float GetDesiredLerp()
		{
			bool flag = IsThrottleActive();
			if (flag)
			{
				lastThrottleActive = flag;
				return 1f;
			}
			if (flag != lastThrottleActive)
			{
				lastTimeThrottleZerod = Time.time;
			}
			lastThrottleActive = flag;
			if (Time.time - lastTimeThrottleZerod > 8f)
			{
				return 0f;
			}
			return 1f;
		}

		private bool IsThrottleActive()
		{
			if (ActiveUnit.GetCurrentTurn() == 0f)
			{
				return ActiveUnit.GetEffectiveThrottle() > 0f;
			}
			return true;
		}

		public void SetLerp(float lerp)
		{
			if (lerp == currentLerp)
			{
				return;
			}
			foreach (ActiveUnitThrottleControlledPart part in Parts)
			{
				part.TargetTransform.localRotation = Quaternion.Euler(Vector3.Lerp(part.NormalRotation, part.TargetRotation, lerp));
			}
			currentLerp = lerp;
		}
	}
}
