namespace OpenFrontier.IP.UI.HUDScannerDisplay
{
	public class HUDScannerDisplayList : ScrollList<HUDScannerItem>
	{
		protected override void OnLostActiveItem()
		{
		}

		public override void ResetScrollPosition()
		{
			scrollRect.verticalScrollbar.value = 0f;
		}
	}
}
