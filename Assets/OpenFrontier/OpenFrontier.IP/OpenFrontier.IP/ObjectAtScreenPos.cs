using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP
{
	public class ObjectAtScreenPos : MonoBehaviour
	{
		public enum CoorindateMode
		{
			Relative,
			Absolute
		}

		public CoorindateMode Coordinates = CoorindateMode.Absolute;

		public Vector3 ScreenPos = Vector3.zero;

		private void Update()
		{
			switch (Coordinates)
			{
			case CoorindateMode.Absolute:
				gameObject.transform.position = GameController.Instance.MainCamera.ScreenToWorldPoint(GetPos());
				break;
			case CoorindateMode.Relative:
				gameObject.transform.position = GameController.Instance.MainCamera.ScreenToWorldPoint(Vector3.Scale(GetPos(), new Vector3(Screen.width, Screen.height, 1f)));
				break;
			}
		}

		private Vector3 GetPos()
		{
			Vector3 screenPos = ScreenPos;
			switch (Coordinates)
			{
			case CoorindateMode.Relative:
				screenPos.y = 1f - screenPos.y;
				break;
			case CoorindateMode.Absolute:
				screenPos.y = (float)Screen.height - screenPos.y;
				break;
			}
			return screenPos;
		}
	}
}
