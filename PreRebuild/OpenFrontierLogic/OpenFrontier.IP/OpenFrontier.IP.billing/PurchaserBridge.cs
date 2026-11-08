using UnityEngine;

namespace OpenFrontier.IP.billing
{
	public class PurchaserBridge : MonoBehaviour
	{
		private IPurchaser purchaser;

		public IPProduct[] Products;

		public IPurchaser Purchaser => purchaser;

		public void InitialisePurchaser()
		{
			if (purchaser == null)
			{
				purchaser = CreatePurchaser();
				purchaser.Products = Products;
				purchaser.Init();
			}
		}

		public bool AnyProductsUnpurchased()
		{
			IPProduct[] products = Products;
			for (int i = 0; i < products.Length; i++)
			{
				if (!products[i].IsPurchased)
				{
					return true;
				}
			}
			return false;
		}

		public IPurchaser CreatePurchaser()
		{
			// Open Frontier: no real IAP backend; stub purchaser keeps the store browsable.
			return gameObject.AddComponent<MockPurchaser>();
		}
	}
}
