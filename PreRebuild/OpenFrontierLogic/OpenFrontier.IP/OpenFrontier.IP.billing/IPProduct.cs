using UnityEngine;

namespace OpenFrontier.IP.billing
{
	public class IPProduct : MonoBehaviour
	{
		public string Id;

		public string InternalDescription;

		public string Name;

		public string FullDescription;

		public bool IsPurchased => Products.HasProduct(this);
	}
}
