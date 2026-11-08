using System;
using System.IO;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.MessageBox;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class CreateUnitVariantScreen : ScreenBase
	{
		public delegate void ConfirmedVariantHandler(CreateUnitVariantScreen sender, CustomUnitVariant customUnitVariant);

		public bool TransientVariant;

		public TextMeshProUGUI CostLabel;

		public TextMeshProUGUI CustomNameLabel;

		public TextMeshProUGUI CustomDescriptionLabel;

		public Button EditBaysButton;

		public Button EditCargoButton;

		public Button EditNameButton;

		public Image IconImage;

		public TextMeshProUGUI BaysLabel;

		public TextMeshProUGUI CargoLabel;

		public TextMeshProUGUI TitleLabel;

		public TextMeshProUGUI BaseTypeLabel;

		public Button SaveButton;

		public Button EditDescriptionButton;

		public Button ConfirmButton;

		public Transform DescriptionRoot;

		public Transform CustomNameRoot;

		public bool IsItemDirty { get; set; }

		public CustomUnitVariant CustomUnitVariant { get; set; }

		public CustomUnitVariant OriginalUnitVariant { get; set; }

		public string OriginalUnitVariantFileName { get; set; }

		public event ConfirmedVariantHandler ConfirmedVariant;

		protected override void awake()
		{
			base.awake();
			EditNameButton.onClick.AddListener(EditNameButtonClick);
			EditCargoButton.onClick.AddListener(EditCargoButtonClick);
			EditBaysButton.onClick.AddListener(EditBaysButtonClick);
			SaveButton.onClick.AddListener(SaveButtonClick);
			EditDescriptionButton.onClick.AddListener(EditDescriptionButtonClick);
		}

		private void SaveButtonClick()
		{
			if (OriginalUnitVariant == null && CustomUnitVariantIO.CustomUnitVariantSaveFileExists(CustomUnitVariant))
			{
				UIController.Instance.ShowMessageBox("A ship variant with the same name already exists. Overwrite the existing one?", MessageBoxButtons.OkCancel, (MessageBoxScreen screen, MessageBoxResult result) =>
				{
					if (result == MessageBoxResult.Ok)
					{
						TrySave();
					}
				}, MessageBoxIcon.Warning, "Save custom variant");
			}
			else
			{
				TrySave();
			}
		}

		private void TrySave()
		{
			if (EngineASX.Instance.UnitClasses.Any((UnitClass e) => e.UnitSeries == CustomUnitVariant.UnitClass.UnitSeries && e.className == CustomUnitVariant.Name))
			{
				UIController.Instance.ShowMessageBox("Name \"" + CustomUnitVariant.UnitClass.UnitSeries.Name + "-" + CustomUnitVariant.Name + "\" is already in use", MessageBoxButtons.Ok, null, MessageBoxIcon.Warning);
				return;
			}
			try
			{
				CustomUnitVariantIO.SaveCustomVariant(CustomUnitVariant);
				if (OriginalUnitVariant != null && OriginalUnitVariant.Name != CustomUnitVariant.Name && !string.IsNullOrWhiteSpace(OriginalUnitVariantFileName))
				{
					CustomUnitVariantIO.DeleteCustomVariant(OriginalUnitVariantFileName);
				}
				if (!string.IsNullOrWhiteSpace(OriginalUnitVariantFileName) && Path.GetExtension(OriginalUnitVariantFileName) == "." + CustomUnitVariantIO.LegacyExtension)
				{
					CustomUnitVariantIO.DeleteCustomVariant(OriginalUnitVariantFileName);
				}
				UIController.Instance.ShowMessageBox("Custom variant saved OK", MessageBoxButtons.Ok, null, MessageBoxIcon.Ok);
				IsItemDirty = false;
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to save custom unit variant: " + ex.Message);
			}
		}

		private void EditCargoButtonClick()
		{
			IsItemDirty = true;
			UIController.Instance.ScreenNavigator.ShowCreateUnitVariantCargoScreen(CustomUnitVariant);
		}

		private void EditBaysButtonClick()
		{
			IsItemDirty = true;
			UIController.Instance.ScreenNavigator.ShowCreateUnitVariantComponentsScreen(CustomUnitVariant);
		}

		private void EditNameButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen(CustomUnitVariant.Name, 1, 12, (RenameUnitScreen handler, bool rename, string newName) =>
			{
				if (rename)
				{
					CustomUnitVariant.Name = newName.Trim();
					RefreshCustomNameLabel();
					IsItemDirty = true;
				}
			}, "^[a-zA-Z0-9_-]*$", "The input text is invalid. Acceptable characters: a-z, A-Z, 0-9, '-' and '_'");
		}

		private void EditDescriptionButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameScreen((RenameUnitScreen screen) =>
			{
				screen.TitleText = "Enter description...";
				screen.MinCharacters = 0;
				screen.MaxCharacters = 200;
				screen.NewName = CustomUnitVariant.Description.Trim();
				screen.RenameConfirmed += (RenameUnitScreen handler, bool rename, string newName) =>
				{
					if (rename)
					{
						CustomUnitVariant.Description = newName;
						RefreshDescriptionLabel();
						IsItemDirty = true;
					}
				};
			});
		}

		protected override bool onNavigatingBack()
		{
			if (IsItemDirty && !TransientVariant)
			{
				UIController.Instance.ShowMessageBox("Quit without saving?", MessageBoxButtons.OkCancel, (MessageBoxScreen sender, MessageBoxResult result) =>
				{
					if (result == MessageBoxResult.Ok)
					{
						UIController.Instance.ScreenNavigator.NavigateBackTo(GetBackTarget());
					}
				});
				return false;
			}
			return base.onNavigatingBack();
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			if (!navigatedForward)
			{
				Refresh();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshSaveButton();
			DescriptionRoot.gameObject.SetActive(!TransientVariant);
			CustomNameRoot.gameObject.SetActive(!TransientVariant);
			if (CustomUnitVariant != null)
			{
				CostLabel.text = GetCostText();
				RefreshCustomNameLabel();
				RefreshDescriptionLabel();
				IconImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(CustomUnitVariant.UnitClass);
				BaysLabel.text = GetBaysText();
				CargoLabel.text = GetCargoText();
				BaseTypeLabel.text = CustomUnitVariant.UnitClass.GetClassAndSeriesName();
				TitleLabel.text = "Create custom " + CustomUnitVariant.UnitClass.GetSeriesName();
			}
		}

		public void CacheOriginalVariant()
		{
			OriginalUnitVariant = CustomUnitVariant.Clone();
		}

		protected override void update()
		{
			base.update();
			RefreshSaveButton();
		}

		private void RefreshSaveButton()
		{
			SaveButton.gameObject.SetActive(!TransientVariant && CustomUnitVariant != null && IsItemDirty);
		}

		private void RefreshCustomNameLabel()
		{
			CustomNameLabel.text = CustomUnitVariant.Name;
		}

		private void RefreshDescriptionLabel()
		{
			CustomDescriptionLabel.text = CustomUnitVariant.Description;
		}

		private string GetCargoText()
		{
			if (CustomUnitVariant.CargoItems.Count == 0)
			{
				return "[None]";
			}
			IOrderedEnumerable<CargoBayItem> source = from e in CustomUnitVariant.CargoItems
				where e.Quantity > 0
				orderby e.Quantity descending
				select e;
			return string.Join("<br>", source.Select((CargoBayItem e) => $"{e.CargoClass.ClassName} x {e.Quantity:N0}"));
		}

		private string GetBaysText()
		{
			IOrderedEnumerable<ComponentClass> source = from e in CustomUnitVariantHelper.GetAllComponentClasses(CustomUnitVariant)
				orderby e.ComponentBayType.OrderInTradeUI, e.BaseCost descending
				select e;
			return string.Join("<br>", source.Select((ComponentClass e) => e.GetFriendlyName()));
		}

		private string GetCostText()
		{
			return TextFormattingHelper.FormatCredits(CalculateCost(), includeSuffix: true);
		}

		private int CalculateCost()
		{
			return ShipBuyScreen.GetItemSaleCostPerUnit(EngineASX.Instance, CustomUnitVariantHelper.CalculateMoneyValue(CustomUnitVariant), null, null);
		}
	}
}
