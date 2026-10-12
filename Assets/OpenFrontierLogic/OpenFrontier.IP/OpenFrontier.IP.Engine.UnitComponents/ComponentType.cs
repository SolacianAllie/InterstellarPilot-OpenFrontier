using UnityEngine;

namespace OpenFrontier.IP.Engine.UnitComponents
{
	public class ComponentType : MonoBehaviour
	{
		public bool AllowSell = true;

		public bool CanBeDamaged = true;

		public bool CanSetUserPower = true;

		public string Description;

		public string FriendlyName;

		public bool InitialAutoChargeValue = true;

		public int MaxCount = 100;

		public int MinCount;

		public int RechargePriority;

		public bool RequiresRecharge;
	}
}
