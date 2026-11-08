using OpenFrontier.IP.Engine;
using OpenFrontier.IP.billing;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.PurchasesStore
{
	public class PurchaseBanner : MonoBehaviour
	{
		public IPProduct Product;

		public Button BuyButton;

		public GameObject PurchasedIndicator;

		public Text PurchasePriceText;

		private bool hasSetPrice;

		private bool? isPurchased;

		private void Awake()
		{
			BuyButton.onClick.AddListener(BuyProduct);
		}

		private void BuyProduct()
		{
			if (Debug.isDebugBuild)
			{
				Products.SetHasProduct(Product.Id, hasProduct: true);
				Refresh();
			}
			else
			{
				GameController.Instance.PurchaseBridge.Purchaser.BuyProductID(Product.Id);
			}
		}

		private void Update()
		{
			if (!isPurchased.HasValue)
			{
				RefreshIsPurchased();
			}
			RefreshInfo();
		}

		public void Refresh()
		{
			RefreshIsPurchased();
			RefreshInfo();
		}

		private void RefreshInfo()
		{
			if (!isPurchased.Value)
			{
				if (!hasSetPrice)
				{
					TrySetPriceText();
				}
				BuyButton.interactable = GameController.Instance.PurchaseBridge.Purchaser != null && GameController.Instance.PurchaseBridge.Purchaser.IsInitialised;
			}
			BuyButton.gameObject.SetActive(!isPurchased.Value);
			PurchasedIndicator.SetActive(isPurchased.Value);
		}

		private void RefreshIsPurchased()
		{
			isPurchased = Products.HasProduct(Product);
		}

		private void TrySetPriceText()
		{
			string price = string.Empty;
			if (GameController.Instance.PurchaseBridge.Purchaser.TryGetPriceText(Product.Id, out price))
			{
				hasSetPrice = true;
				PurchasePriceText.text = price;
			}
		}
	}
}
