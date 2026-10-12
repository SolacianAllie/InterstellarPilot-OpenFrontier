using System.Linq;
using System.Collections.Generic;
using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Extensions;
using OpenFrontier.Unity.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.ComponentTrade
{
	public class ComponentTradeScreen : EngineScreen
	{
		public bool IgnoreCost;

		public bool IgnoreCompatibility;

		public GameObject BayDetailsRoot;

		private Unit unit;

		public Unit DockUnit;

		public ComponentTradeBayList BayList;

		private UnitComponentTrader componentTrader;

		public Button BuyButton;

		public Text BuyCostLabel;

		public GameObject BuyRoot;

		public Text ComponentBayLabel;

		private ComponentBay currentBay;

		public string IncompatibleLimitReachedText = "An additional component of this type cannot be installed";

		public string IncompatibleRatingText = "This component is not compatible with the current bay";

		public Button InstalledItemInfoButton;

		public Text InstalledLabel;

		public ComponentTradeItemList AvailableComponentsList;

		/// <summary>
		/// Picks which family of hardware the bay list is showing.
		/// </summary>
		/// <remarks>
		/// Optional, and null means "do not offer the choice" - the unit variant
		/// editor reuses this screen with a fixed set of bays and should not
		/// grow a filter nobody asked for. Left null it also costs nothing: the
		/// list is then built exactly as it was, all bays, which is what the
		/// editor wants anyway.
		/// </remarks>
		public TMP_Dropdown BayTierDropdown;

		/// <summary>
		/// The family on screen. Defaults to Legacy, which is what every bay in
		/// the game is today, so the list is unchanged until bays are classified.
		/// </summary>
		private BayTier selectedBayTier = BayTier.Legacy;

		public GameObject SelectedComponentRoot;

		public Button SelectedItemInfoButton;

		public Text SelectedLabel;

		public Button SellButton;

		public Text SellPriceLabel;

		public GameObject SellRoot;

		public Transform SellCostRoot;

		public Transform BuyCostRoot;

		public Text Title;

		public Faction DockFaction
		{
			get
			{
				if (DockUnit != null)
				{
					return DockUnit.Faction;
				}
				return null;
			}
		}

		public UnitComponentTrader ComponentTrader
		{
			get
			{
				return componentTrader;
			}
			set
			{
				if (componentTrader != value)
				{
					componentTrader = value;
				}
			}
		}

		public ComponentBay CurrentBay
		{
			get
			{
				return currentBay;
			}
			set
			{
				if (currentBay != value)
				{
					currentBay = value;
					CurrentComponentClass = null;
				}
			}
		}

		public ComponentClass CurrentComponentClass
		{
			get
			{
				return AvailableComponentsList.FirstSelectedItem;
			}
			set
			{
				AvailableComponentsList.FirstSelectedItem = value;
			}
		}

		public Unit UpgradingUnit
		{
			get
			{
				return unit;
			}
			set
			{
				if (unit != value)
				{
					unit = value;
				}
			}
		}

		public int GetBuyPrice(ComponentClass componentClass)
		{
			return ComponentTradeHelper.GetComponentClassSaleCost(Eng, componentClass, DockFaction, Eng.LocalFaction);
		}

		public int GetSellPrice(ComponentBase component)
		{
			return ComponentTradeHelper.GetComponentBuyCost(Eng, component, Eng.LocalFaction, DockFaction);
		}

		public void BuyComponent()
		{
			if (CurrentBay != null && AvailableComponentsList.FirstSelectedItem != null)
			{
				BuyNewComponent(CurrentBay, AvailableComponentsList.FirstSelectedItem);
				Refresh();
			}
		}

		private void RefreshIsModified()
		{
			currentBay.UnitComponents.IsModified = currentBay.UnitComponents.CalculateIsModded();
		}

		public bool IsSelectedComponentAffordable()
		{
			return IsComponentAffordable(AvailableComponentsList.FirstSelectedItem);
		}

		public bool IsComponentAffordable(ComponentClass componentClass)
		{
			if (IgnoreCost)
			{
				return true;
			}
			return Eng.LocalPlayer.Credits >= GetInstallCost(componentClass);
		}

		public void BuyNewComponent(ComponentBay bay, ComponentClass componentClass)
		{
			Debug.Log($"Player buying component: {componentClass} for bay {bay}", this);
			int num = 0;
			if (bay.InstalledComponent != null)
			{
				num = GetSellPrice(bay.InstalledComponent);
				SellInstalledComponent(bay, changeCredits: false);
			}
			bay.InstallComponent(componentClass).UserPowered = true;
			if (!IgnoreCost)
			{
				int buyPrice = GetBuyPrice(componentClass);
				int creditsChange = num - buyPrice;
				ChangeCreditsAndAddMessage(componentClass, installed: true, creditsChange);
			}
			RefreshIsModified();
		}

		public void ChangeCreditsAndAddMessage(ComponentClass componentClass, bool installed, int creditsChange)
		{
			Eng.RegisterTaxedPlayerTrade(DockUI.PlayerDockUnit, creditsChange, FactionTransactionType.EquipmentPurchase);
		}

		public bool SellInstalledComponent(ComponentBay bay, bool changeCredits)
		{
			if (bay != null && bay.InstalledComponent != null)
			{
				if (changeCredits)
				{
					int sellPrice = GetSellPrice(bay.InstalledComponent);
					ChangeCreditsAndAddMessage(bay.InstalledComponent.ComponentClass, installed: false, sellPrice);
				}
				bay.DestroyInstalledComponent();
				RefreshIsModified();
				return true;
			}
			return false;
		}

		public void SellInstalled()
		{
			SellInstalledComponent(currentBay, !IgnoreCost);
			Refresh();
		}

		public ComponentBay[] GetSelectableBays()
		{
			if (unit != null)
			{
				return ShipComponentsHelper.GetUnitVisibleBays(unit).ToArray();
			}
			return new ComponentBay[0];
		}

		public void ChangeComponentBay(int change)
		{
			ComponentBay[] selectableBays = GetSelectableBays();
			if (selectableBays.Length != 0)
			{
				int num = Maths.WrapValue(selectableBays.IndexOf(currentBay) + change, 0, selectableBays.Length);
				if (num >= 0 && num < selectableBays.Length)
				{
					CurrentBay = selectableBays[num];
				}
			}
		}

		public ComponentClass SwitchItem(int movement)
		{
			return SwitchItem(movement, AvailableComponentsList.FirstSelectedItem);
		}

		public ComponentClass SwitchItem(int movement, ComponentClass curComponentClass)
		{
			if (AvailableComponentsList.ActiveItems.Count > 0)
			{
				int num = AvailableComponentsList.ActiveItems.IndexOf(curComponentClass);
				num += movement;
				num = Maths.WrapValue(num, 0, AvailableComponentsList.ActiveItems.Count);
				return AvailableComponentsList.ActiveItems[num];
			}
			return null;
		}

		public bool IsComponentCompatible(ComponentBay componentBay, ComponentClass componentClass, bool considerRating)
		{
			string error = null;
			return IsComponentCompatible(componentBay, componentClass, considerRating, out error);
		}

		public bool IsComponentCompatible(ComponentBay componentBay, ComponentClass componentClass, bool considerRating, out string error)
		{
			error = null;
			if (!componentBay.IsComponentCorrectType(componentClass))
			{
				error = IncompatibleRatingText;
				return false;
			}
			if (considerRating && !componentBay.IsComponentCorrectRating(componentClass))
			{
				error = IncompatibleRatingText;
				return false;
			}
			if (componentBay.InstalledComponent == null && !componentBay.Unit.Components.SupportsAdditionalComponentOfType(componentClass.ComponentType))
			{
				error = IncompatibleLimitReachedText;
				return false;
			}
			return true;
		}

		protected override void awake()
		{
			base.awake();
			SellButton.onClick.AddListener(SellButton_Activated);
			BuyButton.onClick.AddListener(BuyButton_Activated);
			SelectedItemInfoButton.onClick.AddListener(SelectedItemInfoButtonActivated);
			InstalledItemInfoButton.onClick.AddListener(InstalledItemInfoButtonActivated);
			AvailableComponentsList.SelectedItemChanged += AvailableComponentsListSelectedItemChanged;
			BayList.SelectedItemChanged += BayList_SelectedItemChanged;

			if (BayTierDropdown != null)
			{
				PopulateBayTierDropdown();
				BayTierDropdown.onValueChanged.AddListener(BayTierDropdown_Changed);
			}
		}

		/// <summary>
		/// Fills the filter from the enum, so the list of options cannot fall
		/// out of step with what the code accepts.
		/// </summary>
		/// <remarks>
		/// Order is the declaration order of BayTier, which puts Legacy first -
		/// the one every existing bay is, and so the one that has to be selected
		/// on open or the first run of the game loses its shields.
		/// </remarks>
		private void PopulateBayTierDropdown()
		{
			if (BayTierDropdown == null || BayTierDropdown.options.Count == System.Enum.GetValues(typeof(BayTier)).Length)
			{
				return;
			}

			BayTierDropdown.ClearOptions();

			List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

			foreach (BayTier tier in System.Enum.GetValues(typeof(BayTier)))
			{
				options.Add(new TMP_Dropdown.OptionData(BayTierLabel(tier)));
			}

			BayTierDropdown.AddOptions(options);
			BayTierDropdown.value = (int)selectedBayTier;
		}

		/// <summary>
		/// What a tier is called in the dropdown.
		/// </summary>
		/// <remarks>
		/// On purpose, because "OptionalInternal" is not a word and
		/// "UtilityMount" reads as a file format. Spaced and pluralised to match
		/// how the rest of this UI labels things.
		/// </remarks>
		private static string BayTierLabel(BayTier tier)
		{
			switch (tier)
			{
				case BayTier.Legacy:
					return "LEGACY";

				case BayTier.Hardpoint:
					return "Hardpoints";

				case BayTier.CoreInternal:
					return "Core Internals";

				case BayTier.OptionalInternal:
					return "Optional Internals";

				case BayTier.UtilityMount:
					return "Utility Mounts";

				default:
					return tier.ToString();
			}
		}

		private void BayTierDropdown_Changed(int index)
		{
			System.Array tiers = System.Enum.GetValues(typeof(BayTier));
			if (index < 0 || index >= tiers.Length)
			{
				return;
			}

			selectedBayTier = (BayTier)tiers.GetValue(index);
			RefreshBayList();
			AutoSelectBayIfNull();
		}

		private void BayList_SelectedItemChanged(ScrollList<object> sender, object oldItem, object newItem)
		{
			// The list holds headings as well as bays. A heading is not a bay,
			// so this is the one place the cast can come back empty, and what it
			// gets instead of a bay is nothing.
			//
			// It is reachable and not just theoretically: ScrollList's
			// TrySelectFirstItem takes activeItems[0], and the first row of a
			// filtered list is a heading. Its toggle is made
			// non-interactable so a pointer cannot get here, but a
			// programmatic selection still can.
			ComponentBay bay = newItem as ComponentBay;
			CurrentBay = bay;

			if (bay == null)
			{
				// Move to a real bay rather than sitting on a heading with the
				// details panel empty and the right-hand list showing the last
				// bay's contents.
				AutoSelectNextBay();
				return;
			}

			RefreshCurrentBayInfo();
			AvailableComponentsList.Refresh();
			AvailableComponentsList.ResetScrollPosition();
		}

		protected override void update()
		{
			base.update();
			if (currentBay != null)
			{
				RefreshCurrentBayInfo();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			RefreshBayList();
			RefreshTitle();
			if (ComponentTrader != null)
			{
				AutoSelectBayIfNull();
				RefreshCurrentBayInfo();
			}
			SellCostRoot.gameObject.SetActive(!IgnoreCost);
			BuyCostRoot.gameObject.SetActive(!IgnoreCost);
			if (IgnoreCost)
			{
				SellButton.SetText("Remove");
				BuyButton.SetText("Install");
			}
			else
			{
				SellButton.SetText("Sell");
				BuyButton.SetText("Buy");
			}
		}

		private void RefreshTitle()
		{
			Title.text = "Upgrade " + unit.GetFriendlyName();
		}

		private void AutoSelectBayIfNull()
		{
			if (currentBay == null)
			{
				AutoSelectNextBay();
			}
		}

		private void RefreshBayList()
		{
			// The list holds one of two things per row - a bay, or a heading
			// above a run of bays of the same type - so it is built rather than
			// passed straight through. The order it needs is the one
			// GetSelectableBays already produced, and the tier is a hard
			// filter: the dropdown chooses what is on screen rather than
			// rearranging it.
			BayList.SetItems(ShipComponentsHelper.BuildBayListItems(GetSelectableBays(), selectedBayTier));
		}

		private void RefreshCurrentBayInfo()
		{
			BayDetailsRoot.SetActive(currentBay != null);
			if (currentBay != null)
			{
				RefreshBayNameLabel(currentBay);
				RefreshInstalledComponentInfo(currentBay.InstalledComponent);
				SellRoot.gameObject.SetActive(currentBay.InstalledComponent != null && currentBay.InstalledComponent.ComponentClass.ComponentType.AllowSell);
				SelectedComponentRoot.gameObject.SetActive(AvailableComponentsList.FirstSelectedItem != null);
				if (AvailableComponentsList.FirstSelectedItem != null)
				{
					bool flag = AvailableComponentsList.FirstSelectedItem != null && (currentBay.InstalledComponent == null || AvailableComponentsList.FirstSelectedItem != currentBay.InstalledComponent.ComponentClass);
					BuyRoot.gameObject.SetActive(flag);
					if (flag)
					{
						BuyCostLabel.text = GetBuyCostText();
						BuyCostLabel.color = (IsSelectedComponentAffordable() ? Eng.GameSettings.AffordableColor : Eng.GameSettings.UnaffordableColor);
						BuyButton.interactable = CanBuyComponent(AvailableComponentsList.FirstSelectedItem);
					}
					SelectedLabel.text = AvailableComponentsList.FirstSelectedItem.GetFriendlyName();
				}
			}
			AvailableComponentsList.RefreshVisibleItems();
		}

		private string GetBuyCostText()
		{
			if (IgnoreCost)
			{
				return "-";
			}
			return TextFormattingHelper.FormatCredits(GetInstallCost(AvailableComponentsList.FirstSelectedItem), includeSuffix: true);
		}

		private void AvailableComponentsListSelectedItemChanged(ScrollList<ComponentClass> sender, ComponentClass oldItem, ComponentClass newItem)
		{
			RefreshCurrentBayInfo();
		}

		private void SelectedItemInfoButtonActivated()
		{
			UIController.Instance.ScreenNavigator.ShowComponentInfoScreen(AvailableComponentsList.FirstSelectedItem);
		}

		private void InstalledItemInfoButtonActivated()
		{
			UIController.Instance.ScreenNavigator.ShowComponentInfoScreen(CurrentBay.InstalledComponent.ComponentClass);
		}

		private void BuyButton_Activated()
		{
			BuyComponent();
		}

		private void SellButton_Activated()
		{
			SellInstalled();
		}

		private bool CanBuyComponent(ComponentClass componentClass)
		{
			if (IsComponentAffordable(componentClass))
			{
				return IsComponentCompatible(currentBay, componentClass, !IgnoreCompatibility);
			}
			return false;
		}

		private int GetInstallCost(ComponentClass componentClass)
		{
			int num = GetBuyPrice(componentClass);
			if (currentBay.InstalledComponent != null)
			{
				num -= GetSellPrice(currentBay.InstalledComponent);
			}
			return num;
		}

		private void AutoSelectNextBay()
		{
			if (unit != null)
			{
				if (currentBay == null || currentBay.Unit != unit)
				{
					CurrentBay = null;
					NextBay();
				}
			}
			else
			{
				CurrentBay = null;
			}
		}

		public void NextBay()
		{
			ChangeComponentBay(1);
		}

		public void PreviousBay()
		{
			ChangeComponentBay(-1);
		}

		private void RefreshBayNameLabel(ComponentBay componentBay)
		{
			if (componentBay != null)
			{
				ComponentBayLabel.text = componentBay.GetFriendlyName();
			}
		}

		private void RefreshInstalledComponentInfo(ComponentBase component)
		{
			InstalledItemInfoButton.gameObject.SetActive(component != null);
			if (component != null)
			{
				int sellPrice = GetSellPrice(component);
				SellPriceLabel.text = GetSellPriceText(sellPrice);
				SellPriceLabel.color = Eng.GameSettings.AffordableColor;
				InstalledLabel.text = component.ComponentClass.GetFriendlyName();
			}
			else
			{
				InstalledLabel.text = "None";
				SellPriceLabel.text = string.Empty;
			}
		}

		private string GetSellPriceText(int sellPrice)
		{
			if (IgnoreCost)
			{
				return "-";
			}
			return TextFormattingHelper.FormatCredits(sellPrice, includeSuffix: true);
		}
	}
}
