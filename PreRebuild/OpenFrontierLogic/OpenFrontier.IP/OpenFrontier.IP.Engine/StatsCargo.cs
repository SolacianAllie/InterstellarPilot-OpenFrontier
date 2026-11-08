using System;

namespace OpenFrontier.IP.Engine
{
	[Serializable]
	public class StatsCargo
	{
		public double BoughtByAIVolume;

		public long BoughtByAIValue;

		public double SoldByAIVolume;

		public long SoldByAIValue;

		public CargoClass CargoType;

		public double ConsumedVolume;

		public long ConsumedValue;

		public double MinedVolume;

		public long MinedValue;

		public double ProducedVolume;

		public long ProducedValue;

		public double TrimmedVolume;

		public long TrimmedValue;
	}
}
