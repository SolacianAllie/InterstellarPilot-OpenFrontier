using OpenFrontier.IP.Engine;
using OpenFrontier.IP.billing;
using UnityEngine;

namespace OpenFrontier.IP.UI.Screens.PurchasesStore
{
	public class StoreButtonBuyProduct : MonoBehaviour
	{
		public IPProduct Product;

		private void OnClick()
		{
			GameController.Instance.PurchaseBridge.Purchaser.BuyProductID(Product.Id);
		}
	}
}
