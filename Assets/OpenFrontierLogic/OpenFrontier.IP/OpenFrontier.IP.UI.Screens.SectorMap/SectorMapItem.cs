using OpenFrontier.IP.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.SectorMap
{
	public class SectorMapItem : MonoBehaviour
	{
		private bool isSelectable;

		public Image Image;

		public SectorMap SectorMap;

		protected virtual bool RealWorldSize => true;

		public virtual float MinDrawSize => 0f;

		public bool IsSelectable
		{
			get
			{
				return isSelectable;
			}
			set
			{
				isSelectable = value;
			}
		}

		public virtual void Refresh()
		{
			Reposition();
		}

		public void Reposition()
		{
			bool flag = ShouldShowSprite();
			if (flag)
			{
				OnReposition();
			}
			SetActivated(flag);
		}

		public void PlaceAtWorldPosition(Vector3 worldPosition)
		{
			transform.localPosition = SectorMap.ConvertWorldToScreen(worldPosition);
		}

		protected virtual void OnReposition()
		{
			Vector3 rotation = GetRotation();
			Image.transform.localRotation = Quaternion.Euler(rotation);
			Vector3 worldPosition = GetWorldPosition();
			PlaceAtWorldPosition(worldPosition);
			AppySize();
		}

		protected virtual void AppySize()
		{
			Vector2? vector = GetSize();
			if (vector.HasValue)
			{
				if (RealWorldSize)
				{
					vector *= SectorMap.WorldToScreenConversion;
				}
				float num = vector.Value.x;
				float num2 = vector.Value.y;
				if (MinDrawSize > 0f)
				{
					num = Mathf.Max(num, MinDrawSize);
					num2 = Mathf.Max(num2, MinDrawSize);
				}
				Image.rectTransform.SetHeight(num2);
				Image.rectTransform.SetWidth(num);
			}
		}

		protected virtual Vector2? GetSize()
		{
			return null;
		}

		protected virtual Vector3 GetWorldPosition()
		{
			return Vector3.zero;
		}

		protected virtual Vector3 GetRotation()
		{
			return Vector3.zero;
		}

		protected virtual bool ShouldShowSprite()
		{
			return true;
		}

		public virtual void SetActivated(bool active)
		{
			if (Image != null)
			{
				Image.enabled = active;
			}
		}
	}
}
