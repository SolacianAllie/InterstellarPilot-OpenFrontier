using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class PositionAtCamera : MonoBehaviour
	{
		public void Update()
		{
			if (GameController.Instance != null && GameController.Instance.MainCamera != null)
			{
				transform.position = GameController.Instance.MainCamera.transform.position;
			}
		}
	}
}
