using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CustomUnitVariants;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.Unity.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.CreateUnitVariant
{
	public class CreateUnitVariantComponentsScreen : EngineScreen
	{
		public GameObject BayDetailsRoot;

		private CustomUnitVariant unitVariant;

		public CreateUnitVariantComponentBayList BayList;

		private UnitComponentTrader componentTrader;

		public Button SwapComponentButton;

		public Text SelectedComponentCostLabel;

		public GameObject SwapRoot;

		public Text SwapButtonText;

		public Text ComponentBayLabel;

		private ComponentBay currentBay;

		public string IncompatibleLimitReachedText = "An additional component of this type cannot be installed";

		public string IncompatibleRatingText = "This component is not compatible with the current bay";

		public Button InstalledItemInfoButton;

		public Text InstalledLabel;

		public CreateUnitVariantComponentList AvailableComponentsList;

		public GameObject SelectedComponentRoot;

		public Button SelectedItemInfoButton;

		public Text SelectedComponentLabel;

		public Button RemoveButton;

		public Text InstalledComponentValueLabel;

		public GameObject RemoveComponentRoot;

		public Text Title;

		public Transform InstalledComponentRoot;

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

		public ComponentClass CurrentBayComponentClass
		{
			get
			{
				if (currentBay != null)
				{
					return CustomUnitVariantHelper.GetBayComponentClass(unitVariant, currentBay);
				}
				return null;
			}
		}

		public CustomUnitVariant UnitVariant
		{
			get
			{
				return unitVariant;
			}
			set
			{
				if (unitVariant != value)
				{
					unitVariant = value;
				}
			}
		}

		public int GetBuyPrice(ComponentClass componentClass)
		{
			return ComponentTradeHelper.GetComponentClassSaleCost(Eng, componentClass, null, null);
		}

		public int GetSellPrice(ComponentClass componentClass)
		{
			return ComponentTradeHelper.GetComponentClassBuyCost(Eng, componentClass, null, null);
		}

		public void SwapComponent()
		{
			if (CurrentBay != null && AvailableComponentsList.FirstSelectedItem != null)
			{
				SwapComponent(CurrentBay, AvailableComponentsList.FirstSelectedItem);
				Refresh();
			}
		}

		public void SwapComponent(ComponentBay bay, ComponentClass componentClass)
		{
			Debug.Log($"Swapping component: {componentClass} for bay {bay}", this);
			CustomUnitVariantHelper.SetComponentClass(unitVariant, bay, componentClass);
		}

		public bool RemoveInstalledComponent(ComponentBay bay)
		{
			if (bay != null && CustomUnitVariantHelper.GetBayComponentClass(unitVariant, bay) != null)
			{
				CustomUnitVariantHelper.RemoveComponentClass(unitVariant, bay);
				return true;
			}
			return false;
		}

		public void RemoveInstalled()
		{
			RemoveInstalledComponent(currentBay);
			Refresh();
		}

		public ComponentBay[] GetSelectableBays()
		{
			if (unitVariant != null)
			{
				return (from e in unitVariant.UnitClass.UnitPrefab.GetComponentsInChildren<ComponentBay>()
					where !e.BayType.IgnoreInTradeUI
					orderby e.BayType.OrderInTradeUI, e.name
					select e).ToArray();
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

		public bool IsComponentCompatible(ComponentBay componentBay, ComponentClass componentClass)
		{
			string error;
			return CustomUnitVariantHelper.IsComponentCompatible(unitVariant, componentBay, componentClass, out error);
		}

		protected override void awake()
		{
			base.awake();
			RemoveButton.onClick.AddListener(RemoveButton_Activated);
			SwapComponentButton.onClick.AddListener(SwapComponentButtonClick);
			SelectedItemInfoButton.onClick.AddListener(SelectedItemInfoButtonActivated);
			InstalledItemInfoButton.onClick.AddListener(InstalledItemInfoButtonActivated);
			AvailableComponentsList.SelectedItemChanged += AvailableComponentsListSelectedItemChanged;
			BayList.SelectedItemChanged += BayList_SelectedItemChanged;
		}

		private void BayList_SelectedItemChanged(ScrollList<ComponentBay> sender, ComponentBay oldItem, ComponentBay newItem)
		{
			CurrentBay = newItem;
			RefreshCurrentBayInfo();
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
		}

		private void RefreshTitle()
		{
			Title.text = "Customize " + unitVariant.FullName + " components";
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
			ComponentBay[] selectableBays = GetSelectableBays();
			BayList.SetItems(selectableBays);
		}

		private void RefreshCurrentBayInfo()
		{
			BayDetailsRoot.SetActive(currentBay != null);
			if (currentBay != null)
			{
				RefreshBayNameLabel(currentBay);
				RefreshInstalledComponentInfo(CurrentBayComponentClass);
				InstalledComponentRoot.gameObject.SetActive(CurrentBayComponentClass != null);
				RemoveComponentRoot.gameObject.SetActive(CurrentBayComponentClass != null && CurrentBayComponentClass.ComponentType.AllowSell);
				SelectedComponentRoot.gameObject.SetActive(AvailableComponentsList.FirstSelectedItem != null);
				if (AvailableComponentsList.FirstSelectedItem != null)
				{
					bool flag = AvailableComponentsList.FirstSelectedItem != null && (CurrentBayComponentClass == null || AvailableComponentsList.FirstSelectedItem != CurrentBayComponentClass);
					SwapRoot.gameObject.SetActive(flag);
					if (flag)
					{
						SelectedComponentCostLabel.text = TextFormattingHelper.FormatCredits(GetBuyPrice(AvailableComponentsList.FirstSelectedItem), includeSuffix: true);
						SwapComponentButton.interactable = CanSwapComponent(AvailableComponentsList.FirstSelectedItem);
						SwapButtonText.text = ((CurrentBayComponentClass != null) ? "Swap" : "Add");
					}
					SelectedComponentLabel.text = AvailableComponentsList.FirstSelectedItem.GetFriendlyName();
				}
			}
			AvailableComponentsList.Refresh();
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

		private void SwapComponentButtonClick()
		{
			SwapComponent();
		}

		private void RemoveButton_Activated()
		{
			RemoveInstalled();
		}

		private bool CanSwapComponent(ComponentClass componentClass)
		{
			return IsComponentCompatible(currentBay, componentClass);
		}

		private void AutoSelectNextBay()
		{
			if (unitVariant != null)
			{
				if (currentBay == null)
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

		private void RefreshInstalledComponentInfo(ComponentClass componentClass)
		{
			InstalledItemInfoButton.gameObject.SetActive(componentClass != null);
			if (componentClass != null)
			{
				int sellPrice = GetSellPrice(componentClass);
				InstalledComponentValueLabel.text = TextFormattingHelper.FormatCredits(sellPrice, includeSuffix: true);
				InstalledLabel.text = componentClass.GetFriendlyName();
			}
			else
			{
				InstalledLabel.text = "None";
				InstalledComponentValueLabel.text = string.Empty;
			}
		}
	}
}
