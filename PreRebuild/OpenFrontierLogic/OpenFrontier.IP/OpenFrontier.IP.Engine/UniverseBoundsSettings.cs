using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UniverseBoundsSettings : MonoBehaviour
	{
		public int SectorSize = 16000;

		public float MaxUnitDistanceFromOriginLowerBound = 6500f;

		public float MaxUnitDistanceFromOriginUpperBound = 7000f;

		public float SectorSizeOverTwo => (float)SectorSize / 2f;
	}
}
