using System;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.CargoTrade;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class CargoInfoScreen : EngineScreen
	{
		public Image CargoItemPreviewImage;

		public Button StationInfoButton;

		public static CargoClass ShowItem;

		public CargoClass CargoClass;

		public Text ItemDescriptionLabel;

		public Text ItemLabel;

		public Button NextCargoButton;

		public Button PreviousCargoButton;

		public CargoStatGenerator StatGenerator;

		[NonSerialized]
		public CargoTradeItemScreen TradeItemUI;

		[NonSerialized]
		public CargoTradeScreen TradeMenuUI;

		public void SwitchCargo(int change)
		{
			if (TradeMenuUI != null && TradeItemUI != null)
			{
				CargoClass cargoClass = (TradeItemUI.CargoClass = TradeMenuUI.SwitchCargo(CargoClass, change));
				CargoClass = cargoClass;
				Refresh();
			}
		}

		protected override void awake()
		{
			base.awake();
			if (NextCargoButton != null)
			{
				NextCargoButton.onClick.AddListener(NextCargo);
			}
			if (PreviousCargoButton != null)
			{
				PreviousCargoButton.onClick.AddListener(PreviousCargo);
			}
			StationInfoButton.onClick.AddListener(StationInfoButtonClick);
		}

		private void StationInfoButtonClick()
		{
			Unit component = CargoClass.RelatedPrefab.GetComponent<Unit>();
			if (component != null)
			{
				UIController.Instance.ScreenNavigator.ShowUnitInfoScreen(component);
			}
		}

		protected override void refresh()
		{
			base.refresh();
			StationInfoButton.interactable = CargoClass != null && CargoClass.IsDeployable && CargoClass.RelatedPrefab != null;
			StatGenerator.Item = CargoClass;
			StatGenerator.Refresh();
			TradeItemUI = UIController.Instance.ScreenNavigator.LoadedScreens.OfType<CargoTradeItemScreen>().FirstOrDefault();
			TradeMenuUI = UIController.Instance.ScreenNavigator.LoadedScreens.OfType<CargoTradeScreen>().FirstOrDefault();
			bool active = TradeItemUI != null && TradeMenuUI != null;
			if (NextCargoButton != null)
			{
				NextCargoButton.gameObject.SetActive(active);
			}
			if (PreviousCargoButton != null)
			{
				PreviousCargoButton.gameObject.SetActive(active);
			}
			if (CargoClass != null)
			{
				CargoItemPreviewImage.sprite = Eng.EngineResources.GetCargoSpriteOrDefault(CargoClass);
				if (ItemLabel != null)
				{
					ItemLabel.text = CargoClass.ClassName;
				}
				if (ItemDescriptionLabel != null)
				{
					ItemDescriptionLabel.text = GetCargoClassDescription();
				}
			}
		}

		private string GetCargoClassDescription()
		{
			if (!string.IsNullOrEmpty(CargoClass.Description))
			{
				return CargoClass.Description;
			}
			if (CargoClass.IsDeployable && CargoClass.RelatedPrefab != null)
			{
				Unit component = CargoClass.RelatedPrefab.GetComponent<Unit>();
				if (component != null)
				{
					return $"This package contains all the materials needed to construct a {component.UnitClass.GetClassAndSeriesName()} in a single container. Station builders carry this cargo to a suitable deployment location to start construction.";
				}
			}
			return null;
		}

		private void PreviousCargo()
		{
			SwitchCargo(-1);
		}

		private void NextCargo()
		{
			SwitchCargo(1);
		}
	}
}
