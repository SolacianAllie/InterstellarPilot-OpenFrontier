using UnityEngine;

namespace OpenFrontier.IP.billing
{
	public class MockPurchaser : MonoBehaviour, IPurchaser
	{
		public IPProduct[] Products { get; set; }

		public bool IsInitialised => true;

		public int? InitializationFailureResult { get; set; }

		public event PurchaseMadeHandler PurchaseMade;

		public event PurchaserInitialisedHandler Initialised;

		public event PurchasesRestoredHandler PurchasesRestored;

		public event PurchaserInitialisationFailedHandler InitialisationFailed;

		public void Init()
		{
		}

		public void RestorePurchases()
		{
			if (PurchasesRestored != null)
			{
				PurchasesRestored(this, result: true);
			}
		}

		public void BuyProductID(string productId)
		{
			OpenFrontier.IP.billing.Products.SetHasProduct(productId, hasProduct: true);
			if (PurchaseMade != null)
			{
				PurchaseMade(this, productId);
			}
		}

		public bool TryGetPriceText(string productId, out string price)
		{
			price = "Buy (Free)";
			return true;
		}

		public void Dispose()
		{
			Object.Destroy(this);
		}
	}
}
