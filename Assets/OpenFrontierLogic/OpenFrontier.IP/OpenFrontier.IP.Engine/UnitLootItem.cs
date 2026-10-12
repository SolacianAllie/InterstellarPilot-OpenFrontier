using System;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class UnitLootItem
	{
		public CargoClass CargoClass;

		public int MaxQuantity = 1;

		public int MinQuantity = 1;

		public float SpawnProbability = 1f;

		public float SpawnQuantityBias = 1f;
	}
}
