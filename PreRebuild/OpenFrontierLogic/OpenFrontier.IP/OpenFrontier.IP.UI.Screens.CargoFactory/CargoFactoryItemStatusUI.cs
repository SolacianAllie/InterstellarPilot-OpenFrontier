using System;
using OpenFrontier.IP.Common;
using OpenFrontier.IP.Engine.CargoFactory;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoFactory
{
	public class CargoFactoryItemStatusUI : MonoBehaviour
	{
		public Text StatusText;

		public Text TimeRemainingText;

		public void Refresh(CargoFactoryItem item)
		{
			StatusText.text = GetStatusText(item);
			TimeRemainingText.gameObject.SetActive(item.State == CargoFactoryItemState.Producing);
			if (item.State == CargoFactoryItemState.Producing)
			{
				TimeRemainingText.text = GetTimeRemainingText(item);
			}
		}

		public string GetStatusText(CargoFactoryItem item)
		{
			return item.State switch
			{
				CargoFactoryItemState.ProducedWaitingForFreeSpace => "Insufficient Cargo Space", 
				CargoFactoryItemState.Producing => "Producing", 
				_ => "Idle", 
			};
		}

		public string GetTimeRemainingText(CargoFactoryItem item)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds(item.ProductionTimeRemaining);
			string arg = timeSpan.Minutes.ToString().PadLeft(2, '0');
			string arg2 = timeSpan.Seconds.ToString().PadLeft(2, '0');
			return $"Time Remaining {arg}:{arg2}";
		}
	}
}
