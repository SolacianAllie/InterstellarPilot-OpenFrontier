using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.billing;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.IAP
{
	public class RequiredProductItem : MonoBehaviour
	{
		public Text DescriptionText;

		public Image StoreIcon;

		public List<IPProduct> RequiredProducts;

		public void Refresh()
		{
			if (RequiredProducts != null)
			{
				RequiredProducts.TrimNulls();
				bool flag = AllProductsPurchased();
				RefreshDescriptionText(flag);
				RefreshStoreImageColor(flag);
			}
		}

		private void RefreshDescriptionText(bool allProductsPurchased)
		{
			if (!allProductsPurchased)
			{
				DescriptionText.text = GetDescriptionText();
			}
			else
			{
				DescriptionText.text = "Unlocked";
			}
		}

		private string GetDescriptionText()
		{
			if (RequiredProducts.Count > 1)
			{
				string arg = string.Join(", ", RequiredProducts.Select((IPProduct e) => e.Name).ToArray());
				return $"Requires {arg} packs";
			}
			return $"Requires {RequiredProducts[0].Name} pack";
		}

		public bool AllProductsPurchased()
		{
			if (RequiredProducts != null)
			{
				foreach (IPProduct requiredProduct in RequiredProducts)
				{
					if (requiredProduct != null && !requiredProduct.IsPurchased)
					{
						return false;
					}
				}
			}
			return true;
		}

		private void RefreshStoreImageColor(bool purchased)
		{
			StoreIcon.color = (purchased ? GameController.Instance.StorePurchasedColor : GameController.Instance.StoreDefaultColor);
		}
	}
}
