using UnityEngine;

namespace OpenFrontier.IP
{
	public class TimedDestruction : MonoBehaviour
	{
		public float Timeout = 1f;

		public bool DetachChildren;

		private void Awake()
		{
			Invoke("DestroyNow", Timeout);
		}

		private void DestroyNow()
		{
			if (DetachChildren)
			{
				transform.DetachChildren();
			}
			Object.Destroy(gameObject);
		}
	}
}
