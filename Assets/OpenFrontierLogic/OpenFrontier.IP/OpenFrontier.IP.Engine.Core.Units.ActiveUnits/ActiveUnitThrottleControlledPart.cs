using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Core.Units.ActiveUnits
{
	[Serializable]
	public class ActiveUnitThrottleControlledPart
	{
		public Transform TargetTransform;

		public Vector3 NormalRotation;

		public Vector3 TargetRotation;
	}
}
