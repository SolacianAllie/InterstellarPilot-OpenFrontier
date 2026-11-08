using UnityEngine;

namespace OpenFrontier.IP
{
	public class TEST_ONENABLE : MonoBehaviour
	{
		private void OnEnable()
		{
			Debug.LogError("Foo", this);
		}
	}
}
