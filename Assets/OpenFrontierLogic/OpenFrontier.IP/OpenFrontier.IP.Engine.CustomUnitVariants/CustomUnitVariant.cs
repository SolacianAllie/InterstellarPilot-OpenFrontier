using System;
using System.Collections.Generic;

namespace OpenFrontier.IP.Engine.CustomUnitVariants
{
	public class CustomUnitVariant
	{
		public string Name { get; set; } = "";

		public string FullName => UnitClass.GetSeriesName() + "-" + Name;

		public string Description { get; set; } = "";

		public UnitClass UnitClass { get; set; }

		public List<CustomUnitVariantBayMod> BayMods { get; set; } = new List<CustomUnitVariantBayMod>();

		public List<CargoBayItem> CargoItems { get; set; } = new List<CargoBayItem>();

		public float GetCargoUsage()
		{
			float num = 0f;
			foreach (CargoBayItem cargoItem in CargoItems)
			{
				num += cargoItem.Load;
			}
			return num;
		}

		public float GetCargoCapacity()
		{
			return UnitClass.UnitPrefab.GetComponent<UnitComponentHolder>().CargoCapacity;
		}

		public float GetAvailableCargoCapacity()
		{
			return GetCargoCapacity() - GetCargoUsage();
		}

		public CustomUnitVariant Clone()
		{
			CustomUnitVariant customUnitVariant = new CustomUnitVariant();
			customUnitVariant.UnitClass = UnitClass;
			customUnitVariant.Name = Name;
			customUnitVariant.Description = Description;
			foreach (CustomUnitVariantBayMod bayMod in BayMods)
			{
				customUnitVariant.BayMods.Add(bayMod.Clone());
			}
			foreach (CargoBayItem cargoItem in CargoItems)
			{
				customUnitVariant.CargoItems.Add(cargoItem.Clone());
			}
			return customUnitVariant;
		}

		public bool IsValid()
		{
			if (UnitClass != null && (UnitClass.UnitType == UnitType.Ship || UnitClass.UnitType == UnitType.Station))
			{
				return UnitClass.IsUsable;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is CustomUnitVariant customUnitVariant))
			{
				return false;
			}
			if (customUnitVariant.UnitClass == UnitClass)
			{
				return customUnitVariant.Name == Name;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((UnitClass != null) ? UnitClass.UniqueID : (-1), (Name != null) ? UnitClass.UniqueID : (-1));
		}
	}
}
