using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.UI
{
	[RequireComponent(typeof(ZoomCameraButton))]
	public class ZoomCameraButton : MonoBehaviour
	{
		public float Change = 1f;

		private RepeatableButton repeatableButton;

		private void Awake()
		{
			repeatableButton = GetComponent<RepeatableButton>();
		}

		private void Update()
		{
			if (repeatableButton.IsPressed)
			{
				EngineASX.Instance.HudCamera.CamUnitDistance += Change * RealTime.deltaTime;
				EngineASX.Instance.HudCamera.CameraElevation += Change * RealTime.deltaTime;
			}
		}
	}
}
