using System;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class UnitAttributeLevels
	{
		public UnitAttributeTypes AttributeType = UnitAttributeTypes.None;

		public float BaseValue;

		public float ChangePerLevel;
	}
}
