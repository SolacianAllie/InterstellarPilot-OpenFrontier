using UnityEngine;

namespace OpenFrontier.IP.UI.Extensions
{
	public static class RectTransformExtensions
	{
		public static void SetDefaultScale(this RectTransform trans)
		{
			trans.localScale = new Vector3(1f, 1f, 1f);
		}

		public static void SetPivotAndAnchors(this RectTransform trans, Vector2 aVec)
		{
			trans.pivot = aVec;
			trans.anchorMin = aVec;
			trans.anchorMax = aVec;
		}

		public static Vector2 GetSize(this RectTransform trans)
		{
			return trans.rect.size;
		}

		public static float GetWidth(this RectTransform trans)
		{
			return trans.rect.width;
		}

		public static float GetHeight(this RectTransform trans)
		{
			return trans.rect.height;
		}

		public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
		}

		public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x + trans.pivot.x * trans.rect.width, newPos.y + trans.pivot.y * trans.rect.height, trans.localPosition.z);
		}

		public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x + trans.pivot.x * trans.rect.width, newPos.y - (1f - trans.pivot.y) * trans.rect.height, trans.localPosition.z);
		}

		public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x - (1f - trans.pivot.x) * trans.rect.width, newPos.y + trans.pivot.y * trans.rect.height, trans.localPosition.z);
		}

		public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos)
		{
			trans.localPosition = new Vector3(newPos.x - (1f - trans.pivot.x) * trans.rect.width, newPos.y - (1f - trans.pivot.y) * trans.rect.height, trans.localPosition.z);
		}

		public static void SetSize(this RectTransform trans, Vector2 newSize)
		{
			Vector2 size = trans.rect.size;
			Vector2 vector = newSize - size;
			trans.offsetMin -= new Vector2(vector.x * trans.pivot.x, vector.y * trans.pivot.y);
			trans.offsetMax += new Vector2(vector.x * (1f - trans.pivot.x), vector.y * (1f - trans.pivot.y));
		}

		public static void SetSize(this RectTransform trans, float width, float height)
		{
			trans.SetSize(new Vector2(width, height));
		}

		public static void SetWidth(this RectTransform trans, float newSize)
		{
			trans.SetSize(new Vector2(newSize, trans.rect.size.y));
		}

		public static void SetHeight(this RectTransform trans, float newSize)
		{
			trans.SetSize(new Vector2(trans.rect.size.x, newSize));
		}
	}
}
