using UnityEngine;

namespace OpenFrontier.IP.Extensions
{
	public static class UnityExtensions
	{
		public static void ToWorldOrigin(this Transform transform)
		{
			transform.position = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}
	}
}
