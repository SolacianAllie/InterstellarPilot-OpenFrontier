using UnityEngine;
using Input = OpenFrontier.LegacyInput;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapZoomer : MonoBehaviour
	{
		public float ChangeZoomRate = 16f;

		public float MaxZoom = 60f;

		public float MinZoom = 3f;

		public float MouseWheelZoomRate = 10f;

		public SectorMap SectorMap;

		public RepeatableButton ZoomInButton;

		public RepeatableButton ZoomOutButton;

		public void ChangeZoom(float change)
		{
			SetZoomLevel(SectorMap.WorldToScreenConversion + change);
		}

		public void Tick()
		{
			if (SectorMap != null)
			{
				UpdateZoomLevel();
			}
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
			float axis = Input.GetAxis("Mouse ScrollWheel");
			if (axis != 0f)
			{
				SetZoomLevel(SectorMap.WorldToScreenConversion + axis * MouseWheelZoomRate);
			}
		}

		private void ZoomOut()
		{
			ChangeZoom((0f - ChangeZoomRate) * RealTime.deltaTime);
		}

		private void ZoomIn()
		{
			ChangeZoom(ChangeZoomRate * RealTime.deltaTime);
		}

		public void SetMinZoom()
		{
			SetZoomLevel(MinZoom);
		}

		public void SetMaxZoom()
		{
			SetZoomLevel(MaxZoom);
		}

		public void SetZoomLevel(float zoom)
		{
			float worldToScreenConversion = SectorMap.WorldToScreenConversion;
			float num = Mathf.Clamp(zoom, MinZoom, MaxZoom);
			if (num != worldToScreenConversion)
			{
				Vector3 viewWorldPosition = SectorMap.GetViewWorldPosition();
				SectorMap.WorldToScreenConversion = num;
				SectorMap.SetViewWorldPosition(viewWorldPosition);
			}
		}
	}
}
