using UnityEngine;

namespace OpenFrontier.Unity.Utils
{
	public class LayerMaskUtility
	{
		public static int CombineMasks(LayerMask m1, LayerMask m2)
		{
			return (int)m1 | (int)m2;
		}

		public static int CombineMasks(int maskValue, LayerMask m2)
		{
			return maskValue | (int)m2;
		}
	}
}
