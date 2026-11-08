using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.InGameMenu
{
	public class CameraInGameMenu : MonoBehaviour
	{
		public Button StartCinematicButton;

		public Button ResetCameraButton;

		private void Awake()
		{
			StartCinematicButton.onClick.AddListener(StartCinematicButtonClick);
			ResetCameraButton.onClick.AddListener(ResetCameraButtonClick);
		}

		private void StartCinematicButtonClick()
		{
			if (EngineASX.Instance.PlayerUnit != null && EngineASX.Instance.PlayerUnit.IsValidAndNotDestroyed)
			{
				EngineASX.Instance.StartCustomCinematic(allowTogglePause: true);
			}
		}

		private void ResetCameraButtonClick()
		{
			EngineASX.Instance.HudCamera.ResetAll();
		}
	}
}
