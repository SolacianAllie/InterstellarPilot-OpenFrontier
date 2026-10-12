using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using UnityEngine;

namespace OpenFrontier.IP.Scenarios.RandomEvents
{
	public class NewDerilectShipEventUnitClassPopulator : MonoBehaviour
	{
		private NewDerilectShip newDerilectShipEvent;

		private bool hasInitialised;

		private void Awake()
		{
			newDerilectShipEvent = GetComponent<NewDerilectShip>();
		}

		private void Update()
		{
			if (!hasInitialised && GameController.Instance != null)
			{
				if (newDerilectShipEvent != null)
				{
					newDerilectShipEvent.PossibleUnitClasses = GetUnitClasses();
				}
				hasInitialised = true;
				Object.Destroy(this);
			}
		}

		public List<UnitClass> GetUnitClasses()
		{
			return GameController.Instance.LoadedUnitClasses.Where((UnitClass e) => e.RequiredProduct == null && e.UnitType == UnitType.Ship && e.ShipType == ShipType.Normal && e.IsUsable && e.Purposes != UnitPurpose.None).ToList();
		}
	}
}
