using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.CargoFactory;
using OpenFrontier.IP.Engine.UnitComponents;
using OpenFrontier.IP.UI.Screens.CargoFactory;
using OpenFrontier.IP.UI.Screens.UnitCargo;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI.Screens.UnitInfo
{
	public class UnitInfoScreen : ScreenBase
	{
		private const string NoDescriptionText = "No information available";

		public Toggle HangarButton;

		public Toggle BaysButton;

		public Toggle ShowCargoButton;

		public ShipInfoBaysListUI BaysListUI;

		public HangarBayList HangarBaysList;

		public CargoFactoryProfileItemListUI CargoFactoryProfileList;

		public GameObject ComponentBaysRoot;

		public GameObject HangarBaysRoot;

		public GameObject CargoRoot;

		public GameObject FactoryRoot;

		public Text ClassLabel;

		public Toggle DescriptionButton;

		public Toggle FactoryToggle;

		public Text DescriptionLabel;

		public GameObject DescriptionRoot;

		public Text ManufacturerLabel;

		private PanelMode panelMode;

		public ActiveUnitPreviewerUI Previewer;

		public Image ShipImage;

		public Text ShipNameLabel;

		public ShipStatsGenerator ShipStatsGenerator;

		public bool ShowingStats = true;

		public Toggle StatsButton;

		public GameObject StatsRoot;

		public Unit Unit;

		public ShipCargoItemList CargoItemList;

		public UnitClass UnitClass
		{
			get
			{
				if (Unit != null)
				{
					return Unit.UnitClass;
				}
				return null;
			}
		}

		public PanelMode PanelMode
		{
			get
			{
				return panelMode;
			}
			set
			{
				if (panelMode != value)
				{
					panelMode = value;
					switch (panelMode)
					{
					case PanelMode.Cargo:
						ShowCargoButton.isOn = true;
						break;
					case PanelMode.ComponentBays:
						BaysButton.isOn = true;
						break;
					case PanelMode.Info:
						DescriptionButton.isOn = true;
						break;
					case PanelMode.Stats:
						StatsButton.isOn = true;
						break;
					case PanelMode.Factory:
						FactoryToggle.isOn = true;
						break;
					}
					UpdatePanelVisibility();
				}
			}
		}

		protected override void awake()
		{
			base.awake();
			BaysButton.onValueChanged.AddListener(ShowBaysToggleChanged);
			DescriptionButton.onValueChanged.AddListener(ShowDescriptionToggleChanged);
			StatsButton.onValueChanged.AddListener(ShowStatsToggleChanged);
			ShowCargoButton.onValueChanged.AddListener(ShowCargoButtonToggleChanged);
			HangarButton.onValueChanged.AddListener(ShowHangarBaysToggleChanged);
			FactoryToggle.onValueChanged.AddListener(FactoryToggleValueChanged);
		}

		protected override void start()
		{
			base.start();
			UpdatePanelVisibility();
		}

		protected override void refresh()
		{
			base.refresh();
			if (Unit != null)
			{
				BaysButton.gameObject.SetActive(Unit.IsStationOrShip());
				StatsButton.gameObject.SetActive(Unit.IsStationOrShip());
				if (Unit.IsStationOrShip())
				{
					RefreshStats();
				}
				if (Unit.IsStationOrShip())
				{
					ShipNameLabel.text = UnitClass.GetClassAndSeriesName();
				}
				else
				{
					ShipNameLabel.text = UnitClass.UnitPrefab.ClassName;
				}
				bool showRevision = !string.IsNullOrEmpty(UnitClass.Description);
				UpdateManufacturerLabel();
				UpdateDescription(showRevision);
				ClassLabel.text = ShipBuyScreen.GetUnitClassification(Unit);
				if (Previewer != null)
				{
					Previewer.ChangeUnitClass(Unit.UnitClass);
				}
			}
			else if (Previewer != null)
			{
				Previewer.ChangeUnitClass(null);
			}
			UpdateShipSprite();
			if (Unit != null)
			{
				RefreshComponentBaysList();
				HangarButton.gameObject.SetActive(Unit.GetComponent<UnitHangar>() != null);
				FactoryToggle.gameObject.SetActive(GetValidCargoFactoryProfile() != null);
				RefreshHangarBayList();
				RefreshCargoFactoryList();
			}
		}

		private CargoFactoryProfile GetValidCargoFactoryProfile()
		{
			if (Unit != null)
			{
				UnitCargoFactory component = Unit.GetComponent<UnitCargoFactory>();
				if (component != null && component.Items.Count > 0)
				{
					return component.CargoFactoryProfile;
				}
			}
			return null;
		}

		private ComponentBay[] GetUnitComponentBays(Unit unit)
		{
			if (unit.Engine != null)
			{
				if (unit.Components != null)
				{
					return unit.Components.Bays.ToArray();
				}
				return new ComponentBay[0];
			}
			return unit.transform.GetComponentsInChildren<ComponentBay>();
		}

		public IEnumerable<UnitHangarBay> GetUnitHangarBays(Unit unit)
		{
			List<UnitHangarBay> list = new List<UnitHangarBay>();
			unit.transform.AddComponentsInChildrenToList(list);
			return list;
		}

		private void RefreshCargoFactoryList()
		{
			CargoFactoryProfile validCargoFactoryProfile = GetValidCargoFactoryProfile();
			if (validCargoFactoryProfile != null)
			{
				CargoFactoryProfileList.SetItems(validCargoFactoryProfile.Items);
			}
			else
			{
				CargoFactoryProfileList.ClearActiveItems();
			}
		}

		private void RefreshComponentBaysList()
		{
			ComponentBay[] items = (from e in GetUnitComponentBays(Unit)
				where e.BayType.ShowInInfoUI
				orderby e.BayType.OrderInTradeUI, e.name
				select e).ToArray();
			BaysListUI.SetItems(items);
		}

		private void RefreshHangarBayList()
		{
			IOrderedEnumerable<UnitHangarBay> items = from e in GetUnitHangarBays(Unit)
				orderby e.MaxHullType descending
				select e;
			HangarBaysList.SetItems(items);
		}

		protected override void onEnable()
		{
			base.onEnable();
			if (Previewer != null)
			{
				Previewer.ChangeUnitClass(UnitClass);
			}
		}

		protected override void onDisable()
		{
			base.onDisable();
			if (Previewer != null)
			{
				Previewer.ChangeUnitClass(null);
			}
		}

		private void ShowCargoButtonToggleChanged(bool value)
		{
			if (value)
			{
				panelMode = PanelMode.Cargo;
				UpdatePanelVisibility();
			}
		}

		private void ShowStatsToggleChanged(bool value)
		{
			if (value)
			{
				panelMode = PanelMode.Stats;
				UpdatePanelVisibility();
			}
		}

		private void FactoryToggleValueChanged(bool value)
		{
			if (value)
			{
				panelMode = PanelMode.Factory;
				UpdatePanelVisibility();
			}
		}

		private void ShowHangarBaysToggleChanged(bool value)
		{
			if (value)
			{
				panelMode = PanelMode.HangarBays;
				UpdatePanelVisibility();
			}
		}

		private void ShowDescriptionToggleChanged(bool value)
		{
			if (value)
			{
				panelMode = PanelMode.Info;
				UpdatePanelVisibility();
			}
		}

		private void ShowBaysToggleChanged(bool value)
		{
			if (value)
			{
				panelMode = PanelMode.ComponentBays;
				UpdatePanelVisibility();
			}
		}

		private void UpdatePanelVisibility()
		{
			StatsRoot.SetActive(panelMode == PanelMode.Stats);
			DescriptionRoot.SetActive(panelMode == PanelMode.Info);
			ComponentBaysRoot.SetActive(panelMode == PanelMode.ComponentBays);
			CargoRoot.SetActive(panelMode == PanelMode.Cargo);
			HangarBaysRoot.SetActive(panelMode == PanelMode.HangarBays);
			FactoryRoot.SetActive(panelMode == PanelMode.Factory);
		}

		private void RefreshStats()
		{
			ShipStatsGenerator.Item = UnitClass;
			ShipStatsGenerator.Refresh();
		}

		private string GetDescription(bool showRevision)
		{
			if (UnitClass.UnitSeries != null)
			{
				StringBuilder stringBuilder = new StringBuilder(UnitClass.UnitSeries.Description);
				if (showRevision)
				{
					stringBuilder.AppendLine();
					stringBuilder.AppendLine();
					string unitModelName = GetUnitModelName(UnitClass);
					stringBuilder.AppendLine(unitModelName);
					if (!string.IsNullOrEmpty(UnitClass.Description))
					{
						stringBuilder.AppendLine(UnitClass.Description);
					}
				}
				if (string.IsNullOrEmpty(stringBuilder.ToString()))
				{
					return "No information available";
				}
				return stringBuilder.ToString();
			}
			if (!string.IsNullOrEmpty(UnitClass.Description))
			{
				return UnitClass.Description;
			}
			return "No information available";
		}

		private string GetUnitModelName(UnitClass unitClass)
		{
			if (!string.IsNullOrEmpty(UnitClass.UnitPrefab.ClassName))
			{
				return UnityRichTextHelper.Color("Type-" + UnitClass.UnitPrefab.ClassName, EngineASX.Instance.GameSettings.TextHeadingColor);
			}
			return UnityRichTextHelper.Color("Type-Unknown", EngineASX.Instance.GameSettings.TextHeadingColor);
		}

		private void UpdateDescription(bool showRevision)
		{
			DescriptionLabel.text = GetDescription(showRevision);
		}

		private void UpdateManufacturerLabel()
		{
			if (ManufacturerLabel != null)
			{
				if (UnitClass.Manufacturer != null)
				{
					ManufacturerLabel.text = UnitClass.Manufacturer.GetShortNameElseLong();
				}
				else
				{
					ManufacturerLabel.text = "Unknown";
				}
			}
		}

		private void UpdateShipSprite()
		{
			if (ShipImage != null && UnitClass != null)
			{
				ShipImage.sprite = EngineASX.Instance.EngineResources.GetUnitClassRenderSpriteOrDefault(UnitClass);
			}
		}
	}
}
