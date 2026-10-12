using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoTrade
{
	public class TradeMenuColumnHeader : MonoBehaviour
	{
		private Button button;

		public CargoTradeScreen.SortPriceMode SortMode = CargoTradeScreen.SortPriceMode.ShipQuantity;

		private CargoTradeScreen tradeMenuUI;

		public void PositionFilterGraphic()
		{
			if (tradeMenuUI == null)
			{
				Awake();
			}
			if (tradeMenuUI != null)
			{
				Graphic filterWidget = tradeMenuUI.ItemList.FilterWidget;
				if (filterWidget != null)
				{
					filterWidget.rectTransform.anchoredPosition3D = transform.localPosition + tradeMenuUI.ItemList.FilterWidgetOffsetFromHeader;
					Vector3 localScale = filterWidget.transform.localScale;
					localScale.x = Mathf.Abs(localScale.x) * (float)(-tradeMenuUI.SortDirection);
					filterWidget.transform.localScale = localScale;
				}
			}
			else if (LogWrapper.LogMsgs)
			{
				Debug.LogWarning("Could not position filter widget. No reference to trade menu", this);
			}
		}

		private void Awake()
		{
			tradeMenuUI = UnityObjectHelper.FindInParentsOrSelf<CargoTradeScreen>(gameObject);
			if (tradeMenuUI == null)
			{
				Debug.LogWarning("Could not find TradeScreen in parents", this);
			}
			else
			{
				tradeMenuUI.RegisterColumnHeader(SortMode, this);
			}
			button = GetComponentInChildren<Button>();
			if (button != null)
			{
				button.onClick.AddListener(OnClick);
			}
			else
			{
				Debug.LogWarning("Column header missing button", this);
			}
		}

		private void OnClick()
		{
			tradeMenuUI.SortItems(SortMode);
			PositionFilterGraphic();
		}
	}
}
