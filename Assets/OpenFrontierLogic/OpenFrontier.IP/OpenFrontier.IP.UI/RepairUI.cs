using OpenFrontier.IP.Common.Factions;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.Engine.UnitComponents;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class RepairUI : EngineScreen
	{
		public Text UnitNameText;

		public UnitConditionControllerUI ConditionController;

		public Button ShieldRechargeButton;

		public Text ShieldRechargeCostText;

		public Text ConditionLabel;

		private RepairItem currentRepairItem;

		public RepairUIItemList ItemList;

		public Text PartNameLabel;

		public Button RepairAllButton;

		public Text RepairAllCostLabel;

		public Button RepairButton;

		public Text RepairCostLabel;

		public Unit Unit;

		public RepairItem CurrentRepairItem
		{
			get
			{
				return currentRepairItem;
			}
			set
			{
				if (currentRepairItem != value)
				{
					currentRepairItem = value;
				}
			}
		}

		public bool CanAffordToRepairCurrentItem()
		{
			return Eng.LocalPlayer.Credits >= GetItemRepairCost(currentRepairItem);
		}

		public bool CanAffordRepairAll()
		{
			return CanAffordCost(CalculateRepairAllCost(Unit));
		}

		public bool CanAffordCost(int cost)
		{
			return Eng.LocalPlayer.Credits >= cost;
		}

		public string GetComponentPartName(ComponentBase component)
		{
			if (component == null)
			{
				return "Hull";
			}
			return component.ComponentClass.GetFriendlyName();
		}

		public bool AreRepairsNeeded(UnitComponentHolder componentHolder)
		{
			return DockUI.AreRepairsNeeded(componentHolder);
		}

		public bool AreRepairsNeededToCurrentItem()
		{
			return GetCurrentComponentCondition(currentRepairItem) < 1f;
		}

		public bool IsShieldRechargeNeeded()
		{
			if (Unit.Components.ShieldComponent != null)
			{
				return !Unit.Components.ShieldComponent.IsFullyCharged;
			}
			return false;
		}

		public bool CanRechargeShield()
		{
			if (IsShieldRechargeNeeded())
			{
				return CanAffordShieldRecharge();
			}
			return false;
		}

		public bool CanAffordShieldRecharge()
		{
			Faction faction = Unit.Faction;
			int costToRechargeShield = RepairHelper.GetCostToRechargeShield(DockUI.PlayerDockUnit.Faction, faction, DockUI.PlayerDockUnit, Unit);
			return Eng.LocalPlayer.Faction.Credits >= costToRechargeShield;
		}

		public void RepairAll()
		{
			if (!GetAllowRepairAll() || currentRepairItem == null || !(Unit != null))
			{
				return;
			}
			int repairCost = CalculateRepairAllCost(Unit);
			foreach (RepairItem activeItem in ItemList.ActiveItems)
			{
				RepairItem(activeItem);
			}
			OnPurchaseRepairs(repairCost);
		}

		private void OnPurchaseRepairs(int repairCost)
		{
			Eng.RegisterTaxedPlayerTrade(DockUI.PlayerDockUnit, -repairCost, FactionTransactionType.ShipRepairs);
			Refresh();
		}

		public void Repair()
		{
			if (Unit != null && GetAllowRepairCurrentItem() && currentRepairItem != null)
			{
				int itemRepairCost = GetItemRepairCost(currentRepairItem);
				RepairCurrentItem();
				OnPurchaseRepairs(itemRepairCost);
			}
		}

		private void RepairCurrentItem()
		{
			RepairItem(currentRepairItem);
		}

		private void RepairItem(RepairItem item)
		{
			if (item.Component == null)
			{
				Unit.Destructable.RestoreHealth();
			}
			else
			{
				item.Component.RestoreHealth();
			}
		}

		public float GetCurrentComponentCondition(RepairItem item)
		{
			return GetCurrentComponentCondition(Unit, item.Component);
		}

		public float GetCurrentComponentCondition(Unit unit, ComponentBase component)
		{
			if (component == null)
			{
				return unit.Destructable.HealthNormalized;
			}
			return component.HealthNormalized;
		}

		public int CalculateRepairAllCost(Unit unit)
		{
			int num = 0;
			foreach (RepairItem activeItem in ItemList.ActiveItems)
			{
				num += GetItemRepairCost(activeItem);
			}
			return num;
		}

		public int GetItemRepairCost(RepairItem item)
		{
			Faction faction = Unit.Faction;
			Faction faction2 = DockUI.PlayerDockUnit.Faction;
			if (item.Component != null)
			{
				return RepairHelper.GetComponentRepairCost(faction2, faction, DockUI.PlayerDockUnit, item.Component);
			}
			return RepairHelper.GetHullRepairCost(faction2, faction, DockUI.PlayerDockUnit, Unit);
		}

		protected override void start()
		{
			base.start();
			RepairButton.onClick.AddListener(Repair);
			RepairAllButton.onClick.AddListener(RepairAll);
			ShieldRechargeButton.onClick.AddListener(RechargeShields);
			ItemList.SelectedItemChanged += ItemList_SelectedItemChanged;
			if (ItemList.FirstSelectedItem != null)
			{
				ItemList_SelectedItemChanged(ItemList, null, ItemList.FirstSelectedItem);
			}
		}

		protected override void update()
		{
			base.update();
			if (Unit != null && Unit.IsValidAndNotDestroyed)
			{
				RefreshCurrentRepairItemData();
				if (RepairButton != null)
				{
					RepairButton.interactable = GetAllowRepairCurrentItem();
				}
				if (RepairAllButton != null)
				{
					RepairAllButton.interactable = GetAllowRepairAll();
				}
				RefreshShieldRechargeInfo();
				ConditionController.LocalUnit = Unit;
			}
		}

		private void RefreshShieldRechargeInfo()
		{
			bool flag = IsShieldRechargeNeeded();
			bool flag2 = CanAffordShieldRecharge();
			ShieldRechargeButton.interactable = flag & flag2;
			RefreshShieldRechargeCost(flag);
			SetCostLabelColor(ShieldRechargeCostText, flag2);
		}

		private void RefreshShieldRechargeCost(bool shieldRechargeNeeded)
		{
			if (shieldRechargeNeeded)
			{
				Faction faction = Unit.Faction;
				Faction faction2 = DockUI.PlayerDockUnit.Faction;
				ShieldRechargeCostText.text = TextFormattingHelper.FormatCredits(RepairHelper.GetCostToRechargeShield(faction2, faction, DockUI.PlayerDockUnit, Unit));
			}
			else
			{
				ShieldRechargeCostText.text = "-";
			}
		}

		protected override void refresh()
		{
			base.refresh();
			if (DockUI != null)
			{
				Unit = DockUI.PlayerCurrentUnit;
			}
			ItemList.Refresh();
			RefreshCurrentRepairItemData();
			if (Unit != null)
			{
				UnitNameText.text = Unit.GetFriendlyName();
			}
		}

		private void RepairSlider_ValueChanged(float delta)
		{
			RefreshRepairCostLabel();
		}

		private void ItemList_SelectedItemChanged(ScrollList<RepairItem> sender, RepairItem oldItem, RepairItem newItem)
		{
			CurrentRepairItem = newItem;
		}

		private void RefreshRepairCostLabel()
		{
			if (Unit != null)
			{
				RepairAllCostLabel.text = TextFormattingHelper.FormatCredits(CalculateRepairAllCost(Unit));
				SetCostLabelColor(RepairAllCostLabel, CanAffordRepairAll());
				if (currentRepairItem != null)
				{
					int itemRepairCost = GetItemRepairCost(currentRepairItem);
					RepairCostLabel.text = TextFormattingHelper.FormatCredits(itemRepairCost);
					SetCostLabelColor(RepairCostLabel, CanAffordToRepairCurrentItem());
				}
				else
				{
					RepairCostLabel.text = "-";
					SetCostLabelColor(RepairCostLabel, affordable: true);
				}
			}
		}

		private void SetCostLabelColor(Text label, bool affordable)
		{
			label.color = (affordable ? Eng.GameSettings.AffordableColor : Eng.GameSettings.UnaffordableColor);
		}

		private void RefreshConditionLabel()
		{
			if (currentRepairItem != null && Unit != null)
			{
				ConditionLabel.text = $"{GetCurrentComponentCondition(Unit, currentRepairItem.Component):P1}";
			}
			else
			{
				ConditionLabel.text = "-";
			}
		}

		private bool GetAllowRepairCurrentItem()
		{
			if (currentRepairItem != null && AreRepairsNeededToCurrentItem() && Unit != null)
			{
				return CanAffordToRepairCurrentItem();
			}
			return false;
		}

		private bool GetAllowRepairAll()
		{
			if (Unit != null)
			{
				if (ItemList.ActiveItems.Count > 0)
				{
					return CanAffordRepairAll();
				}
				return false;
			}
			return false;
		}

		private void RefreshPartNameLabel()
		{
			if (currentRepairItem != null)
			{
				string componentPartName = GetComponentPartName(currentRepairItem.Component);
				if (currentRepairItem.Component != null)
				{
					PartNameLabel.text = $"{componentPartName} ({GetComponentType()})";
				}
				else
				{
					PartNameLabel.text = componentPartName;
				}
			}
			else
			{
				PartNameLabel.text = null;
			}
		}

		private string GetComponentType()
		{
			if (currentRepairItem != null && currentRepairItem.Component != null)
			{
				return currentRepairItem.Component.ComponentClass.ComponentType.FriendlyName;
			}
			return null;
		}

		private void RefreshCurrentRepairItemData()
		{
			RefreshConditionLabel();
			RefreshRepairCostLabel();
			RefreshPartNameLabel();
		}

		public void RechargeShields()
		{
			if (CanRechargeShield())
			{
				Faction faction = Unit.Faction;
				int costToRechargeShield = RepairHelper.GetCostToRechargeShield(DockUI.PlayerDockUnit.Faction, faction, DockUI.PlayerDockUnit, Unit);
				Unit.Components.ShieldComponent.RechargeFull();
				OnPurchaseRepairs(costToRechargeShield);
			}
		}
	}
}
