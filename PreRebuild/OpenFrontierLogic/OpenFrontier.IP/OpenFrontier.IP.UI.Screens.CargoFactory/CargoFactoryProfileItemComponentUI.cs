using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CargoFactory;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CargoFactory
{
	public class CargoFactoryProfileItemComponentUI : MonoBehaviour
	{
		public Text Text;

		public Image CargoIcon;

		private CargoFactoryProfileItemComponent cargoFactoryProfileItemComponent;

		public CargoFactoryProfileItemComponent CargoFactoryProfileItemComponent
		{
			get
			{
				return cargoFactoryProfileItemComponent;
			}
			set
			{
				cargoFactoryProfileItemComponent = value;
			}
		}

		public void Refresh()
		{
			if (cargoFactoryProfileItemComponent != null)
			{
				Text.text = TextFormattingHelper.FormatCargoAmount(cargoFactoryProfileItemComponent.Quantity);
				CargoIcon.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(cargoFactoryProfileItemComponent.CargoClass);
			}
		}
	}
}
