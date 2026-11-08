using UnityEngine;

namespace OpenFrontier.IP.UI
{
	public class LoadingWidgetUI : MonoBehaviour
	{
		public Transform TransformToRotate;

		public float TimeBetweenRotation = 0.1f;

		private float lastRotateTime;

		private void Update()
		{
			if (Time.realtimeSinceStartup > lastRotateTime + TimeBetweenRotation)
			{
				RotateWidget();
				lastRotateTime = Time.realtimeSinceStartup;
			}
		}

		private void RotateWidget()
		{
			TransformToRotate.Rotate(new Vector3(0f, 0f, -60f));
		}
	}
}
