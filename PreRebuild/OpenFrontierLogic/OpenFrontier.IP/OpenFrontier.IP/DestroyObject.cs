using UnityEngine;

namespace OpenFrontier.IP
{
	public class DestroyObject : MonoBehaviour
	{
		private void Awake()
		{
			Object.Destroy(gameObject);
		}
	}
}
