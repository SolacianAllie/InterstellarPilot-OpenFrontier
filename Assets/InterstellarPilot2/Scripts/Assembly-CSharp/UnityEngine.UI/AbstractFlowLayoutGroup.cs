using System.Collections.Generic;

namespace UnityEngine.UI
{
	[ExecuteAlways]
	public abstract class AbstractFlowLayoutGroup : LayoutGroup
	{
		[SerializeField]
		protected float m_Spacing;

		[SerializeField]
		protected float m_LineSpacing;

		[SerializeField]
		protected bool m_ChildControlWidth = true;

		[SerializeField]
		protected bool m_ChildControlHeight = true;

		[SerializeField]
		protected bool m_ChildScaleWidth;

		[SerializeField]
		protected bool m_ChildScaleHeight;

		[SerializeField]
		protected bool m_ReverseArrangement;

		public float spacing
		{
			get
			{
				return m_Spacing;
			}
			set
			{
				SetProperty(ref m_Spacing, value);
			}
		}

		public float lineSpacing
		{
			get
			{
				return m_LineSpacing;
			}
			set
			{
				SetProperty(ref m_LineSpacing, value);
			}
		}

		public virtual Vector2 Spacing => new Vector2(spacing, lineSpacing);

		public bool childControlWidth
		{
			get
			{
				return m_ChildControlWidth;
			}
			set
			{
				SetProperty(ref m_ChildControlWidth, value);
			}
		}

		public bool childControlHeight
		{
			get
			{
				return m_ChildControlHeight;
			}
			set
			{
				SetProperty(ref m_ChildControlHeight, value);
			}
		}

		public bool childScaleWidth
		{
			get
			{
				return m_ChildScaleWidth;
			}
			set
			{
				SetProperty(ref m_ChildScaleWidth, value);
			}
		}

		public bool childScaleHeight
		{
			get
			{
				return m_ChildScaleHeight;
			}
			set
			{
				SetProperty(ref m_ChildScaleHeight, value);
			}
		}

		public bool reverseArrangement
		{
			get
			{
				return m_ReverseArrangement;
			}
			set
			{
				SetProperty(ref m_ReverseArrangement, value);
			}
		}

		public float GetWidth => rectTransform.rect.width - (float)padding.horizontal;

		public float GetHeight => rectTransform.rect.height;

		protected override void Awake()
		{
			base.Awake();
			LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
		}

		protected void CalcAlongAxis(int axis, bool isVertical)
		{
			_ = base.rectTransform.rect.size[axis];
			if (axis != 0)
			{
				_ = padding.vertical;
			}
			else
			{
				_ = padding.horizontal;
			}
			float num = ((axis == 0) ? padding.horizontal : padding.vertical);
			bool controlSize = ((axis == 0) ? m_ChildControlWidth : m_ChildControlHeight);
			bool flag = ((axis == 0) ? m_ChildScaleWidth : m_ChildScaleHeight);
			float num2 = num;
			float num3 = num;
			float num4 = 0f;
			bool flag2 = isVertical ^ (axis == 1);
			_ = rectChildren.Count;
			int num5 = ((axis == 0) ? 1 : 0);
			float num6 = base.rectTransform.rect.size[num5] - (float)((num5 == 0) ? padding.horizontal : padding.vertical);
			bool flag3 = ((num5 == 0) ? m_ChildScaleWidth : m_ChildScaleHeight);
			int num7 = 0;
			float num8 = 0f;
			float num9 = 0f;
			float num10 = 0f;
			int num11 = (m_ReverseArrangement ? (rectChildren.Count - 1) : 0);
			int num12 = ((!m_ReverseArrangement) ? rectChildren.Count : 0);
			int num13 = ((!m_ReverseArrangement) ? 1 : (-1));
			for (int i = num11; m_ReverseArrangement ? (i >= num12) : (i < num12); i += num13)
			{
				RectTransform rectTransform = rectChildren[i];
				GetChildSizes(rectTransform, axis, controlSize, childForceExpand: false, out var min, out var preferred, out var flexible);
				GetChildSizes(rectTransform, num5, controlSize, childForceExpand: false, out var _, out var _, out var _);
				if (flag)
				{
					float num14 = rectTransform.localScale[axis];
					min *= num14;
					preferred *= num14;
					flexible *= num14;
				}
				if (flag2)
				{
					num2 = Mathf.Max(min + num, num2);
					num3 = Mathf.Max(preferred + num, num3);
					num4 = Mathf.Max(flexible, num4);
					num8 += rectTransform.sizeDelta[num5] * (flag3 ? rectTransform.localScale[num5] : 1f);
					if (num8 > num6)
					{
						num7++;
						num9 += num10;
						num10 = rectTransform.sizeDelta[axis] * (flag ? rectTransform.localScale[axis] : 1f);
						num8 = rectTransform.sizeDelta[num5] * (flag3 ? rectTransform.localScale[num5] : 1f);
					}
					else
					{
						num10 = Mathf.Max(rectTransform.sizeDelta[axis] * (flag ? rectTransform.localScale[axis] : 1f), num10);
					}
					num8 += spacing;
				}
				else
				{
					num2 += min + spacing;
					num3 += preferred + spacing;
					num4 += flexible;
				}
			}
			if (!flag2 && rectChildren.Count > 0)
			{
				num2 -= spacing;
				num3 -= spacing;
			}
			num3 = Mathf.Max(num2, num3);
			if (flag2)
			{
				num9 += num10;
				num2 = num3;
				num3 = num9 + lineSpacing * (float)num7 + num;
			}
			if (!flag2)
			{
				base.SetLayoutInputForAxis(num4, LayoutUtility.DefaultMaxSize, num4, num4, axis);
			}
			else
			{
				base.SetLayoutInputForAxis(num2, LayoutUtility.DefaultMaxSize, num3, num4, axis);
			}
		}

		protected void SetChildrenAlongAxis(int axis, bool isVertical)
		{
			float num = base.rectTransform.rect.size[axis];
			float innerSize = num - (float)((axis == 0) ? padding.horizontal : padding.vertical);
			bool controlSize = ((axis == 0) ? m_ChildControlWidth : m_ChildControlHeight);
			bool useScale = ((axis == 0) ? m_ChildScaleWidth : m_ChildScaleHeight);
			float alignmentOnAxis = GetAlignmentOnAxis(axis);
			bool num2 = isVertical ^ (axis == 1);
			int startIndex = (m_ReverseArrangement ? (rectChildren.Count - 1) : 0);
			int endIndex = ((!m_ReverseArrangement) ? rectChildren.Count : 0);
			int increment = ((!m_ReverseArrangement) ? 1 : (-1));
			int num3 = ((axis == 0) ? 1 : 0);
			bool flag = ((num3 == 0) ? m_ChildScaleWidth : m_ChildScaleHeight);
			float num4 = base.rectTransform.rect.size[num3] - (float)((num3 == 0) ? padding.horizontal : padding.vertical);
			if (num2)
			{
				float num5 = 0f;
				int num6 = 0;
				float num7 = 0f;
				float num8 = 0f;
				for (int i = startIndex; m_ReverseArrangement ? (i >= endIndex) : (i < endIndex); i += increment)
				{
					RectTransform rectTransform = rectChildren[i];
					GetChildSizes(rectTransform, axis, controlSize, childForceExpand: false, out var min, out var preferred, out var flexible);
					GetChildSizes(rectTransform, num3, controlSize, childForceExpand: false, out var _, out var _, out var _);
					float scaleFactor = (useScale ? rectTransform.localScale[axis] : 1f);
					float num9 = Mathf.Clamp(innerSize, min, (flexible > 0f) ? num : preferred);
					float delta = num - GetTotalPreferredSize(axis);
					delta = CalcOtherAxisOffset(delta);
					delta += (float)((axis == 0) ? padding.left : padding.top);
					num5 += rectTransform.sizeDelta[num3] * (flag ? rectTransform.localScale[num3] : 1f);
					if (num5 > num4)
					{
						num6++;
						num8 += num7;
						num7 = rectTransform.sizeDelta[axis] * (useScale ? rectTransform.localScale[axis] : 1f);
						num5 = rectTransform.sizeDelta[num3] * (flag ? rectTransform.localScale[num3] : 1f);
					}
					else
					{
						num7 = Mathf.Max(rectTransform.sizeDelta[axis] * (flag ? rectTransform.localScale[axis] : 1f), num7);
					}
					num5 += spacing;
					float num10 = num8 + lineSpacing * (float)num6;
					if (controlSize)
					{
						SetChildAlongAxisWithScale(rectTransform, axis, delta + num10, num9, scaleFactor);
						continue;
					}
					float num11 = (num9 - rectTransform.sizeDelta[axis]) * alignmentOnAxis;
					SetChildAlongAxisWithScale(rectTransform, axis, delta + num11 + num10, scaleFactor);
				}
				return;
			}
			float num12 = ((axis == 0) ? padding.left : padding.top);
			float num13 = 0f;
			GetTotalPreferredSize(axis);
			float t = 0f;
			if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
			{
				t = Mathf.Clamp01((num - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));
			}
			List<List<RectTransform>> rows = DivideIntoRows();
			int num14 = 0;
			float num15 = 0f;
			for (int j = startIndex; m_ReverseArrangement ? (j >= endIndex) : (j < endIndex); j += increment)
			{
				RectTransform rectTransform2 = rectChildren[j];
				GetChildSizes(rectTransform2, axis, controlSize, childForceExpand: false, out var min3, out var preferred3, out var flexible3);
				min3 = preferred3;
				num15 += rectTransform2.sizeDelta[axis] * (useScale ? rectTransform2.localScale[axis] : 1f);
				if (num15 > innerSize)
				{
					num14++;
					num15 = rectTransform2.sizeDelta[axis] * (useScale ? rectTransform2.localScale[axis] : 1f);
					num12 = ((axis == 0) ? padding.left : padding.top);
				}
				num15 += spacing;
				float num16 = (useScale ? rectTransform2.localScale[axis] : 1f);
				float num17 = Mathf.Lerp(min3, preferred3, t);
				num17 += flexible3 * num13;
				float num18 = CalcRowOffset(num14);
				if (controlSize)
				{
					SetChildAlongAxisWithScale(rectTransform2, axis, num12 + num18, num17, num16);
				}
				else
				{
					SetChildAlongAxisWithScale(rectTransform2, axis, num12 + num18, num16);
				}
				num12 += num17 * num16 + spacing;
			}
			float CalcOtherAxisOffset(float num19)
			{
				if (axis == 0)
				{
					if (childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.LowerLeft)
					{
						return 0f;
					}
					if (childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.LowerCenter)
					{
						return num19 / 2f;
					}
					return num19;
				}
				if (childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.UpperRight)
				{
					return 0f;
				}
				if (childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.MiddleRight)
				{
					return num19 / 2f;
				}
				return num19;
			}
			float CalcRowOffset(int index)
			{
				float num19 = CalcRowSize(index);
				float num20 = innerSize - num19;
				if (axis == 0)
				{
					if (childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.LowerLeft)
					{
						return 0f;
					}
					if (childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.LowerCenter)
					{
						return num20 / 2f;
					}
					return num20;
				}
				if (childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.UpperRight)
				{
					return 0f;
				}
				if (childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.MiddleRight)
				{
					return num20 / 2f;
				}
				return num20;
			}
			float CalcRowSize(int index)
			{
				List<RectTransform> list = rows[index];
				float num19 = 0f;
				foreach (RectTransform item in list)
				{
					num19 += item.sizeDelta[axis] * (useScale ? item.localScale[axis] : 1f);
					num19 += spacing;
				}
				if (list.Count > 0)
				{
					num19 -= spacing;
				}
				return num19;
			}
			List<List<RectTransform>> DivideIntoRows()
			{
				int num19 = 0;
				float num20 = 0f;
				List<List<RectTransform>> list = new List<List<RectTransform>>
				{
					new List<RectTransform>()
				};
				for (int k = startIndex; m_ReverseArrangement ? (k >= endIndex) : (k < endIndex); k += increment)
				{
					RectTransform rectTransform3 = rectChildren[k];
					GetChildSizes(rectTransform3, axis, controlSize, childForceExpand: false, out var min4, out var preferred4, out var _);
					min4 = preferred4;
					num20 += rectTransform3.sizeDelta[axis] * (useScale ? rectTransform3.localScale[axis] : 1f);
					if (num20 > innerSize)
					{
						num19++;
						list.Add(new List<RectTransform>());
						num20 = rectTransform3.sizeDelta[axis] * (useScale ? rectTransform3.localScale[axis] : 1f);
					}
					list[num19].Add(rectTransform3);
					num20 += spacing;
				}
				return list;
			}
		}

		private void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand, out float min, out float preferred, out float flexible)
		{
			if (!controlSize)
			{
				min = child.sizeDelta[axis];
				preferred = min;
				flexible = 0f;
			}
			else
			{
				min = LayoutUtility.GetMinSize(child, axis);
				preferred = LayoutUtility.GetPreferredSize(child, axis);
				flexible = LayoutUtility.GetFlexibleSize(child, axis);
			}
		}
	}
}
