using System.Collections.Generic;
using OpenFrontier.Unity.Utils;

namespace OpenFrontier.IP.Engine.CompatibleComponents
{
	public class UnitClassCompatibleComponents
	{
		private Dictionary<int, BayCompatibleComponents> bayComponents = new Dictionary<int, BayCompatibleComponents>();

		public BayCompatibleComponents GetBayComponents(int bayId)
		{
			return bayComponents.GetItemOrDefault(bayId);
		}

		public void SetBayComponents(int bayId, BayCompatibleComponents bayCompatibleComponents)
		{
			bayComponents[bayId] = bayCompatibleComponents;
		}
	}
}
