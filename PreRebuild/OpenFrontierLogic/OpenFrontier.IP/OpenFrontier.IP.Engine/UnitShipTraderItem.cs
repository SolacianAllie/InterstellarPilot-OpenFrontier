using System;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.billing;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class UnitShipTraderItem
	{
		public float SellMultiplier = 1f;

		public UnitClass UnitClass;

		private CustomUnitVariant customUnitVariant;

		public CustomUnitVariant CustomUnitVariant
		{
			get
			{
				return customUnitVariant;
			}
			set
			{
				customUnitVariant = value;
			}
		}

		public bool RequiresIap
		{
			get
			{
				if (Products.IAPEnabled && UnitClass != null)
				{
					return UnitClass.RequiredProduct != null;
				}
				return false;
			}
		}

		public IPProduct RequiredProduct
		{
			get
			{
				if (Products.IAPEnabled && UnitClass != null)
				{
					return UnitClass.RequiredProduct;
				}
				return null;
			}
		}
	}
}
