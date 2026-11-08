using UnityEngine;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI.Screens.UniverseMap
{
	public class UniverseMapZoomer : MonoBehaviour
	{
		public float ButtonChangeZoomRate = 16f;

		public RepeatableButton ZoomInButton;

		public RepeatableButton ZoomOutButton;

		public UniverseMapScreen UniverseMapScreen;

		public float MouseScrollZoomRate = 0.1f;

		public float MinZoom => UniverseMapScreen.MinScaleFactor;

		public float MaxZoom => UniverseMapScreen.MaxScaleFactor;

		public void UpdateZoom()
		{
			UpdateZoomLevel();
		}

		private void UpdateZoomLevel()
		{
			if (ZoomInButton.IsPressed)
			{
				ZoomIn();
			}
			if (ZoomOutButton.IsPressed)
			{
				ZoomOut();
			}
			MouseScroll();
		}

		private void MouseScroll()
		{
			float scaleFactor = UniverseMapScreen.ScaleFactor + Input.mouseScrollDelta.y * MouseScrollZoomRate;
			UniverseMapScreen.SetScaleFactor(scaleFactor);
		}

		private void ZoomOut()
		{
			ChangeZoom((0f - ButtonChangeZoomRate) * RealTime.deltaTime);
		}

		private void ZoomIn()
		{
			ChangeZoom(ButtonChangeZoomRate * RealTime.deltaTime);
		}

		public void SetMinZoom()
		{
			SetZoomLevel(MinZoom);
		}

		public void SetMaxZoom()
		{
			SetZoomLevel(MaxZoom);
		}

		public void ChangeZoom(float change)
		{
			SetZoomLevel(UniverseMapScreen.ScaleFactor + change);
		}

		public void ResetZoom()
		{
			SetZoomLevel(UniverseMapScreen.DefaultScaleFactor);
		}

		public void SetZoomLevel(float scaleFactor)
		{
			UniverseMapScreen.SetScaleFactor(scaleFactor);
		}
	}
}
