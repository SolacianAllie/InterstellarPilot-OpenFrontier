using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine
{
	public static class UnitComponentHolderExtensions
	{
		public static string GetClassAndName(this UnitComponentHolder unitComponentHolder, bool shortName = false)
		{
			if (unitComponentHolder.Unit.UnitType == UnitType.Ship && !string.IsNullOrEmpty(unitComponentHolder.ShipName))
			{
				return string.Format("{0} {1}\"{2}\"", unitComponentHolder.Unit.GetClassAndSeriesName(), (!shortName) ? "class " : string.Empty, unitComponentHolder.ShipName);
			}
			return unitComponentHolder.Unit.GetClassAndSeriesName();
		}

		public static bool AnyComponentDamaged(this UnitComponentHolder unitComponentHolder)
		{
			foreach (ComponentBase unitComponent in unitComponentHolder.UnitComponents)
			{
				if (unitComponent.IsDamaged)
				{
					return true;
				}
			}
			return false;
		}

		public static bool AnyComponentDamaged(this UnitComponentHolder unitComponentHolder, float damageNormalizedThreshold)
		{
			foreach (ComponentBase unitComponent in unitComponentHolder.UnitComponents)
			{
				if (unitComponent.NormalizedDamage > damageNormalizedThreshold)
				{
					return true;
				}
			}
			return false;
		}

		public static float GetShieldHealthNormalized(this UnitComponentHolder unitComponentHolder)
		{
			ShieldComponent shieldComponent = unitComponentHolder.ShieldComponent;
			if (shieldComponent != null)
			{
				return shieldComponent.GetNormalizedShieldCharge();
			}
			return 0f;
		}
	}
}
