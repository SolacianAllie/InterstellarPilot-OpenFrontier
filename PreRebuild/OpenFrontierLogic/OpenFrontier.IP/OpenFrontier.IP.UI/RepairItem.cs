using OpenFrontier.IP.Engine.UnitComponents;

namespace OpenFrontier.IP.UI
{
	public class RepairItem
	{
		public ComponentBase Component;

		public override string ToString()
		{
			return "RepairItem. Component: " + Component;
		}

		public override bool Equals(object obj)
		{
			if (obj is RepairItem repairItem)
			{
				return repairItem.Component == Component;
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (Component != null)
			{
				return Component.GetHashCode();
			}
			return base.GetHashCode();
		}
	}
}
