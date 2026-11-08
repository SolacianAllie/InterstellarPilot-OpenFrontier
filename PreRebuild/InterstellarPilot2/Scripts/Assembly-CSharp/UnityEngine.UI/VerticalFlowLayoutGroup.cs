namespace UnityEngine.UI
{
	[AddComponentMenu("Layout/Vertical Flow Layout Group", 155)]
	public class VerticalFlowLayoutGroup : AbstractFlowLayoutGroup
	{
		public override Vector2 Spacing => new Vector2(lineSpacing, spacing);

		protected VerticalFlowLayoutGroup()
		{
		}

		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			CalcAlongAxis(1, isVertical: true);
			CalcAlongAxis(0, isVertical: true);
		}

		public override void CalculateLayoutInputVertical()
		{
			CalcAlongAxis(1, isVertical: true);
			CalcAlongAxis(0, isVertical: true);
		}

		public override void SetLayoutHorizontal()
		{
			SetChildrenAlongAxis(1, isVertical: true);
			SetChildrenAlongAxis(0, isVertical: true);
		}

		public override void SetLayoutVertical()
		{
			SetChildrenAlongAxis(1, isVertical: true);
			SetChildrenAlongAxis(0, isVertical: true);
		}
	}
}
