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

		/// <summary>
		/// The reach every scanner has, before its own hull contributes
		/// anything and before the size of what it is looking at.
		/// </summary>
		/// <remarks>
		/// This is a real gameplay constant and it lives here rather than being
		/// read out of HudSettings, where a display pad of the same size used to
		/// sit.
		///
		/// It exists because of a mistake worth recording. ComponentHolder's
		/// ScanRange field defaults to 1500, so it is natural to assume a scanner
		/// reaches 1500 - and every ship prefab says otherwise. Eighty-five of
		/// eighty-six ship prefabs author it explicitly, and they author it as 50,
		/// or as 1 on twenty-six of them. The default is never used.
		///
		/// An earlier version of the sensor work built its whole model on that
		/// assumption and took a scanner's reach to be about 1500 when it was
		/// really 50. Detection collapsed by more than an order of magnitude and
		/// the drawn circle with it.
		///
		/// So the reach is this plus the hull's own ScanRange, which is the same
		/// 1550 the sector map circle was effectively drawing all along - the
		/// circle was ScanRange plus a 1500 display pad, and the detection test
		/// was ScanRange plus a target's range. The two disagreed by 400 metres
		/// and the circle was the one that was right.
		/// </remarks>
		public float SensorBaseRange = 1500f;
	}
}
