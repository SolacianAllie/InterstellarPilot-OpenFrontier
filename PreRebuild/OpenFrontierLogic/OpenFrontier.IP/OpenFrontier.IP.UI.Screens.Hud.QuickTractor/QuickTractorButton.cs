using OpenFrontier.IP.Engine;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.Hud.QuickTractor
{
	public class QuickTractorButton : MonoBehaviour
	{
		public Button Button;

		public Image CargoIconImage;

		private Cargo cargoComponent;

		public Cargo CargoComponent
		{
			get
			{
				return cargoComponent;
			}
			set
			{
				cargoComponent = value;
				if (cargoComponent != null)
				{
					CargoIconImage.sprite = EngineASX.Instance.EngineResources.GetCargoSpriteOrDefault(cargoComponent.CargoClass);
				}
			}
		}
	}
}
