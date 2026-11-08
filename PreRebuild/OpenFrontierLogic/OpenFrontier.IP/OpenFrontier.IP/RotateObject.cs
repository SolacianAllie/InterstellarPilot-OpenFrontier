using UnityEngine;

namespace OpenFrontier.IP
{
	public class RotateObject : MonoBehaviour
	{
		public Space RelativeSpace = Space.Self;

		public Vector3 RotationRate = new Vector3(0f, 90f, 0f);

		public void Update()
		{
			transform.Rotate(RotationRate * Time.deltaTime, RelativeSpace);
		}
	}
}
