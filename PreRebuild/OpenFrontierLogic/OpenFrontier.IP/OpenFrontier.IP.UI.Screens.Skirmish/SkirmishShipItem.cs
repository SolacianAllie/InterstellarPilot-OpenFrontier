using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CombatRatings;
using OpenFrontier.IP.Engine.CustomUnitVariants;

namespace OpenFrontier.IP.UI.Screens.Skirmish
{
	public class SkirmishShipItem
	{
		public UnitClass UnitClass;

		public CustomUnitVariant CustomVariant;

		public bool IsPlayer { get; set; }

		public static SkirmishShipItem FromCustomVariant(CustomUnitVariant customUnitVariant, bool isPlayer = false)
		{
			return new SkirmishShipItem
			{
				UnitClass = customUnitVariant.UnitClass,
				CustomVariant = customUnitVariant,
				IsPlayer = isPlayer
			};
		}

		public static SkirmishShipItem FromUnitClass(UnitClass unitClass, bool isPlayer = false)
		{
			return new SkirmishShipItem
			{
				UnitClass = unitClass,
				IsPlayer = isPlayer
			};
		}

		public static SkirmishShipItem FromSkirmishTeamParamsItem(SkirmishTeamParamsItem item, bool isPlayer = false)
		{
			return new SkirmishShipItem
			{
				UnitClass = item.UnitClass,
				CustomVariant = item.CustomUnitVariant,
				IsPlayer = isPlayer
			};
		}

		public SkirmishShipItem Clone()
		{
			return new SkirmishShipItem
			{
				UnitClass = UnitClass,
				CustomVariant = CustomVariant
			};
		}

		public string GetClassAndSeriesName()
		{
			if (CustomVariant == null)
			{
				return UnitClass.GetClassAndSeriesName();
			}
			return CustomVariant.FullName;
		}

		public float GetCombatRating()
		{
			if (CustomVariant == null)
			{
				return UnitClass.CombatRating;
			}
			return CombatRatingHelper.CalculateCustomVariantCombatRating(CustomVariant);
		}
	}
}
