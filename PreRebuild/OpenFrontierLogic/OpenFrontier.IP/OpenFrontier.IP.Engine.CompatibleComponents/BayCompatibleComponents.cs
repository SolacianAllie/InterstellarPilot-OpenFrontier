using System.Collections.Generic;
using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.Engine.CompatibleComponents
{
	public class BayCompatibleComponents
	{
		private List<ComponentClass> components = new List<ComponentClass>(10);

		public List<ComponentClass> ComponentClasses => components;
	}
}
