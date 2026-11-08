using OpenFrontier.IP.ZFrame;

namespace OpenFrontier.IP.billing
{
	public static class Products
	{
		public static bool IAPEnabled => false;

		public static bool HasProduct(string product)
		{
			// Open Frontier: all former IAP content is unlocked for everyone.
			return true;
		}

		public static bool HasProduct(IPProduct product)
		{
			return HasProduct(product.Id);
		}

		public static void SetHasProduct(string product, bool hasProduct)
		{
			ZPlayerPrefs.SetInt(product, hasProduct ? 1 : 0);
		}
	}
}
