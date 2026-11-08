using System.Collections.Generic;
using UnityEngine;

namespace OpenFrontier.IP.Engine.CargoFactory
{
	public class CargoFactoryProfile : MonoBehaviour
	{
		[SerializeField]
		private List<CargoFactoryProfileItem> items = new List<CargoFactoryProfileItem>();

		public List<CargoFactoryProfileItem> Items => items;
	}
}
