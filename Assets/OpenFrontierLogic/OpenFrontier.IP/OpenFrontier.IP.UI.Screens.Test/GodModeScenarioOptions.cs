using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Test
{
	public class GodModeScenarioOptions : MonoBehaviour
	{
		public Toggle AllowStationCaptureToggle;

		public Toggle AllowAbandonShipToggle;

		private void Awake()
		{
			AllowAbandonShipToggle.onValueChanged.AddListener((bool value) =>
			{
				EngineASX.Instance.World.ScenarioOptions.AllowAbandonShip = value;
			});
			AllowStationCaptureToggle.onValueChanged.AddListener((bool value) =>
			{
				EngineASX.Instance.World.ScenarioOptions.AllowStationCapture = value;
			});
		}

		private void OnEnable()
		{
			AllowStationCaptureToggle.SetIsOnWithoutNotify(EngineASX.Instance.World.ScenarioOptions.AllowStationCapture);
			AllowAbandonShipToggle.SetIsOnWithoutNotify(EngineASX.Instance.World.ScenarioOptions.AllowAbandonShip);
		}
	}
}
