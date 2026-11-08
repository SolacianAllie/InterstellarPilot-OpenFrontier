using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine.CustomUnitVariants
{
	public class CustomUnitVariantBayMod
	{
		public int BayId { get; set; }

		public ComponentClass ComponentClass { get; set; }

		public CustomUnitVariantBayMod Clone()
		{
			return new CustomUnitVariantBayMod
			{
				BayId = BayId,
				ComponentClass = ComponentClass
			};
		}
	}
}
