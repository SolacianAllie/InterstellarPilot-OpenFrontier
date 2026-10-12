using OpenFrontier.IP.UI.Extensions;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapGridLineItem : SectorMapItem
	{
		public enum OrientationMode
		{
			Horizontal,
			Vertical
		}

		public OrientationMode Orientation;

		public Vector3 WorldPosition;

		public float Thickness = 1f;

		public override float MinDrawSize => Thickness;

		protected override Vector3 GetWorldPosition()
		{
			return WorldPosition;
		}

		protected override Vector3 GetRotation()
		{
			if (Orientation == OrientationMode.Horizontal)
			{
				return new Vector3(0f, 0f, 90f);
			}
			return Vector3.zero;
		}

		protected override void AppySize()
		{
			float width = Image.rectTransform.GetWidth();
			float mapWorldRadius = SectorMap.GetMapWorldRadius();
			Image.rectTransform.SetHeight(mapWorldRadius * 2f * SectorMap.WorldToScreenConversion);
			Image.rectTransform.SetWidth(width);
		}
	}
}
