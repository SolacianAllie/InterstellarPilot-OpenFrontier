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

		// Off by default so parts authored before position support existed are
		// strictly untouched: a missing prefab key reads false.
		public bool ControlPosition;

		// Local offset from the rest position at full lerp.
		public Vector3 PositionOffset;

		// The part's localPosition as authored, noted down before the first
		// lerp that moves it - never Vector3.zero, so unconfigured data cannot
		// drag a part to its pivot's origin.
		[NonSerialized]
		public Vector3 RestPosition;

		[NonSerialized]
		public bool RestPositionCaptured;
	}
}
