using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class ElevateCameraButton : MonoBehaviour
	{
		public float Change = 1f;

		private EngineASX engine;

		private Button hudButton;

		private void Awake()
		{
			engine = EngineASX.Instance;
		}

		private void Start()
		{
			hudButton = GetComponent<Button>();
			hudButton.onClick.AddListener(hudButton_Pressed);
		}

		private void hudButton_Pressed()
		{
			engine.HudCamera.CameraElevation += Change * Time.deltaTime;
		}
	}
}
