using OpenFrontier.IP.Engine.Fleets.ActiveOrders;
using OpenFrontier.IP.UI.Screens.EnterSlider;

namespace OpenFrontier.IP.UI.Screens.Orders.Buttons
{
	public static class RearmOrderHelper
	{
		public static void ShowEquipmentUsagePrompt(EnterSliderIngameScreen.EnterSliderConfirmedHandler handler)
		{
			UIController.Instance.ScreenNavigator.ShowEnterSliderScreen((EnterSliderIngameScreen enterSliderScreen) =>
			{
				enterSliderScreen.PromptText.text = "Enter cargo space for rearming...";
				enterSliderScreen.Slider.minValue = 0.1f;
				enterSliderScreen.Slider.value = 0.25f;
				enterSliderScreen.Slider.maxValue = 1f;
				enterSliderScreen.GetSliderValueText = (float value) => $"{ActiveRearmOrder.GetDesiredEquipmentUsage01(value):P0}";
				enterSliderScreen.NavigateBackOnOk = false;
				enterSliderScreen.EnterSliderConfirmed += handler;
			});
		}
	}
}
