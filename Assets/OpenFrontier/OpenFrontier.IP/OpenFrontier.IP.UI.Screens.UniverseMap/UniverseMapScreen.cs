using System;
using System.Collections.Generic;
using System.Linq;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.Engine.Factions;
using OpenFrontier.IP.UI.Screens.Fleets;
using OpenFrontier.IP.UI.Screens.NewFleetOrder;
using OpenFrontier.IP.UI.Screens.RenameUnit;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace OpenFrontier.IP.UI.Screens.UniverseMap
{
	public class UniverseMapScreen : EngineScreen
	{
		public bool IgnoreIntel;

		public Func<UniverseMapItemUI, bool> SelectingSectorItem;

		public IEnumerable<Sector> CustomDisplayedSectors;

		public Button ClearSelectionButton;

		public float MinItemScale = 0.1f;

		public UniverseMapZoomer Zoomer;

		public Button CenterOnOriginButton;

		public Button RenameSectorButton;

		public Button RestrictSectorNavigationButton;

		public Button AllowSectorNavigationButton;

		public Button ConfirmSectorSelectionButton;

		public IEnumerable<Sector> EnabledSectors;

		public Text TitleLabel;

		public bool ShowSelectedSectorInfo = true;

		public Action<UniverseMapScreen, Sector> SectorSelectedCallback;

		public float ConnectionAlpha = 1f;

		public Button ViewSectorMapButton;

		public Button SetWaypointToSectorButton;

		public Button FlyToSectorButton;

		public Button ViewPropertyButton;

		public Button ViewFleetsButton;

		public bool AllowSectorSelection = true;

		public bool AllowSectorSelectionPick = true;

		private float defaultScaleFactor = -1f;

		public float ConnectionRotationZFudge;

		private UniverseMapItemUI selectedSectorItem;

		public Color DefaultSceneColor = Color.white;

		public Color DefaultSceneConnectionColor = Color.white;

		public float GutterSize = 20f;

		public RectTransform ItemHolder;

		public UniverseMapItemUI ItemPrefab;

		private List<UniverseMapItemUI> mapItems = new List<UniverseMapItemUI>();

		public ScrollRect MapScrollView;

		public UniverseMapConnectionItem SectorConnectionPrefab;

		public UniverseMapConnectionItem SectorUnstableConnectionPrefab;

		private List<UniverseMapConnectionItem> sectorConnections = new List<UniverseMapConnectionItem>();

		public Sprite ConnectionStubSprite;

		public Sprite ConnectionSprite;

		public float SceneConnectionStubLength = 80f;

		public float SceneConnectionWidthScale = 20f;

		public float StableConnectionLengthMultiplierFudge = 1f;

		[FormerlySerializedAs("ScenePositionScaleFactor")]
		public float ScaleFactor = 1f;

		public float MinScaleFactor = 0.5f;

		public float MaxScaleFactor = 2f;

		private List<Sector> sectorsToRender = new List<Sector>();

		public GameObject SelectedSceneRoot;

		public TextMeshProUGUI SelectedSectorLabel;

		public Text SelectedSectorOwnershipLabel;

		public Text SelectedSectorHomeSectorsLabel;

		public Text SelectedSectorSecurityRatingLabel;

		public bool UseSecurityColors = true;

		public float WaypointSceneConnectionWidthScale = 20f;

		private Vector3 worldCenterMapPosition = Vector3.zero;

		public float DiscoveredSectorConnectionLengthMultiplier = 1f;

		public float ShowLabelsScaleFactorThreshold = 0.35f;

		public GamePlayer LocalPlayer => Eng.LocalPlayer;

		public UniverseMapItemUI SelectedSectorItem
		{
			get
			{
				return selectedSectorItem;
			}
			set
			{
				if (selectedSectorItem != value)
				{
					UniverseMapItemUI universeMapItemUI = selectedSectorItem;
					selectedSectorItem = value;
					if (universeMapItemUI != null)
					{
						universeMapItemUI.RefreshSelectedImage();
					}
					if (selectedSectorItem != null)
					{
						selectedSectorItem.RefreshSelectedImage();
					}
					OnCurrentItemChanged();
				}
			}
		}

		public float DefaultScaleFactor => defaultScaleFactor;

		public List<UniverseMapItemUI> MapItems => mapItems;

		public float ItemScale => Mathf.Clamp(ScaleFactor / defaultScaleFactor, MinItemScale, float.MaxValue);

		public string Title
		{
			get
			{
				return TitleLabel.text;
			}
			set
			{
				TitleLabel.text = value;
			}
		}

		internal void SetSectorFromOrderTarget(NewOrderTarget orderTarget)
		{
			Sector sector = orderTarget.Sectors.FirstOrDefault();
			if (sector != null)
			{
				SelectSector(sector);
				CenterOnSector(sector);
			}
		}

		public void RestrictNavigationAway()
		{
			RestrictNavigationAwayEnabled = true;
			ShowDockHeader = false;
			ShowSelectedSectorInfo = false;
		}

		public void SelectSector(Sector sector)
		{
			if (GetSectorItem(sector) != null)
			{
				SelectedSectorItem = GetSectorItem(sector);
			}
		}

		private void OnCurrentItemChanged()
		{
			RefreshSelectedSectorRoot();
			RefreshSelectedSectorInfo();
			RefreshSelectedSectorButtonsEnabled();
			RefreshConfirmSelectionButton();
		}

		private bool ShouldShowAllowSectorNavigationButton()
		{
			if (AllowSectorSelection && selectedSectorItem != null && EngineASX.Instance.LocalFaction != null)
			{
				return EngineASX.Instance.LocalFaction.AutopilotExcludedSectors.Contains(selectedSectorItem.Sector.UniqueId);
			}
			return false;
		}

		internal void TrySelect(UniverseMapItemUI universeMapItemUI)
		{
			if (SelectingSectorItem == null || SelectingSectorItem(universeMapItemUI))
			{
				SelectedSectorItem = universeMapItemUI;
			}
		}

		private bool ShouldShowRestrictSectorNavigationButton()
		{
			if (AllowSectorSelection && selectedSectorItem != null && EngineASX.Instance.LocalFaction != null)
			{
				return !EngineASX.Instance.LocalFaction.AutopilotExcludedSectors.Contains(selectedSectorItem.Sector.UniqueId);
			}
			return false;
		}

		private void RefreshConfirmSelectionButton()
		{
			ConfirmSectorSelectionButton.gameObject.SetActive(AllowSectorSelectionPick && selectedSectorItem != null);
		}

		private void RefreshSelectedSectorRoot()
		{
			SelectedSceneRoot.gameObject.SetActive(ShowSelectedSectorInfo && selectedSectorItem != null);
		}

		public Vector3 GetSectorLocalPosition(Sector scene)
		{
			return GetLocalPosition(scene.MapPosition);
		}

		public Vector3 GetLocalPosition(Vector3 universePosition)
		{
			universePosition -= worldCenterMapPosition;
			return new Vector3(universePosition.x * ScaleFactor, universePosition.z * ScaleFactor, 0f);
		}

		public void RefreshSelectedSectorInfo()
		{
			if (selectedSectorItem != null)
			{
				SelectedSectorLabel.text = selectedSectorItem.Sector.Name;
				SelectedSectorOwnershipLabel.text = GetSectorOwnershipDescription(selectedSectorItem.Sector);
				SelectedSectorOwnershipLabel.color = GetSectorOwnershipColor(selectedSectorItem.Sector);
				SelectedSectorHomeSectorsLabel.text = GetSectorHomeSectorsDescription(selectedSectorItem.Sector);
				SelectedSectorSecurityRatingLabel.text = GetSectorSecurityLevelDescription(selectedSectorItem.Sector);
			}
		}

		private string GetSectorSecurityLevelDescription(Sector sector)
		{
			return sector.GetSectorSecurityLevelDescription();
		}

		private string GetSectorHomeSectorsDescription(Sector sector)
		{
			IEnumerable<Faction> source = sector.FactionsHeadquartered.Where((Faction e) => sector.ControllingFaction != e && !e.IsFreelancer && !e.IsPlayerFaction && e.IsKnownToPlayer && e.Name != sector.Name + " Bandits");
			if (source.Any())
			{
				return string.Join(", ", from e in source.OrderByDescending((Faction e) => e.GetCachedNetWorth()).Take(3)
					select e.GetFriendlyName());
			}
			return "-";
		}

		private Color GetSectorOwnershipColor(Sector sector)
		{
			if (sector.ControllingFaction == null)
			{
				return EngineASX.Instance.GetFactionHostilityColor(sector.ControllingFaction, EngineASX.Instance.LocalFaction);
			}
			return EngineASX.Instance.AttitudeNeutralColor;
		}

		private string GetSectorOwnershipDescription(Sector sector)
		{
			if (sector.ControllingFaction == null)
			{
				return "[Unclaimed]";
			}
			string text = ((EngineASX.Instance.LocalFaction == sector.ControllingFaction || EngineASX.Instance.LocalFaction.HasAttitudeToFaction(sector.ControllingFaction)) ? sector.ControllingFaction.Name : "an unknown empire");
			if (sector == sector.ControllingFaction.HomeSector)
			{
				return "Home of " + text;
			}
			return "Controlled by " + text;
		}

		public void CenterOnPlayerSector()
		{
			CenterOnSector(Eng.LocalPlayerSector);
		}

		public void CenterOnOrderTargetSector(NewOrderTarget newOrderTarget)
		{
			Sector sector = newOrderTarget.Sectors.FirstOrDefault();
			if (sector != null)
			{
				CenterOnSector(sector);
			}
		}

		public void CenterOnSector(Sector sector)
		{
			if (sector != null)
			{
				CenterOnMapPosition(sector.MapPosition);
			}
			RemoveScrollVelocity();
		}

		public void RemoveScrollVelocity()
		{
			MapScrollView.velocity = Vector2.zero;
		}

		public void CenterOnMapPosition(Vector3 position)
		{
			ItemHolder.transform.localPosition = -GetLocalPosition(position);
		}

		protected override void awake()
		{
			base.awake();
			OnCurrentItemChanged();
			ViewSectorMapButton.onClick.AddListener(ViewSectorMapButtonClick);
			FlyToSectorButton.onClick.AddListener(FlyToSectorButtonClick);
			ViewPropertyButton.onClick.AddListener(ViewPropertyButtonClick);
			ViewFleetsButton.onClick.AddListener(ViewFleetsButtonClick);
			SetWaypointToSectorButton.onClick.AddListener(SetWaypointToSectorButtonClick);
			ConfirmSectorSelectionButton.onClick.AddListener(ConfirmSectorSelectionButtonClick);
			RenameSectorButton.onClick.AddListener(RenameSectorButtonClick);
			CenterOnOriginButton.onClick.AddListener(CenterOnOriginButtonClick);
			AllowSectorNavigationButton.onClick.AddListener(AllowSectorNavigationButtonClick);
			RestrictSectorNavigationButton.onClick.AddListener(RestrictSectorNavigationButtonClick);
			defaultScaleFactor = ScaleFactor;
			ClearSelectionButton.onClick.AddListener(ClearSelectionButtonClick);
		}

		private void AllowSectorNavigationButtonClick()
		{
			if (ShouldShowAllowSectorNavigationButton())
			{
				EngineASX.Instance.LocalFaction.AutopilotExcludedSectors.Remove(selectedSectorItem.Sector.UniqueId);
				selectedSectorItem.Refresh();
			}
		}

		private void RestrictSectorNavigationButtonClick()
		{
			if (ShouldShowRestrictSectorNavigationButton())
			{
				EngineASX.Instance.LocalFaction.AutopilotExcludedSectors.Add(selectedSectorItem.Sector.UniqueId);
				selectedSectorItem.Refresh();
			}
		}

		private void ViewPropertyButtonClick()
		{
			if (selectedSectorItem != null)
			{
				UIController.Instance.ScreenNavigator.ShowPropertyScreen((PropertyScreen screen) =>
				{
					screen.SectorFilter = selectedSectorItem.Sector;
				});
			}
		}

		private void ViewFleetsButtonClick()
		{
			if (selectedSectorItem != null)
			{
				UIController.Instance.ScreenNavigator.ShowFleetsScreen((FleetsScreen screen) =>
				{
					screen.SectorFilter.Sector = selectedSectorItem.Sector;
				});
			}
		}

		private void ClearSelectionButtonClick()
		{
			SelectedSectorItem = null;
		}

		protected override void onNotCurrentPanel()
		{
			base.onNotCurrentPanel();
			MapScrollView.velocity = Vector2.zero;
		}

		private void SetWaypointToSectorButtonClick()
		{
			if (CanSetWaypointToSelectedSector())
			{
				Eng.LocalPlayer.SetCustomWaypointToSectorPosition(selectedSectorItem.Sector, Vector3.zero, autoRemove: false);
			}
		}

		public void ConfirmSectorSelectionButtonClick()
		{
			if (selectedSectorItem != null && SectorSelectedCallback != null)
			{
				SectorSelectedCallback(this, selectedSectorItem.Sector);
			}
		}

		private bool CanSetWaypointToSelectedSector()
		{
			return selectedSectorItem != null;
		}

		private void FlyToSectorButtonClick()
		{
			if (CanFlyToSelectedSector())
			{
				OrdersHelper.OrderMoveToSectorTarget(OrdersHelper.FindOrCreateNpcAndFleetAndPilotShip(EngineASX.Instance.PlayerUnit), new SectorTarget
				{
					Sector = selectedSectorItem.Sector,
					SectorPosition = Vector3.zero
				}, stack: false);
			}
		}

		private bool CanFlyToSelectedSector()
		{
			if (selectedSectorItem != null && OrdersHelper.CanPlayerOrderUnit(EngineASX.Instance.PlayerUnit))
			{
				return selectedSectorItem.Sector != EngineASX.Instance.PlayerUnit.Sector;
			}
			return false;
		}

		public void RebuildIfNeeded()
		{
			if (mapItems.Count == 0)
			{
				Rebuild();
			}
		}

		public void RebuildIfNeededAndApplyPlayerIntel()
		{
			RebuildIfNeeded();
			RefreshDiscoveredSectors();
			RefreshDiscoveredConnections();
		}

		public void RefreshSectorNameLabels()
		{
			foreach (UniverseMapItemUI mapItem in mapItems)
			{
				mapItem.RefreshNameLabel();
			}
		}

		protected override void refresh()
		{
			base.refresh();
			if (Eng != null)
			{
				ClearSelectedItemWhenInvalid();
				RefreshSelectedSectorInfo();
				RefreshSelectedSectorButtonsEnabled();
				RefreshConfirmSelectionButton();
				RefreshSelectedSectorRoot();
			}
		}

		private void RefreshPropertyButtonInteractable()
		{
			ViewPropertyButton.gameObject.SetActive(selectedSectorItem != null && selectedSectorItem.Sector != null && selectedSectorItem.Sector.ContainsPlayerProperty());
		}

		private void RefreshFleetsButtonInteractable()
		{
			ViewFleetsButton.gameObject.SetActive(selectedSectorItem != null && selectedSectorItem.Sector != null && selectedSectorItem.Sector.ContainsPlayerFleets());
		}

		private void RefreshRenameSectorButton()
		{
			RenameSectorButton.gameObject.SetActive(selectedSectorItem != null && selectedSectorItem.Sector.IsControlledByPlayer());
		}

		public void ClearSelectedItemWhenInvalid()
		{
			if (selectedSectorItem != null && (!mapItems.Contains(selectedSectorItem) || !selectedSectorItem.gameObject.activeSelf))
			{
				SelectedSectorItem = null;
			}
		}

		public void ClearSelectedItem()
		{
			SelectedSectorItem = null;
		}

		private void RefreshDiscoveredSectors()
		{
			foreach (UniverseMapItemUI mapItem in mapItems)
			{
				mapItem.gameObject.SetActive(ShouldShowSector(mapItem.Sector));
			}
		}

		private bool ShouldShowSector(Sector sector)
		{
			if (IgnoreIntel || EngineASX.Instance.LocalFaction == null || CustomDisplayedSectors != null)
			{
				if (CustomDisplayedSectors != null)
				{
					return CustomDisplayedSectors.Contains(sector);
				}
				return true;
			}
			return EngineASX.Instance.LocalFaction.Intel.IsSectorDiscovered(sector);
		}

		private bool ShouldShowSectorConnection(UniverseMapConnectionItem sectorConnection)
		{
			if (!ShouldShowSector(sectorConnection.ConnectingGate.Sector))
			{
				return false;
			}
			if (!IgnoreIntel && !(EngineASX.Instance.LocalFaction == null))
			{
				return ShowSectorConnectionForWormholeBasedOnIntel(sectorConnection.Neighbour.Value.ConnectingGate);
			}
			return true;
		}

		public bool ShowSectorConnectionForWormholeBasedOnIntel(Wormhole wormhole)
		{
			if (EngineASX.Instance.LocalFaction.Intel.IsUnitDiscovered(wormhole.Unit.UniqueId) && EngineASX.Instance.LocalFaction.Intel.IsSectorDiscovered(wormhole.Unit.Sector))
			{
				if (wormhole.IsUnstable)
				{
					return EngineASX.Instance.LocalFaction.Intel.HasWormholeBeenEntered(wormhole);
				}
				return true;
			}
			return false;
		}

		private void RenameSectorButtonClick()
		{
			UIController.Instance.ScreenNavigator.ShowRenameSectorScreen(selectedSectorItem.Sector, RenameSectorButtonClickCallback);
		}

		private void RenameSectorButtonClickCallback(RenameUnitScreen handler, bool rename, string newName)
		{
			if (!rename)
			{
				return;
			}
			selectedSectorItem.Sector.Name = newName;
			Refresh();
			foreach (UniverseMapItemUI mapItem in mapItems)
			{
				mapItem.RefreshStatic();
			}
		}

		private void RefreshDiscoveredConnections()
		{
			foreach (UniverseMapConnectionItem sectorConnection in sectorConnections)
			{
				sectorConnection.gameObject.SetActive(ShouldShowSectorConnection(sectorConnection));
			}
		}

		private void RefreshSelectedSectorButtonsEnabled()
		{
			ViewSectorMapButton.gameObject.SetActive(selectedSectorItem != null);
			FlyToSectorButton.gameObject.SetActive(CanFlyToSelectedSector());
			SetWaypointToSectorButton.gameObject.SetActive(CanSetWaypointToSelectedSector());
			RefreshRenameSectorButton();
			RefreshPropertyButtonInteractable();
			RefreshFleetsButtonInteractable();
			AllowSectorNavigationButton.gameObject.SetActive(ShouldShowAllowSectorNavigationButton());
			RestrictSectorNavigationButton.gameObject.SetActive(ShouldShowRestrictSectorNavigationButton());
		}

		public void AutoSelectPlayerSector()
		{
			if (selectedSectorItem == null && AllowSectorSelection)
			{
				SelectedSectorItem = GetActiveSectorItem();
			}
		}

		public UniverseMapItemUI GetActiveSectorItem()
		{
			return GetSectorItem(EngineASX.Instance.ActiveSector);
		}

		public UniverseMapItemUI GetSectorItem(Sector sector)
		{
			return mapItems.FirstOrDefault((UniverseMapItemUI e) => e.Sector != null && e.Sector == sector);
		}

		protected override void update()
		{
			base.update();
			if (!(Eng != null))
			{
				return;
			}
			Zoomer.UpdateZoom();
			RefreshDiscoveredSectors();
			RefreshDiscoveredConnections();
			RefreshSelectedSectorButtonsEnabled();
			RefreshSectorConnections();
			foreach (UniverseMapItemUI mapItem in mapItems)
			{
				mapItem.UpdateMapItem();
			}
		}

		public void RefreshSectorConnections()
		{
			foreach (UniverseMapConnectionItem sectorConnection in sectorConnections)
			{
				sectorConnection.Refresh();
			}
		}

		private void CenterOnOriginButtonClick()
		{
			Zoomer.ResetZoom();
			CenterOnPlayerSector();
		}

		public void SetScaleFactor(float newScaleFactor)
		{
			_ = ScaleFactor;
			float num = Mathf.Clamp(newScaleFactor, MinScaleFactor, MaxScaleFactor);
			if (ScaleFactor == num)
			{
				return;
			}
			Vector3 viewWorldPosition = GetViewWorldPosition();
			ScaleFactor = num;
			RepositionUniverseMap(viewWorldPosition);
			foreach (UniverseMapItemUI mapItem in mapItems)
			{
				mapItem.Rescale();
			}
			RefreshSectorConnections();
		}

		private void RepositionUniverseMap(Vector3 worldPosition)
		{
			UpdateItemHolderSize();
			SetViewWorldPosition(worldPosition);
			foreach (UniverseMapItemUI mapItem in mapItems)
			{
				mapItem.Reposition();
			}
			foreach (UniverseMapConnectionItem sectorConnection in sectorConnections)
			{
				sectorConnection.SetPositionAndRotation();
			}
		}

		public void SetViewWorldPosition(Vector3 worldPosition)
		{
			Vector3 localPosition = ConvertWorldToScreen(worldPosition);
			ItemHolder.localPosition = localPosition;
		}

		public Vector3 GetViewWorldPosition()
		{
			Vector3 localPosition = ItemHolder.localPosition;
			return ConvertMapAreaLocalPositionToWorld(localPosition);
		}

		public Vector3 ConvertMapAreaLocalSectorPosition(Vector3 screenPosition)
		{
			return new Vector3
			{
				x = screenPosition.x / ScaleFactor,
				y = 0f,
				z = screenPosition.y / ScaleFactor
			};
		}

		public Vector3 ConvertWorldToScreen(Vector3 worldPosition)
		{
			worldPosition -= worldCenterMapPosition;
			return new Vector3
			{
				x = worldPosition.x * ScaleFactor,
				y = worldPosition.z * ScaleFactor
			};
		}

		public Vector3 ConvertMapAreaLocalPositionToWorld(Vector3 screenPosition)
		{
			return ConvertMapAreaLocalSectorPosition(screenPosition) + worldCenterMapPosition;
		}

		private void ViewSectorMapButtonClick()
		{
			if (SelectedSectorItem != null)
			{
				UIController.Instance.ScreenNavigator.ShowSectorMapScreen(SelectedSectorItem.Sector, null);
			}
		}

		private void UpdateWorldCenter()
		{
			Rect mapRect = GetMapRect();
			worldCenterMapPosition = new Vector3(mapRect.center.x, 0f, mapRect.center.y);
		}

		private Rect GetMapRect()
		{
			List<Sector> source = sectorsToRender;
			float num = source.Select((Sector e) => e.MapPosition.x).Min();
			float num2 = source.Select((Sector e) => e.MapPosition.x).Max();
			float num3 = source.Select((Sector e) => e.MapPosition.z).Min();
			float num4 = source.Select((Sector e) => e.MapPosition.z).Max();
			return new Rect(num, num3, num2 - num, num4 - num3);
		}

		private void Clear()
		{
			sectorConnections.Clear();
			mapItems.Clear();
			if (ItemHolder != null)
			{
				UnityObjectHelper.DestroyChildren(ItemHolder.gameObject, destroyImmediate: true);
			}
		}

		public static bool IsGateConnectionOnPlayerWaypointPath(PlayerWaypointPath path, Wormhole wormhole)
		{
			if (path.HasValidPath)
			{
				List<WorldNavpoint> waypoints = path.Waypoints;
				for (int i = 0; i < waypoints.Count - 1; i++)
				{
					if (waypoints[i].TargetType == WorldNavpointTargetType.Gate && waypoints[i].TargetSectorObject == wormhole.Unit)
					{
						return true;
					}
				}
			}
			return false;
		}

		protected override void onMadeCurrentPanel(bool navigatedForward)
		{
			base.onMadeCurrentPanel(navigatedForward);
			RefreshSelectedSectorRoot();
			RefreshSectorNameLabels();
			RefreshSelectedSectorInfo();
		}

		private void Rebuild()
		{
			Clear();
			if (ItemHolder != null && Eng != null)
			{
				PopulateSectorsToRender();
				UpdateWorldCenter();
				CreateUnstableSectorConnections();
				CreateStableSectorConnections();
				CreateSectorItems();
				UpdateItemHolderSize();
			}
		}

		private void CreateStableSectorConnections()
		{
			foreach (Sector item in sectorsToRender)
			{
				foreach (SectorNeighbour neighbour in item.Neighbours)
				{
					if (neighbour.IsStableConnection)
					{
						CreateAndAddSectorConnection(item, neighbour);
					}
				}
			}
		}

		private void CreateUnstableSectorConnections()
		{
			foreach (Sector item in sectorsToRender)
			{
				foreach (SectorNeighbour neighbour in item.Neighbours)
				{
					if (!neighbour.IsStableConnection)
					{
						CreateAndAddSectorConnection(item, neighbour);
					}
				}
			}
		}

		private void CreateAndAddSectorConnection(Sector sector, SectorNeighbour neighbor)
		{
			UniverseMapConnectionItem universeMapConnectionItem = UnityEngine.Object.Instantiate(GetSectorConnectionPrefab(neighbor));
			universeMapConnectionItem.UniverseMap = this;
			universeMapConnectionItem.ConnectingGate = neighbor.ConnectingGate;
			universeMapConnectionItem.Sector = sector;
			universeMapConnectionItem.Neighbour = neighbor;
			universeMapConnectionItem.transform.SetParent(ItemHolder.transform);
			universeMapConnectionItem.Build();
			universeMapConnectionItem.Refresh();
			sectorConnections.Add(universeMapConnectionItem);
		}

		private UniverseMapConnectionItem GetSectorConnectionPrefab(SectorNeighbour neighbor)
		{
			if (neighbor.IsStableConnection)
			{
				return SectorConnectionPrefab;
			}
			return SectorUnstableConnectionPrefab;
		}

		public float GetConnectionScaleFromDistance(Vector3 p1, Vector3 p2)
		{
			return Vector3.Distance(p1, p2) * DiscoveredSectorConnectionLengthMultiplier;
		}

		public Color GetConnectionColor(Sector sector, bool useSecurityColours, Color defaultSectorConnectionColour)
		{
			if (!useSecurityColours)
			{
				return defaultSectorConnectionColour;
			}
			Color result = ColorBySecurity(sector);
			result.a = ConnectionAlpha;
			return result;
		}

		private static Color ColorBySecurity(Sector sector)
		{
			if (sector.ControllingFaction != null && EngineASX.Instance.LocalFaction != null && sector.ControllingFaction != EngineASX.Instance.LocalFaction && sector.ControllingFaction.IsHostileToOrAlwaysHostileTo(EngineASX.Instance.LocalFaction))
			{
				return GameController.Instance.GameSettings.ColorSettings.UniverseMapHostileSectorColor;
			}
			return GetConnectionColor(sector.SecurityLevel);
		}

		public Color GetSectorColor(Sector sector)
		{
			if (!UseSecurityColors)
			{
				return DefaultSceneColor;
			}
			return ColorBySecurity(sector);
		}

		private void PopulateSectorsToRender()
		{
			sectorsToRender = Eng.Sectors.ToList();
		}

		private void CreateSectorItems()
		{
			ToggleGroup component = ItemHolder.GetComponent<ToggleGroup>();
			if (ItemPrefab != null)
			{
				foreach (Sector item in sectorsToRender)
				{
					UniverseMapItemUI universeMapItemUI = CreateItem(item);
					universeMapItemUI.name = item.Name;
					universeMapItemUI.transform.SetParent(ItemHolder.transform, worldPositionStays: true);
					universeMapItemUI.transform.localScale = Vector3.one;
					universeMapItemUI.RefreshStatic();
					Toggle component2 = universeMapItemUI.GetComponent<Toggle>();
					if (component2 != null)
					{
						component2.group = component;
					}
					mapItems.Add(universeMapItemUI);
					universeMapItemUI.gameObject.SetActive(LocalPlayer.Faction.Intel.IsSectorDiscovered(item));
				}
				return;
			}
			Debug.LogError("Cannot create scene items. ItemPrefab not assigned", this);
		}

		private void UpdateItemHolderSize()
		{
			Rect mapRect = GetMapRect();
			Vector2 vector = new Vector2(mapRect.width * ScaleFactor, mapRect.height * ScaleFactor);
			ItemHolder.sizeDelta = new Vector2(vector.x + GutterSize, vector.y + GutterSize);
		}

		public static Color GetConnectionColor(float securityLevel)
		{
			return EngineASX.Instance.GetSecurityColor(securityLevel);
		}

		private UniverseMapItemUI CreateItem(Sector s)
		{
			UniverseMapItemUI component = UnityEngine.Object.Instantiate(ItemPrefab.gameObject).GetComponent<UniverseMapItemUI>();
			component.UniverseMapUI = this;
			component.Sector = s;
			component.Refresh();
			component.NextSectorIconsRefreshTime = Time.time + UnityEngine.Random.value * 5f;
			return component;
		}
	}
}
