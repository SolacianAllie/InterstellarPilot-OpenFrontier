using System.Text;
using OpenFrontier.IP.Engine;
using OpenFrontier.IP.UI.Screens.UniverseMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenFrontier.IP.UI
{
	public class DockUIHeader : MonoBehaviour
	{
		private float lastTimeRefreshedTitleLabel = -100f;

		public TextMeshProUGUI CreditsText;

		public Image PausedActivatorImage;

		public Button NewMessagesButton;

		private string lastKnownShipName;

		private string lastKnownUnitName;

		private string lastKnownRootUnitName;

		private Sector lastPlayerScene;

		private Unit lastPlayerUnit;

		private Unit lastPlayerRootUnit;

		public TextMeshProUGUI LocationText;

		public Button LogButton;

		public Button MenuButton;

		private int oldPlayerCredits = -1;

		public Button PauseButton;

		public Button SectorMapButton;

		public Button UniverseMapButton;

		public Button PropertyButton;

		public Button FleetsButton;

		private static StringBuilder stringBuilder = new StringBuilder();

		public EngineASX Engine => EngineASX.Instance;

		public DockUI DockUI
		{
			get
			{
				if (Engine != null)
				{
					return Engine.DockUI;
				}
				return null;
			}
		}

		public void StorePlayerCreditsValue()
		{
			if (Engine != null)
			{
				oldPlayerCredits = Engine.CreditsAnimation.AnimatedValue;
				RefreshCreditsLabel();
			}
		}

		public void RefreshCreditsLabel()
		{
			CreditsText.text = TextFormattingHelper.FormatCredits(Engine.CreditsAnimation.AnimatedValue, includeSuffix: true);
		}

		public void UpdateTitleLabels()
		{
			LocationText.text = GetLocationText();
			lastTimeRefreshedTitleLabel = Time.realtimeSinceStartup;
		}

		private void Awake()
		{
			if (LogButton != null)
			{
				LogButton.onClick.AddListener(LogButton_Activated);
			}
			if (MenuButton != null)
			{
				MenuButton.onClick.AddListener(MenuButton_Activated);
			}
			PauseButton.onClick.AddListener(PauseButton_Activated);
			NewMessagesButton.onClick.AddListener(NewMessagesButtonClick);
			PropertyButton.onClick.AddListener(PropertyButtonClick);
			FleetsButton.onClick.AddListener(FleetsButtonClick);
			SectorMapButton.onClick.AddListener(SectorMapButtonClick);
			UniverseMapButton.onClick.AddListener(UniverseMapButtonClick);
			PausedActivatorImage.enabled = false;
			CreditsText.text = string.Empty;
		}

		private void PropertyButtonClick()
		{
			UIController.Instance.ScreenNavigator.TogglePropertyScreen();
		}

		private void FleetsButtonClick()
		{
			UIController.Instance.ScreenNavigator.ToggleFleetsScreen();
		}

		private void SectorMapButtonClick()
		{
			UIController.Instance.ScreenNavigator.ToggleSectorMapScreenWhenPilotting();
		}

		private void UniverseMapButtonClick()
		{
			ToggleUniverseMap();
		}

		public static void ToggleUniverseMap()
		{
			UIController.Instance.ScreenNavigator.ToggleCustomUniverseMapScreen((UniverseMapScreen screen) =>
			{
				screen.AutoSelectPlayerSector();
				screen.CenterOnPlayerSector();
			});
		}

		private void NewMessagesButtonClick()
		{
			UIController.Instance.ScreenNavigator.ToggleMessagesScreen();
		}

		private void PauseButton_Activated()
		{
			Engine.IsPaused = !Engine.IsPaused;
			if (Engine.IsPaused)
			{
				UIController.Instance.QuickMsg.AddMessage("Paused");
			}
		}

		private void MenuButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ShowPauseMenu(Engine.IsPaused);
		}

		private void LogButton_Activated()
		{
			UIController.Instance.ScreenNavigator.ToggleLogScreen();
		}

		private void Start()
		{
			StorePlayerCreditsValue();
		}

		private void Update()
		{
			if (!(Engine != null))
			{
				return;
			}
			Button sectorMapButton = SectorMapButton;
			bool interactable = (UniverseMapButton.interactable = Engine.World.Permissions.AllowDockUIMap);
			sectorMapButton.interactable = interactable;
			NewMessagesButton.interactable = Engine.World.Permissions.AllowDockUIMessages;
			PropertyButton.interactable = Engine.World.Permissions.AllowPropertyScreen;
			NewMessagesButton.interactable = Engine.AllowUiMessagesNavigation();
			PausedActivatorImage.enabled = Engine.IsPaused;
			if (!(Engine.LocalPlayer != null))
			{
				return;
			}
			Unit playerUnit = Engine.PlayerUnit;
			if (playerUnit != null)
			{
				Unit playerRootUnit = Engine.PlayerRootUnit;
				if (lastPlayerUnit != playerUnit || lastPlayerRootUnit != playerRootUnit || lastKnownUnitName != playerUnit.UnitName || lastKnownRootUnitName != playerRootUnit.UnitName || lastPlayerScene != Engine.ActiveSector || Time.realtimeSinceStartup > lastTimeRefreshedTitleLabel + 2f || (playerUnit != null && playerUnit.Components != null && playerUnit.Components.ShipName != lastKnownShipName))
				{
					if (playerUnit.IsValidAndNotDestroyed)
					{
						UpdateTitleLabels();
					}
					lastKnownUnitName = playerUnit.UnitName;
					lastKnownRootUnitName = playerRootUnit.UnitName;
					CacheLastKnownShipName(playerUnit);
					lastPlayerUnit = playerUnit;
					lastPlayerScene = Engine.ActiveSector;
					lastPlayerRootUnit = playerRootUnit;
				}
			}
			UpdateCreditsLabel();
			UpdateLocationLabelVisibility();
		}

		private void CacheLastKnownShipName(Unit playerUnit)
		{
			if (playerUnit != null && playerUnit.Components != null)
			{
				lastKnownShipName = playerUnit.Components.ShipName;
			}
			else
			{
				lastKnownShipName = null;
			}
		}

		private void UpdateLocationLabelVisibility()
		{
			bool active = UIController.Instance.QuickMsg.ShownMessageCount == 0 && UIController.Instance.QuickMsg.QueuedMessageCount == 0;
			LocationText.gameObject.SetActive(active);
		}

		private void OnEnable()
		{
			StorePlayerCreditsValue();
		}

		private void UpdateCreditsLabel()
		{
			if (oldPlayerCredits != Engine.CreditsAnimation.AnimatedValue)
			{
				oldPlayerCredits = Engine.CreditsAnimation.AnimatedValue;
				RefreshCreditsLabel();
			}
		}

		private void StopCreditsAnimation(int playerCredits)
		{
			oldPlayerCredits = playerCredits;
			RefreshCreditsLabel();
		}

		private static string GetUnitName(Unit unit, bool shortName = true)
		{
			string text = unit.GetFriendlyName(shortName);
			if (unit.Faction != null && !unit.Faction.IsPlayerFaction)
			{
				string shortNameElseLong = unit.Faction.GetShortNameElseLong();
				if (shortNameElseLong != unit.UnitName)
				{
					text += $" ({shortNameElseLong})";
				}
			}
			return text;
		}

		private string GetLocationText()
		{
			stringBuilder.Length = 0;
			Unit playerCurrentUnit = DockUI.PlayerCurrentUnit;
			if (playerCurrentUnit != null)
			{
				GetLocationTextNonAlloc(playerCurrentUnit, stringBuilder);
				return stringBuilder.ToString();
			}
			return null;
		}

		public static void GetLocationTextNonAlloc(Unit currentUnit, StringBuilder stringBuilder)
		{
			stringBuilder.Append(GetUnitName(currentUnit));
			if (currentUnit.IsDocked)
			{
				stringBuilder.Append(", docked in " + GetUnitName(currentUnit.GetDockUnit()));
			}
			stringBuilder.Append(", " + currentUnit.Sector.Name);
		}
	}
}
