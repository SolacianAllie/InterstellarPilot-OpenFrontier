using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitComponentTrader : MonoBehaviour
	{
		public bool SellsSpecificComponents;

		public ComponentClass[] AvailableComponentClasses;

		public UnitComponentHolder UnitComponents { get; private set; }

		public void Init()
		{
			UnitComponents = GetComponent<UnitComponentHolder>();
		}
	}
}
