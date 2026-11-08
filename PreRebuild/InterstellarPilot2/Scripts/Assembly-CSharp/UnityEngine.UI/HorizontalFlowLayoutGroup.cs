namespace UnityEngine.UI
{
	[AddComponentMenu("Layout/Horizontal Flow Layout Group", 154)]
	public class HorizontalFlowLayoutGroup : AbstractFlowLayoutGroup
	{
		protected HorizontalFlowLayoutGroup()
		{
		}

		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			CalcAlongAxis(0, isVertical: false);
			CalcAlongAxis(1, isVertical: false);
		}

		public override void CalculateLayoutInputVertical()
		{
			CalcAlongAxis(0, isVertical: false);
			CalcAlongAxis(1, isVertical: false);
		}

		public override void SetLayoutHorizontal()
		{
			SetChildrenAlongAxis(0, isVertical: false);
			SetChildrenAlongAxis(1, isVertical: false);
		}

		public override void SetLayoutVertical()
		{
			SetChildrenAlongAxis(0, isVertical: false);
			SetChildrenAlongAxis(1, isVertical: false);
		}
	}
}
