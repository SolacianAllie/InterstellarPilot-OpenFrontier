using System;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class CargoBayItem
	{
		public CargoClass CargoClass;

		public int Quantity;

		public float Load => CargoClass.Volume * (float)Quantity;

		public CargoBayItem Clone()
		{
			return new CargoBayItem
			{
				CargoClass = CargoClass,
				Quantity = Quantity
			};
		}
	}
}
