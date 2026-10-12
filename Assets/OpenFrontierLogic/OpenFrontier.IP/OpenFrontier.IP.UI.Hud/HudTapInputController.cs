using DigitalRubyShared;
using OpenFrontier.IP.UI.Screens.Hud;
using UnityEngine;

namespace OpenFrontier.IP.UI.Hud
{
	public class HudTapInputController : MonoBehaviour
	{
		public HudScreen Hud;

		private TapGestureRecognizer tapGesture;

		private void OnEnable()
		{
			tapGesture = new TapGestureRecognizer
			{
				MaximumNumberOfTouchesToTrack = 1
			};
			tapGesture.StateUpdated += TapGesture_StateUpdated;
			tapGesture.PlatformSpecificView = gameObject;
			FingersScript.Instance.AddGesture(tapGesture);
		}

		private void TapGesture_StateUpdated(GestureRecognizer gesture)
		{
			if (Hud.ComponentManager.GetHudComponentActive(HudComponent.TargetSprites) && gesture.State == GestureRecognizerState.Ended)
			{
				if (!UI.IsMobileDevice && gesture.IsRightMouseButton())
				{
					Hud.TargetSelectRightButton();
				}
				else
				{
					Hud.TargetSelectLeftButton(new Vector2(gesture.FocusX, gesture.FocusY));
				}
			}
		}

		private void OnDisable()
		{
			FingersScript.Instance.RemoveGesture(tapGesture);
		}
	}
}
