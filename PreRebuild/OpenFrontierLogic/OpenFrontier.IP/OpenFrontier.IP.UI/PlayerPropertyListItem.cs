using System.Text;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Core.Units;
using OpenFrontier.IP.UI.Components;
using OpenFrontier.IP.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class PlayerPropertyListItem : ScrollListItem<Unit>
	{
		public bool AlwaysShowConditionController = true;

		public Graphic CustomPathIcon;

		public Graphic IdleGraphic;

		public Image UnitIcon;

		public Image UnitCargoIcon;

		public UnitConstructionControllerUI UnitConstructionController;

		public CloakImageComponent CloakImageComponent;

		public int OffsetPerDockLevel;

		public CargoUsageSlider CargoUsageSlider;

		public UnitConditionControllerUI UnitConditionController;

		public TextMeshProUGUI LocationLabel;

		public TextMeshProUGUI NameLabel;

		public TextMeshProUGUI FleetLabel;

		public TextMeshProUGUI OrderLabel;

		public Graphic CurrentShipGraphic;

		public Graphic UnderAttackGraphic;

		private int oldDockLevel;

		public RectTransform ContentRoot;

		private static StringBuilder locationStringBuilder = new StringBuilder();

		public PropertyScreen PropertyScreen
		{
			get
			{
				if (ParentList is PlayerPropertyList playerPropertyList)
				{
					return playerPropertyList.PropertyScreen;
				}
				return null;
			}
		}

		public bool ShowLocationText { get; set; }

		protected override void awake()
		{
			base.awake();
			UnderAttackGraphic.enabled = false;
			CurrentShipGraphic.enabled = false;
			OrderLabel.enabled = false;
			IdleGraphic.enabled = false;
			UnitCargoIcon.enabled = false;
			CloakImageComponent.enabled = false;
			UnitConditionController.gameObject.SetActive(value: false);
		}

		public override void Refresh()
		{
			base.Refresh();
			UnitIcon.sprite = Item.UnitClass.GetIconSprite();
			UnitConditionController.LocalUnit = Item;
			UnitCargoIcon.enabled = Item.CargoComponent != null && Item.CargoComponent.CargoClass != null;
			if (UnitCargoIcon.enabled)
			{
				UnitCargoIcon.sprite = Item.CargoComponent.CargoClass.GetCargoSpriteOrDefault();
			}
			CargoUsageSlider.RefreshFromCargoBayComponent(Item.CargoBayComponent);
			RefreshNameLabel();
			RefreshLocationLabel();
			RefreshOrderLabel();
			CurrentShipGraphic.enabled = Item.IsPlayerCurrentUnit;
			RefreshDockLevel();
			CloakImageComponent.TargetCloakComponent = Item.CloakComponent;
			UnderAttackGraphic.enabled = Item.IsUnderAttack();
			bool flag = AlwaysShowConditionController || (Item.IsStationOrShip() && (Item.IsShieldDamaged() || Item.IsHullDamaged()));
			UnitConditionController.gameObject.SetActive(flag);
			if (flag)
			{
				UnitConditionController.Tick();
			}
			UnitConstructionController.Unit = Item;
			UnitConstructionController.Refresh();
			CustomPathIcon.enabled = Item.IsPlayerCustomPathTarget();
		}

		public override void Tick()
		{
			base.Tick();
			if (UnitConditionController.gameObject.activeSelf)
			{
				UnitConditionController.Tick();
			}
			UnitConstructionController.Refresh();
			CloakImageComponent.Tick();
		}

		private void RefreshDockLevel()
		{
			int dockLevelOwnFaction = Item.GetDockLevelOwnFaction();
			if (dockLevelOwnFaction != oldDockLevel)
			{
				ContentRoot.SetLeft(dockLevelOwnFaction * OffsetPerDockLevel);
				oldDockLevel = dockLevelOwnFaction;
			}
		}

		public static string GetPropertyDisplayName(Unit unit)
		{
			return ShipListHelper.GetShipNameClassAndFleetWithEmbeddedFleetSprite(unit, shortName: true, omitFleetNameIfSingleShip: true);
		}

		private void RefreshNameLabel()
		{
			string propertyDisplayName = GetPropertyDisplayName(Item);
			NameLabel.text = propertyDisplayName;
			NameLabel.color = Item.Engine.OwnedColor;
			if (FleetLabel != null)
			{
				FleetLabel.text = ((Item.GetFleet() != null) ? Item.GetFleet().GetFriendlyName() : "");
			}
		}

		private void RefreshOrderLabel()
		{
			bool flag = OrdersHelper.HasFleetGotStatusToDisplay(Item);
			if (OrderLabel != null)
			{
				OrderLabel.enabled = flag;
				if (flag)
				{
					OrderLabel.text = OrdersHelper.GetOrdersTextAndFleetStatus(Item, EngineASX.Instance.LocalFaction);
				}
			}
			IdleGraphic.enabled = Item.UnitType == UnitType.Ship && !flag;
		}

		private void RefreshLocationLabel()
		{
			bool flag = ShouldShowLocationLabel();
			LocationLabel.enabled = flag;
			if (flag)
			{
				locationStringBuilder.Length = 0;
				GetSectorAndLocationTextOrDockNameNonAlloc(Item, locationStringBuilder);
				LocationLabel.SetText(locationStringBuilder);
			}
		}

		public void GetSectorAndLocationTextOrDockNameNonAlloc(Unit unit, StringBuilder stringBuilder)
		{
			if (unit.IsStatic || !unit.IsDocked)
			{
				unit.GetSectorAndLocationTextNonAlloc(stringBuilder);
				stringBuilder.Append(UnitNamer.AppendJumpDistanceIfMoreThanZero(EngineASX.Instance.ActiveSector, unit.Sector, EngineASX.Instance.LocalFaction));
				return;
			}
			stringBuilder.Append(unit.GetDockUnit().GetFriendlyName());
			stringBuilder.Append(" - ");
			stringBuilder.Append(unit.Sector.Name);
			stringBuilder.Append(UnitNamer.AppendJumpDistanceIfMoreThanZero(EngineASX.Instance.ActiveSector, unit.Sector, EngineASX.Instance.LocalFaction));
		}

		private bool ShouldShowLocationLabel()
		{
			Unit dockUnit = Item.GetDockUnit();
			if (dockUnit != null)
			{
				if (dockUnit.Faction != Item.Faction)
				{
					return true;
				}
				switch (dockUnit.UnitType)
				{
				case UnitType.Ship:
					return false;
				case UnitType.Station:
				{
					PropertyScreen propertyScreen = PropertyScreen;
					if (propertyScreen != null)
					{
						return propertyScreen.ShowStationsToggle.isOn;
					}
					break;
				}
				}
				return false;
			}
			return true;
		}

		public override void CleanupOnDisable()
		{
			UnitConditionController.CleanupOnDisable();
		}
	}
}
