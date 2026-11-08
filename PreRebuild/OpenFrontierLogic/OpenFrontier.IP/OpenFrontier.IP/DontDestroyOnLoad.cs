using UnityEngine;

namespace OpenFrontier.IP
{
	public class DontDestroyOnLoad : MonoBehaviour
	{
		private void Awake()
		{
			Object.DontDestroyOnLoad(gameObject);
		}
	}
}
