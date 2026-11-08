using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class LockCameraButton : MonoBehaviour
	{
		private HudScreen hud;

		private Button button;

		public Image CameraModeImage;

		public Sprite LockCameraSprite;

		public Sprite FixedCameraSprite;

		public Sprite FreeCameraSprite;

		public HudScreen Hud => hud;

		private void Awake()
		{
			hud = UnityObjectHelper.FindInParentsOrSelf<HudScreen>(gameObject);
			button = GetComponent<Button>();
			button.onClick.AddListener(hudButton_Pressed);
		}

		private void Start()
		{
			Refresh();
		}

		private void Update()
		{
			RefreshCameraModeImageSprite();
		}

		private void OnEnable()
		{
			Refresh();
		}

		private void Refresh()
		{
			RefreshCameraModeImageSprite();
		}

		private void RefreshCameraModeImageSprite()
		{
			if (!(hud == null))
			{
				CameraModeImage.sprite = GetCameraModeSprite();
				CameraModeImage.enabled = CameraModeImage.sprite != null;
			}
		}

		private Sprite GetCameraModeSprite()
		{
			return hud.CameraMode switch
			{
				HudCameraMode.FixedForward => FixedCameraSprite, 
				HudCameraMode.Free => FreeCameraSprite, 
				HudCameraMode.LockTarget => LockCameraSprite, 
				_ => null, 
			};
		}

		private void hudButton_Pressed()
		{
			switch (hud.CameraMode)
			{
			case HudCameraMode.Free:
				hud.CameraMode = HudCameraMode.FixedForward;
				break;
			case HudCameraMode.FixedForward:
				hud.CameraMode = HudCameraMode.LockTarget;
				break;
			case HudCameraMode.LockTarget:
				hud.CameraMode = HudCameraMode.FixedForward;
				break;
			}
		}
	}
}
