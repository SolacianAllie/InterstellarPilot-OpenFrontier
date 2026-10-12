using UnityEngine;

namespace OpenFrontier.IP.Engine.Settings
{
	public class IntelSettings : MonoBehaviour
	{
		public float NonStaticTargetPersistTime = 20f;

		public float NonStaticMaxTimeBeforeDisposal = 600f;

		public float NonStaticTargetPersistTimeWhenPlayerPilotting = 5f;

		public float NonStaticTargetPersistTimeActiveSector = 5f;

		public float UnitScanWhenActiveFrequency = 2f;

		public float UnitScanWhenInactiveFrequency = 5f;

		public float MaxScanRange = 2000f;
	}
}
