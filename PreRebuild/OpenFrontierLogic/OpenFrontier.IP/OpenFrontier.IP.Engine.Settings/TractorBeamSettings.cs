using System;
using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class TractorBeamSettings : MonoBehaviour
	{
		[NonSerialized]
		public UnitType TractorableUnitTypes = UnitType.Ship | UnitType.Cargo;

		public float TractorBeamRetractSpeed = 5f;

		public float TractorBreakDistance = 125f;

		public float TractorMaxForceApplyDist = 100f;

		public float TractorMinForceApplyDist = 50f;
	}
}
