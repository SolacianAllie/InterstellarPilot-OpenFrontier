using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public static class PasssengerGroupHelper
	{
		public static int GetMaxPassengerGroupCountAtUnit(Unit unit)
		{
			return Mathf.CeilToInt(unit.UnitClass.PassengerGroupCountMultiplier * (float)GameController.Instance.GameSettings.PassengersPreferredGroupCount);
		}
	}
}
