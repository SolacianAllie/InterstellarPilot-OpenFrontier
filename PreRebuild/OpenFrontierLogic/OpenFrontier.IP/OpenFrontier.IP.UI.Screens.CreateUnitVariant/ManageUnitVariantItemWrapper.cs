using OpenFrontier.IP.Engine.CustomUnitVariants;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class ManageUnitVariantItemWrapper
	{
		public CustomUnitVariant CustomUnitVariant { get; set; }

		public string FileNameWithoutDirectory { get; set; }

		public override bool Equals(object obj)
		{
			if (!(obj is ManageUnitVariantItemWrapper manageUnitVariantItemWrapper))
			{
				return false;
			}
			if (manageUnitVariantItemWrapper.CustomUnitVariant == null != (CustomUnitVariant == null))
			{
				return false;
			}
			if (CustomUnitVariant == null)
			{
				return true;
			}
			return CustomUnitVariant.Equals(manageUnitVariantItemWrapper.CustomUnitVariant);
		}

		public override int GetHashCode()
		{
			if (CustomUnitVariant != null)
			{
				return CustomUnitVariant.GetHashCode();
			}
			return base.GetHashCode();
		}
	}
}
