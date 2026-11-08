using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine
{
	public class UnitShipTrader : MonoBehaviour
	{
		public UnitShipTraderProfile Profile;

		public List<UnitShipTraderItem> ShipTradeItems = new List<UnitShipTraderItem>();

		public List<UnitShipTraderItem> GetShipItems()
		{
			if (Profile != null)
			{
				return Profile.ShipTradeItems;
			}
			return ShipTradeItems;
		}

		public void TrimDuplicates()
		{
			List<UnitClass> list = new List<UnitClass>();
			for (int i = 0; i < ShipTradeItems.Count; i++)
			{
				if (list.Contains(ShipTradeItems[i].UnitClass))
				{
					ShipTradeItems.RemoveAt(i);
					i--;
				}
				else
				{
					list.Add(ShipTradeItems[i].UnitClass);
				}
			}
		}

		public bool Sells(UnitClass unitClass)
		{
			foreach (UnitShipTraderItem shipItem in GetShipItems())
			{
				if (shipItem.UnitClass == unitClass)
				{
					return true;
				}
			}
			return false;
		}
	}
}
