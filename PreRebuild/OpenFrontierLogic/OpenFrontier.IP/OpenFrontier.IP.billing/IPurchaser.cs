namespace OpenFrontier.IP.billing
{
	public interface IPurchaser
	{
		IPProduct[] Products { get; set; }

		bool IsInitialised { get; }

		int? InitializationFailureResult { get; }

		event PurchaserInitialisationFailedHandler InitialisationFailed;

		event PurchaseMadeHandler PurchaseMade;

		event PurchaserInitialisedHandler Initialised;

		event PurchasesRestoredHandler PurchasesRestored;

		bool TryGetPriceText(string productId, out string price);

		void BuyProductID(string productId);

		void Init();

		void Dispose();

		void RestorePurchases();
	}
}
