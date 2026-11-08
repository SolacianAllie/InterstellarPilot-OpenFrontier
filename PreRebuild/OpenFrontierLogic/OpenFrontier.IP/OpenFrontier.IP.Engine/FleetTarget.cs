using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class FleetTarget
	{
		public Vector3 Position = Vector3.zero;

		public Quaternion TargetRotation = Quaternion.identity;
	}
}
