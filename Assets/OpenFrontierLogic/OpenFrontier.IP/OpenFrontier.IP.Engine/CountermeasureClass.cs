using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class CountermeasureClass : MonoBehaviour
	{
		public float Effectiveness = 0.5f;

		public bool ExpendAfterDisruption;

		public float MaxDisruptionRange = 100f;

		public float MinTimeBeforeDisruption = 0.4f;

		public float Drag = 0.3f;
	}
}
